using System.Security.Claims;
using BitacoraService.ApiResponse;
using BitacoraService.Data;
using BitacoraService.Interfaz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BitacoraService.Controllers
{
    [ApiController]
    [Route("audit")]
    public class BitacoraController : Controller
    {
        private readonly IAppDBContext _context;

        public BitacoraController(IAppDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Security_Ops")]
        public async Task<IActionResult> ObtenerEventos(
            [FromQuery] string? entityType = null,
            [FromQuery] string? entityId = null,
            [FromQuery] Guid? actorId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] Action_Evento? action = null,
            [FromQuery] int? latestCount = null)
        {
            var idUsuario = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (!string.IsNullOrEmpty(entityId) && string.IsNullOrEmpty(entityType))
            {

                return BadRequest(ApiResponse<string>.Fail("Validation", "Para filtrar por entityId debe especificar también entityType."));
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                startDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                var now = DateTime.UtcNow.Date;

                if (endDate.Value.Date == now)
                {
                    // Si la fecha de fin es hoy, ponemos la hora actual como límite
                    endDate = DateTime.UtcNow;
                }
                else
                {
                    endDate = DateTime.SpecifyKind(endDate.Value.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                }


                if (startDate > endDate)
                {
                    return BadRequest(ApiResponse<string>.Fail("Validation", "La fecha de inicio no puede ser mayor que la fecha de fin."));
                }

                if (startDate > DateTime.UtcNow || endDate > DateTime.UtcNow)
                {
                    return BadRequest(ApiResponse<string>.Fail("Validation", "Las fechas no pueden ser futuras."));

                }

            }


            if (latestCount.HasValue && latestCount.Value > 0)
            {
                int count = Math.Clamp(latestCount.Value, 1, 500);
                var evento = await _context.Evento
                    .OrderByDescending(e => e.Timestamp)
                    .Take(count)
                    .ToListAsync();

                await RegistrarAcceso("Consulta Completa", idUsuario);

                return Ok(ApiResponse<List<Evento>>.Ok(evento));

            }

            var query = ConstruirQueryFiltros(entityType, entityId, actorId, startDate, endDate, action);
            var totalItems = await query.CountAsync();
            var eventos = await AplicarPaginacion(query, page, pageSize).ToListAsync();

            var pagination = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            if (actorId.HasValue && !eventos.Any())
                return NotFound(ApiResponse<string>.Fail("NotFound", "No se encontraron eventos para el usuario especificado."));

            var tipoConsulta = (entityType != null || entityId != null || actorId != null || startDate != null || endDate != null)
                    ? "Consulta Filtrada" : "Consulta Completa";

            await RegistrarAcceso(tipoConsulta, idUsuario);

            return Ok(ApiResponse<List<Evento>>.Ok(eventos,pagination));



        }

        private IQueryable<Evento> ConstruirQueryFiltros(string? entityType, string? entityId, Guid? actorId, DateTime? startDate, DateTime? endDate, Action_Evento? action)
        {
            var query = _context.Evento.AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(e => e.EntityType == entityType);

            if (!string.IsNullOrEmpty(entityId))
                query = query.Where(e => e.Payload != null && e.Payload.Id == entityId);

            if (actorId.HasValue)
                query = query.Where(e => e.ActorId == actorId.Value);

            if (startDate.HasValue)
                query = query.Where(e => e.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.Timestamp <= endDate.Value);

            if (action.HasValue)
                query = query.Where(e => e.Action == action.Value);


            return query;
        }

        private async Task RegistrarAcceso(string operation, Guid idUsuario)
        {
            Console.WriteLine($"Registrando acceso: {operation}");
            Console.WriteLine($"UserId desde claims: {idUsuario}");

            var acceso = new Evento
            {
                Id = Guid.NewGuid(),
                ActorId = idUsuario,
                EntityType = "Bitacora",
                Action = Action_Evento.Access,
                Timestamp = DateTime.UtcNow,
                Payload = new Payload
                {
                    Id = "N/A",
                    Antes = null,
                    Despues = null
                },
            
                Descripcion = $"Acceso a consulta de auditoría: {operation}"
            };

            try
            {
                _context.Evento.Add(acceso);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando acceso: {ex.Message}");
            }
        }



        private IQueryable<Evento> AplicarPaginacion(IQueryable<Evento> query, int page, int pageSize)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Max(pageSize, 1);

            return query
                .OrderByDescending(e => e.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
        }
    }
}
