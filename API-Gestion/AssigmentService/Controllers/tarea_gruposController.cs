using System.Collections.Generic;
using System.Security.Claims;
using AssigmentService.ApiResponse;
using AssigmentService.Data;
using AssigmentService.DTOs;
using AssigmentService.Interfaz;
using AssigmentService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssigmentService.Controllers
{
    [Route("tarea_grupos")]
    [ApiController]
    public class tarea_gruposController : Controller
    {
        private readonly IAppDBContext _context;
        private readonly RabbitMQProducer _rabbitMQProducer;
        private readonly HttpService _httpService;

        public tarea_gruposController(IAppDBContext appDBContext, RabbitMQProducer rabbitMQProducer, HttpService httpService)
        {
            _context = appDBContext;
            _rabbitMQProducer = rabbitMQProducer;
            _httpService = httpService;
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(
            Guid? tarea_id,
            Guid? grupo_id,
            Estado? estado,
            DateTime? fecha_completada_inicio,
            DateTime? fecha_completada_fin)
        {
            var query = _context.tareas_grupos.AsQueryable();

            if (tarea_id.HasValue)
                query = query.Where(tg => tg.tarea_id == tarea_id.Value);

            if (grupo_id.HasValue)
                query = query.Where(tg => tg.grupo_id == grupo_id.Value);

            if (estado.HasValue)
                query = query.Where(tg => tg.estado == estado.Value);

            if (fecha_completada_inicio.HasValue)
                query = query.Where(tg => tg.fecha_completada >= fecha_completada_inicio.Value);

            if (fecha_completada_fin.HasValue)
                query = query.Where(tg => tg.fecha_completada <= fecha_completada_fin.Value);

            var asignaciones = await query.ToListAsync();

            if (!asignaciones.Any())
                return NotFound(ApiResponse<string>.Fail("NotFound", "No hay asignaciones que cumplan los filtros."));


            var token = HttpContext.Request.Headers["Authorization"].ToString();

            var resultado = new List<TareaGrupoExtendidaDTO>();

            foreach (var asignacion in asignaciones)
            {
                // Obtener tarea
                var tareasResponse = await _httpService.GetAsync<ApiResponse<List<TareaDTO>>>(
                    $"http://apigateway:8080/tareas?id_tarea={asignacion.tarea_id}", token);

                var tarea = tareasResponse?.Success == true && tareasResponse.Data != null
                    ? tareasResponse.Data.FirstOrDefault()
                    : null;

                if (tarea == null)
                {
                    Console.WriteLine("Tarea no encontrada.");
                    return NotFound(ApiResponse<string>.Fail("NotFound", "No se encontró la tarea asociada."));
                }

                // Obtener grupo
                var gruposResponse = await _httpService.GetAsync<ApiResponse<List<GrupoDTO>>>(
                    $"http://apigateway:8080/grupos?grupoId={asignacion.grupo_id}", token);

                var grupo = gruposResponse?.Success == true && gruposResponse.Data != null
                    ? gruposResponse.Data.FirstOrDefault()
                    : null;

                if (grupo == null)
                {
                    Console.WriteLine("Grupo no encontrado.");
                    return NotFound(ApiResponse<string>.Fail("NotFound", "No se encontró el grupo asociado."));
                }


                if (tarea != null && grupo != null)
                {
                    resultado.Add(new TareaGrupoExtendidaDTO
                    {
                        TareaId = asignacion.tarea_id,
                        NombreTarea = tarea.Nombre,
                        DescripcionTarea = tarea.Descripcion,
                        UbicacionTarea = tarea.Ubicacion,
                        GravedadTarea = tarea.gravedad.ToString(),  

                        GrupoId = grupo.Id,
                        NombreGrupo = grupo.nombre,                 
                        ZonaAsignada = grupo.zonaAsignada,

                        estado = asignacion.estado.ToString(),
                        fecha_completada = asignacion.fecha_completada
                    });
                }
            }

            return Ok(ApiResponse<List<TareaGrupoExtendidaDTO>>.Ok(resultado));

        }


        [HttpPatch("tarea/{tareaId}/grupo/{grupoId}")]
        [Authorize]
        public async Task<IActionResult> ToggleEstado(Guid tareaId, Guid grupoId)
        {
            var entity = await _context.tareas_grupos
                .FirstOrDefaultAsync(tg => tg.tarea_id == tareaId && tg.grupo_id == grupoId);

            if (entity == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", $"No se encontró la asociación tarea {tareaId} y grupo {grupoId}."));

            var antes = new Dictionary<string, object>();
            var despues = new Dictionary<string, object>();

            var voluntarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            antes["estado"] = entity.estado;
            antes["fecha"] = entity.fecha_completada;


            entity.estado = entity.estado == Estado.EnProgreso ? Estado.Completada : Estado.EnProgreso;

            entity.fecha_completada = entity.estado == Estado.Completada ? DateTime.UtcNow : (DateTime?)null;

            despues["estado"] = entity.estado;
            despues["fecha"] = entity.fecha_completada;

            await _context.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Tareas_Grupo",
                EntityId = $"{grupoId}_{voluntarioId}",
                Action = "Update",
                ActorId = voluntarioId,
                Antes = antes,
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            var estadosRelacionados = await _context.tareas_grupos
                .Where(tg => tg.tarea_id == tareaId)
                .Select(tg => tg.estado)
                .ToListAsync();

            string nuevoEstado;

            if (estadosRelacionados.All(e => e == Estado.Completada))
            {
                nuevoEstado = "Completada";
            }
            else if (estadosRelacionados.Any(e => e == Estado.EnProgreso))
            {
                nuevoEstado = "EnProgreso";
            }
            else
            {
                nuevoEstado = "SinEmpezar";
            }

            var estadoDTO = new TaskUpdateDTO
            {
                tarea_id = tareaId,
                id_usuario = voluntarioId,
                estado = nuevoEstado
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("task_update_queue", estadoDTO, "assigment_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Estado de la tarea actualizado correctamente."));

        }

    }
}
