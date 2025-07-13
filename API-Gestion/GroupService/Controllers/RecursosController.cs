using System.Security.Claims;
using GroupService.ApiResponse;
using GroupService.Data;
using GroupService.DTOs;
using GroupService.Interfaz;
using GroupService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroupService.Controllers
{
    [Route("recursos")]
    [ApiController]
    public class RecursosController : ControllerBase
    {
        private readonly IAppDBContext _context;
        private readonly RabbitMQProducer _rabbitMQProducer;
        public RecursosController(IAppDBContext appDBContext, RabbitMQProducer rabbitMQProducer)
        {
            _context = appDBContext;
            _rabbitMQProducer = rabbitMQProducer;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(
            [FromQuery(Name = "nombres")] List<string>? nombres,
            Guid? voluntarioId,
            Guid? grupoId)
        {
            var query = _context.recursos.AsQueryable();

            if (nombres != null && nombres.Any())
            {
                var lowerNames = nombres
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n.Trim().ToLower())
                    .ToList();

                query = query.Where(r =>
                    lowerNames.Any(n => r.Nombre.ToLower().Contains(n))
                );
            }

            if (voluntarioId.HasValue)
                query = query.Where(r => r.VoluntarioId == voluntarioId.Value);

            if (grupoId.HasValue)
                query = query.Where(r => r.GrupoId == grupoId.Value);

            var recursos = await query.ToListAsync();

            if (!recursos.Any())
                return NotFound(ApiResponse<string>.Fail("NotFound", "No se encontraron recursos con los parámetros proporcionados."));

            var recursosDto = recursos.Select(r => new Recursos
            {
                Id = r.Id,
                Nombre = r.Nombre,
                VoluntarioId = r.VoluntarioId,
                GrupoId = r.GrupoId
            }).ToList();

            return Ok(ApiResponse<List<Recursos>>.Ok(recursosDto));
        }


        [HttpPost("{grupoId}")]
        [Authorize]
        public async Task<IActionResult> CreateMultiple(Guid grupoId, List<CrearRecursoDTO> recursosDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Usuario no identificado."));

            var grupo = await _context.grupos.FindAsync(grupoId);
            if (grupo == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Grupo no encontrado."));

            if (recursosDto == null || !recursosDto.Any())
                return BadRequest(ApiResponse<string>.Fail("BadRequest", "Debe enviar al menos un recurso."));

            var recursos = recursosDto.Select(dto => new Recursos
            {
                Id = Guid.NewGuid(),
                GrupoId = grupoId,
                Nombre = dto.Nombre,
                VoluntarioId = userId
            }).ToList();

            await _context.recursos.AddRangeAsync(recursos);
            await _context.SaveChangesAsync();

            foreach (var recurso in recursos)
            {
                var despues = new Dictionary<string, object>
                {
                    { "GrupoID", recurso.GrupoId },
                    { "Nombre Recurso", recurso.Nombre },
                    { "VoluntarioId", userId }
                };

                var evento = new BitacoraEvent
                {
                    EntityType = "Recurso",
                    EntityId = recurso.Id.ToString(),
                    Action = "Create",
                    ActorId = userId,
                    Antes = new Dictionary<string, object>(),
                    Despues = despues,
                    Timestamp = DateTime.UtcNow
                };

                await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");
            }

            return Ok(ApiResponse<string>.Ok("Recursos creados correctamente."));
        }



        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, [FromBody] PatchRecursoDTO dto)
        {
            var recurso = await _context.recursos.FindAsync(id);
            if (recurso == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Recurso no encontrado."));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Usuario no identificado."));

            if (recurso.VoluntarioId != userId)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<string>.Fail("Forbidden", "Solo puedes actualizar tus propios recursos."));

            bool seActualizoAlgo = false;
            var antes = new Dictionary<string, object>();
            var despues = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(dto.Nombre) && dto.Nombre != recurso.Nombre)
            {
                antes["Nombre Recurso"] = recurso.Nombre;
                despues["Nombre Recurso"] = dto.Nombre;
                recurso.Nombre = dto.Nombre;
                seActualizoAlgo = true;
            }

            if (!seActualizoAlgo)
                return BadRequest(ApiResponse<string>.Fail("NoChanges", "No se realizó ningún cambio."));

            await _context.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Recurso",
                EntityId = recurso.Id.ToString(),
                Action = "Update",
                ActorId = userId,
                Antes = antes,
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Recurso actualizado correctamente."));
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var recurso = await _context.recursos.FindAsync(id);
            if (recurso == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Recurso no encontrado."));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Usuario no identificado."));

            if (recurso.VoluntarioId != userId)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<string>.Fail("Forbidden", "Solo puedes eliminar tus propios recursos."));


            var antes = new Dictionary<string, object>
            {
                { "Nombre Recurso", recurso.Nombre },
                { "GrupoId", recurso.GrupoId },
                { "VoluntarioId", recurso.VoluntarioId }
            };

            _context.recursos.Remove(recurso);
            await _context.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Recurso",
                EntityId = recurso.Id.ToString(),
                Action = "Delete",
                ActorId = userId,
                Antes = antes,
                Despues = new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Recurso eliminado correctamente."));
        }


    }
}
