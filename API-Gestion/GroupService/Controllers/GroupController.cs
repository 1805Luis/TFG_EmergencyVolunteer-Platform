using System.Security.Claims;
using GroupService.ApiResponse;
using GroupService.Data;
using GroupService.DTOs;
using GroupService.Interfaz;
using GroupService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroupService.Controllers
{
    [Route("grupos")]
    [ApiController]
    public class GroupController: ControllerBase
    {
        private readonly IAppDBContext _context;
        private readonly RabbitMQProducer _rabbitMQProducer;
        public GroupController(IAppDBContext appDBContext, RabbitMQProducer rabbitMQProducer)
        {
            _context = appDBContext;
            _rabbitMQProducer = rabbitMQProducer;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(
            string? nombreGrupo,
            string? zonaAsignada,
            [FromQuery(Name = "nombresRecursos")] List<string>? nombresRecursos,
            Guid? voluntarioId,
            Guid? grupoId,
            bool matchAll = false)
        {
            var query = _context.grupos
                .Include(g => g.Recursos)
                .AsQueryable();

            if (grupoId.HasValue)
                query = query.Where(g => g.Id == grupoId.Value);

            if (!string.IsNullOrWhiteSpace(nombreGrupo))
                query = query.Where(g => g.nombre.ToLower().Contains(nombreGrupo.ToLower()));

            if (!string.IsNullOrWhiteSpace(zonaAsignada))
                query = query.Where(g => g.zona_asignada.ToLower().Contains(zonaAsignada.ToLower()));

            if (nombresRecursos != null && nombresRecursos.Any())
            {
                var lowerRecursos = nombresRecursos
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n.Trim().ToLower())
                    .ToList();

                if (matchAll)
                {
                    foreach (var nombre in lowerRecursos)
                    {
                        query = query.Where(g => g.Recursos.Any(r => r.Nombre.ToLower().Contains(nombre)));
                    }
                }
                else
                {
                    query = query.Where(g =>
                        g.Recursos.Any(r =>
                            lowerRecursos.Any(nombre => r.Nombre.ToLower().Contains(nombre))
                        )
                    );
                }
            }

            if (voluntarioId.HasValue)
                query = query.Where(g => g.Recursos.Any(r => r.VoluntarioId == voluntarioId.Value));

            var grupos = await query
            .Select(g => new GrupoConRecursosDTO
            {
                Id = g.Id,
                Nombre = g.nombre,
                ZonaAsignada = g.zona_asignada,
                Recursos = g.Recursos.Select(r => new RecursoSimpleDTO
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    VoluntarioId = r.VoluntarioId
                }).ToList()
            })
            .ToListAsync();


            if (!grupos.Any())
                return NotFound(ApiResponse<string>.Fail("NotFound", "No se encontraron grupos con los parámetros proporcionados."));

            return Ok(ApiResponse<List<GrupoConRecursosDTO>>.Ok(grupos));
        }



        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(CrearGrupoConTareaDTO dto)
        {
            if (dto == null)
                return BadRequest(ApiResponse<string>.Fail("Validation", "Datos del grupo inválidos."));

            var grupo = new Grupo
            {
                Id = Guid.NewGuid(),
                nombre = dto.Grupo.nombre,
                zona_asignada = dto.Grupo.zona_asignada,
                Recursos = new List<Recursos>()
            };

            if (dto.Grupo.Recursos != null && dto.Grupo.Recursos.Any())
            {
                foreach (var recursoDto in dto.Grupo.Recursos)
                {
                    grupo.Recursos.Add(new Recursos
                    {
                        Id = Guid.NewGuid(),
                        GrupoId = grupo.Id,
                        Nombre = recursoDto.Nombre,
                        VoluntarioId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)
                    });
                }
            }

            await _context.grupos.AddAsync(grupo);
            await _context.SaveChangesAsync();

            var despues = new Dictionary<string, object>
            {
                { "nombre", grupo.nombre },
                { "zona_asignada", grupo.zona_asignada },
                { "Recursos", grupo.Recursos.Select(r => new { r.Id, r.Nombre, r.VoluntarioId }).ToList() }
            };

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            Guid actorId = Guid.TryParse(userIdClaim?.Value, out var idParsed) ? idParsed : Guid.Empty;

            var evento = new BitacoraEvent
            {
                EntityType = "Grupo y Recursos",
                EntityId = grupo.Id.ToString(),
                Action = "Create",
                ActorId = actorId,
                Antes = new Dictionary<string, object>(),
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            var asignacion = new AsignaciónRabbitDTO
            {
                Id_grupo = grupo.Id,
                Id_tarea = dto.id_tarea,
                Id_usuario = actorId
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("group_create_queue", asignacion, "group_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Grupo y recursos creados exitosamente."));
        }


        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> Patch(Guid id, PatchGrupoDTO dto)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var grupo = await _context.grupos.FindAsync(id);
            if (grupo == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Grupo no encontrado."));

            var antes = new Dictionary<string, object>();
            var despues = new Dictionary<string, object>();
            bool cambios = false;

            if (!string.IsNullOrWhiteSpace(dto.nombre) && dto.nombre != grupo.nombre)
            {
                antes["nombre"] = grupo.nombre;
                despues["nombre"] = dto.nombre;
                grupo.nombre = dto.nombre;
                cambios = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.zona_asignada) && dto.zona_asignada != grupo.zona_asignada)
            {
                antes["zona_asignada"] = grupo.zona_asignada;
                despues["zona_asignada"] = dto.zona_asignada;
                grupo.zona_asignada = dto.zona_asignada;
                cambios = true;
            }

            if (!cambios)
                return BadRequest(ApiResponse<string>.Fail("NoChanges", "No se detectaron cambios."));

            await _context.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Grupo",
                EntityId = id.ToString(),
                Action = "Update",
                ActorId = userId,
                Antes = antes,
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Grupo actualizado correctamente."));
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var grupo = await _context.grupos
                .Include(g => g.Recursos)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grupo == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Grupo no encontrado."));

            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var antes = new Dictionary<string, object>
            {
                { "nombre", grupo.nombre },
                { "zona_asignada", grupo.zona_asignada },
                { "Recursos", grupo.Recursos.Select(r => new
                    {
                        r.Id,
                        r.Nombre,
                        r.VoluntarioId
                    }).ToList()
                }
            };

            _context.grupos.Remove(grupo);
            await _context.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Grupo y Recursos",
                EntityId = id.ToString(),
                Action = "Delete",
                ActorId = actorId,
                Antes = antes,
                Despues = new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            var task = new GroupDeleteDTO
            {
                id_usuario = actorId,
                id_group = new List<Guid> { id }
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("group_delete_queue", task, "group_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Grupo y recursos eliminados correctamente."));
        }



    }
}
