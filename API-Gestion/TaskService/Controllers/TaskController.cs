using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskService.ApiResponse;
using TaskService.Data;
using TaskService.DTOs;
using TaskService.Interfaz;
using TaskService.Service;

namespace TaskService.Controllers
{
    [Route("tareas")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly IAppDBContext _context;
        private readonly RabbitMQProducer _rabbitMQProducer;
        public TaskController(IAppDBContext appDBContext, RabbitMQProducer rabbitMQProducer) {
            _context = appDBContext;
            _rabbitMQProducer = rabbitMQProducer;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(
            string? estado,
            string? gravedad,
            Guid? emergencia_id,
            Guid? id_tarea,
            string? nombre,
            string? ubicacion,
            string? descripcion,
            [FromQuery(Name = "habilidadesMin")] List<string>? habilidadesMin,
            bool matchAll = false)
        {
            var query = _context.Tareas.AsQueryable();

            if (!string.IsNullOrEmpty(estado) && Enum.TryParse<Estado>(estado, true, out var estadoEnum))
                query = query.Where(t => t.estado == estadoEnum);

            if (!string.IsNullOrEmpty(gravedad) && Enum.TryParse<Gravedad>(gravedad, true, out var gravedadEnum))
                query = query.Where(t => t.gravedad == gravedadEnum);

            if (id_tarea.HasValue)
                query = query.Where(t => t.Id == id_tarea.Value);

            if (emergencia_id.HasValue)
                query = query.Where(t => t.emergencia_id == emergencia_id.Value);

            if (!string.IsNullOrWhiteSpace(nombre))
                query = query.Where(t => t.Nombre.ToLower().Contains(nombre.ToLower()));

            if (!string.IsNullOrWhiteSpace(ubicacion))
                query = query.Where(t => t.Ubicacion.ToLower().Contains(ubicacion.ToLower()));

            if (!string.IsNullOrWhiteSpace(descripcion))
                query = query.Where(t => t.Descripcion.ToLower().Contains(descripcion.ToLower()));

            var tareas = await query.ToListAsync();

            if (habilidadesMin != null && habilidadesMin.Any())
            {
                var habilidadesBuscadas = habilidadesMin
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .Select(h => h.Trim().ToLower())
                    .ToList();

                tareas = tareas.Where(t =>
                {
                    var habilidadesTarea = t.HabilidadesMin?
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(h => h.ToLower())
                        .ToList() ?? new List<string>();

                    return matchAll
                        ? habilidadesBuscadas.All(h => habilidadesTarea.Any(hab => hab.Contains(h)))
                        : habilidadesBuscadas.Any(h => habilidadesTarea.Any(hab => hab.Contains(h)));
                }).ToList();
            }

            if (!tareas.Any())
                return NotFound(ApiResponse<string>.Fail("NotFound", "No se encontraron tareas con los parámetros proporcionados."));

            var tareasDto = tareas.Select(t => new TareaDTO
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Ubicacion = t.Ubicacion,
                Descripcion = t.Descripcion,
                gravedad = t.gravedad.ToString(),
                estado = t.estado.ToString(),
                emergencia_id = t.emergencia_id,
                HabilidadesMin = t.HabilidadesMin
            }).ToList();

            return Ok(ApiResponse<List<TareaDTO>>.Ok(tareasDto));
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, ActualizarTareaDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Token inválido o usuario no identificado."));

            var tarea = await _context.Tareas.FindAsync(id);
            if (tarea == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Tarea no encontrada."));

            var userId = Guid.Parse(userIdClaim.Value);

            bool seActualizoAlgo = false;
            var antes = new Dictionary<string, object>();
            var despues = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(dto.Nombre) && dto.Nombre != tarea.Nombre)
            {
                antes["Nombre"] = tarea.Nombre;
                despues["Nombre"] = dto.Nombre;
                tarea.Nombre = dto.Nombre;
                seActualizoAlgo = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Ubicacion) && dto.Ubicacion != tarea.Ubicacion)
            {
                antes["Ubicacion"] = tarea.Ubicacion;
                despues["Ubicacion"] = dto.Ubicacion;
                tarea.Ubicacion = dto.Ubicacion;
                seActualizoAlgo = true;
            }

            if (!string.IsNullOrEmpty(dto.gravedad) &&
                Enum.TryParse<Gravedad>(dto.gravedad, true, out var gravedad) &&
                tarea.gravedad != gravedad)
            {
                antes["Gravedad"] = tarea.gravedad.ToString();
                despues["Gravedad"] = gravedad.ToString();
                tarea.gravedad = gravedad;
                seActualizoAlgo = true;
            }

            if (!string.IsNullOrEmpty(dto.estado) &&
                Enum.TryParse<Estado>(dto.estado, true, out var estado) &&
                tarea.estado != estado)
            {
                antes["Estado"] = tarea.estado.ToString();
                despues["Estado"] = estado.ToString();
                tarea.estado = estado;
                seActualizoAlgo = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.HabilidadesMin) &&
                dto.HabilidadesMin != tarea.HabilidadesMin)
            {
                antes["HabilidadesMin"] = tarea.HabilidadesMin;
                despues["HabilidadesMin"] = dto.HabilidadesMin;
                tarea.HabilidadesMin = dto.HabilidadesMin;
                seActualizoAlgo = true;
            }

            if (!seActualizoAlgo)
                return BadRequest(ApiResponse<string>.Fail("NoChanges", "No se realizó ningún cambio."));

            await _context.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Tarea",
                EntityId = id.ToString(),
                Action = "Update",
                ActorId = userId,
                Antes = antes,
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Tarea actualizada correctamente."));
        }

        [HttpPost("{emergencia_id}")]
        [Authorize]
        public async Task<IActionResult> Create(Guid emergencia_id, CrearTareaDTO dto)
        {
            if (!Enum.TryParse<Gravedad>(dto.gravedad, true, out var gravedadEnum))
                return BadRequest(ApiResponse<string>.Fail("Validation", "Gravedad inválida."));

            var nuevaTarea = new Tarea
            {
                Id = Guid.NewGuid(),
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Ubicacion = dto.Ubicacion,
                emergencia_id = emergencia_id,
                gravedad = gravedadEnum,
                estado = Estado.SinEmpezar,
                HabilidadesMin = dto.HabilidadesMin
            };

            await _context.Tareas.AddAsync(nuevaTarea);
            await _context.SaveChangesAsync();

            var despues = new Dictionary<string, object>
            {
                { "Nombre", nuevaTarea.Nombre },
                { "Descripcion", nuevaTarea.Descripcion },
                { "EmergenciaId", nuevaTarea.emergencia_id },
                { "Estado", nuevaTarea.estado.ToString() },
                { "Gravedad", nuevaTarea.gravedad.ToString() },
                { "HabilidadesMin", nuevaTarea.HabilidadesMin }
            };

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            Guid actorId = Guid.TryParse(userIdClaim?.Value, out var parsedId) ? parsedId : Guid.Empty;

            var evento = new BitacoraEvent
            {
                EntityType = "Tarea",
                EntityId = nuevaTarea.Id.ToString(),
                Action = "Create",
                ActorId = actorId,
                Antes = new Dictionary<string, object>(),
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Tarea creada correctamente."));
        }



        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tarea = await _context.Tareas.FindAsync(id);
            if (tarea == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Tarea no encontrada."));

            var antes = new Dictionary<string, object>
            {
                { "Nombre", tarea.Nombre },
                { "Descripcion", tarea.Descripcion },
                { "Ubicación", tarea.Ubicacion },
                { "EmergenciaId", tarea.emergencia_id },
                { "Estado", tarea.estado.ToString() },
                { "Gravedad", tarea.gravedad.ToString() },
                { "HabilidadesMin", tarea.HabilidadesMin }
            };

            _context.Tareas.Remove(tarea);
            await _context.SaveChangesAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            Guid userId = Guid.TryParse(userIdClaim?.Value, out var parsedId) ? parsedId : Guid.Empty;

            var evento = new BitacoraEvent
            {
                EntityType = "Tarea",
                EntityId = tarea.Id.ToString(),
                Action = "Delete",
                ActorId = userId,
                Antes = antes,
                Despues = new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            var task = new TaskDeleteDTO
            {
                id_Usuario = userId,
                id_task = new List<Guid> { id }
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("task_delete_queue", task, "task_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Tarea eliminada con éxito."));
        }

    }
}
