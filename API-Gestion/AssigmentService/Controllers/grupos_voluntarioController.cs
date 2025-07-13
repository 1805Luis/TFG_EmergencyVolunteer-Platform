using System.Security.Claims;
using AssigmentService.ApiResponse;
using AssigmentService.Data;
using AssigmentService.DTOs;
using AssigmentService.Interfaz;
using AssigmentService.Service;
using AuthService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssigmentService.Controllers
{
    [Route("grupos_voluntarios")]
    [ApiController]
    public class grupos_voluntarioController : ControllerBase
    {

        private readonly IAppDBContext _context;
        private readonly RabbitMQProducer _rabbitMQProducer;
        private readonly HttpService _httpService;

        public grupos_voluntarioController(IAppDBContext appDBContext, RabbitMQProducer rabbitMQProducer, HttpService httpService)
        {
            _context = appDBContext;
            _rabbitMQProducer = rabbitMQProducer;
            _httpService = httpService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMisAsignaciones(Guid? grupo_id = null, Rol? rol = null, Estado? estado = null, int page = 1, int pageSize = 50)
        {
            var voluntarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (voluntarioId == Guid.Empty)
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Identidad de usuario no válida."));
            var query = _context.grupos_voluntarios
                .Where(gv => gv.voluntario_id == voluntarioId);

            if (grupo_id.HasValue)
                query = query.Where(gv => gv.grupo_id == grupo_id.Value);

            if (rol.HasValue)
                query = query.Where(gv => gv.rol == rol.Value);

            var result = await query.ToListAsync();

            var token = HttpContext.Request.Headers["Authorization"].ToString();

            var resultado = new List<MisGrupos>();

            foreach (var asignacion in result)
            {

                var response = await _httpService.GetAsync<ApiResponse<List<GrupoDTO>>>($"http://apigateway:8080/grupos?grupoId={asignacion.grupo_id}", token);

                if (response == null || !response.Success || response.Data == null)
                {
                    Console.WriteLine("No se pudo obtener el grupo desde el servicio de grupos.");
                    return NotFound(ApiResponse<string>.Fail("NotFound", "No se encontró el grupo correspondiente."));
                }

                var grupo = response.Data.FirstOrDefault();
                if (grupo != null)
                {
                    var tareaGrupoQuery = _context.tareas_grupos
                        .Where(tg => tg.grupo_id == grupo.Id);

                    if (estado.HasValue)
                        tareaGrupoQuery = tareaGrupoQuery.Where(tg => tg.estado == estado.Value);

                    var tareaGrupo = await tareaGrupoQuery.FirstOrDefaultAsync();


                    resultado.Add(new MisGrupos
                    {
                        Grupo_Id = grupo.Id,
                        NombreGrupo = grupo.nombre,
                        Rol = asignacion.rol.ToString(),
                        Tarea_Id = tareaGrupo?.tarea_id,
                        EstadoTarea = tareaGrupo?.estado.ToString()
                    });

                    resultado = resultado
                        .Where(g =>
                            g.Grupo_Id != null &&
                            g.NombreGrupo != null &&
                            g.Rol != null &&
                            g.Tarea_Id != null &&
                            g.EstadoTarea != null
                        )
                        .ToList();
                }
            }

            var totalItems = resultado.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);



            var grupos = AplicarPaginacion(resultado, page, pageSize);

            var pagination = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return Ok(ApiResponse<List<MisGrupos>>.Ok(grupos, pagination));

        }

        private List<T> AplicarPaginacion<T>(List<T> lista, int page, int pageSize)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Max(pageSize, 1);

            return lista
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }


        [HttpGet("migrupo/{grupo_id}")]
        [Authorize]
        public async Task<IActionResult> MiGrupo(Guid grupo_id, Rol? rol = null)
        {
            var voluntarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var estaEnGrupo = await _context.grupos_voluntarios
                .AnyAsync(gv => gv.grupo_id == grupo_id && gv.voluntario_id == voluntarioId);

            if (!estaEnGrupo)
            {
                return StatusCode(403, ApiResponse<string>.Fail("Forbidden", "No perteneces a ese grupo."));

            }

            var asignacionesQuery = _context.grupos_voluntarios
                .Where(gv => gv.grupo_id == grupo_id);

            if (rol.HasValue)
                asignacionesQuery = asignacionesQuery.Where(gv => gv.rol == rol.Value);

            var asignaciones = await asignacionesQuery.ToListAsync();

            foreach (var item in asignaciones)
            {
                Console.WriteLine($"GrupoID: {item.grupo_id}, VoluntarioID: {item.voluntario_id}, Rol: {item.rol}");
            }

            var token = HttpContext.Request.Headers["Authorization"].ToString();

            var listaVoluntarios = new List<VoluntarioConRolDTO>();

            foreach (var asignacion in asignaciones)
            {

                var response = await _httpService.GetAsync<ApiResponse<UserDTO>>($"http://apigateway:8080/user/{asignacion.voluntario_id}", token);


                if (response == null || !response.Success || response.Data == null)
                {
                    return NotFound(ApiResponse<string>.Fail("NotFound", "Usuario no encontrado en el servicio de Usuarios."));
                }

                var userInfo = response.Data;

                if (userInfo != null)
                {
                    listaVoluntarios.Add(new VoluntarioConRolDTO
                    {
                        Id = userInfo.Id,
                        Nombre = userInfo.Name,
                        Rol = asignacion.rol,
                        EsMiUsuario = userInfo.Id == voluntarioId
                    });
                }
            }

            return Ok(ApiResponse<List<VoluntarioConRolDTO>>.Ok(listaVoluntarios));


        }



        [HttpPost("grupo/{grupoId}")]
        [Authorize]
        public async Task<IActionResult> AssignVoluntario(Guid grupoId, RolDTO rolSeleccionado)
        {
            if (rolSeleccionado == null)
                return BadRequest(ApiResponse<string>.Fail("Validation", "Debe enviar el rol."));

            if (!Enum.IsDefined(typeof(Rol), rolSeleccionado.Rol))
                return BadRequest(ApiResponse<string>.Fail("Validation", "El rol proporcionado no es válido."));

            if (rolSeleccionado.Rol == Rol.Lider)
                return BadRequest(ApiResponse<string>.Fail("Forbidden", "No puedes asignarte como Líder en este endpoint."));

            var grupoRelacionado = await _context.tareas_grupos.AnyAsync(tg => tg.grupo_id == grupoId);

            if (!grupoRelacionado)
                return NotFound(ApiResponse<string>.Fail("NotFound", "El grupo no está relacionado con ninguna tarea."));


            var voluntarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (voluntarioId == Guid.Empty)
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "No se pudo determinar el usuario autenticado."));

            var exists = await _context.grupos_voluntarios
                .AnyAsync(gv => gv.grupo_id == grupoId && gv.voluntario_id == voluntarioId);

            if (exists)
                return Conflict(ApiResponse<string>.Fail("Conflict", "Ya estás asignado a este grupo."));

            var nuevaAsignacion = new Grupos_Voluntarios
            {
                grupo_id = grupoId,
                voluntario_id = voluntarioId,
                rol = rolSeleccionado.Rol
            };

            _context.grupos_voluntarios.Add(nuevaAsignacion);
            await _context.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Grupos_Voluntarios",
                EntityId = $"{grupoId}_{voluntarioId}",
                Action = "Create",
                ActorId = voluntarioId,
                Antes = new Dictionary<string, object>(),
                Despues = new Dictionary<string, object>
        {
            { "grupo_id", grupoId },
            { "voluntario_id", voluntarioId },
            { "rol", rolSeleccionado.Rol }
        },
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Asignación creada exitosamente"));
        }


        [HttpPatch("mirol/grupo/{grupoId}")]
        [Authorize]
        public async Task<IActionResult> UpdateMyRol(Guid grupoId, RolDTO nuevoRol)
        {
            if (nuevoRol == null)
                return BadRequest(ApiResponse<string>.Fail("Validation", "Debe enviar el rol."));

            if (!Enum.IsDefined(typeof(Rol), nuevoRol.Rol))
                return BadRequest(ApiResponse<string>.Fail("Validation", "El rol proporcionado no es válido."));

            if (nuevoRol.Rol == Rol.Lider)
                return BadRequest(ApiResponse<string>.Fail("Forbidden", "No puedes asignarte como Líder."));

            var voluntarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (voluntarioId == Guid.Empty)
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "No se pudo identificar el usuario."));

            var asignacion = await _context.grupos_voluntarios
                .FirstOrDefaultAsync(gv => gv.grupo_id == grupoId && gv.voluntario_id == voluntarioId);

            if (asignacion == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "No estás asignado a ese grupo."));

            if (nuevoRol.Rol == asignacion.rol)
                return BadRequest(ApiResponse<string>.Fail("Validation", "Ya tienes ese rol asignado."));

            var antes = new Dictionary<string, object> { ["rol"] = asignacion.rol.ToString() };
            var despues = new Dictionary<string, object> { ["rol"] = nuevoRol.Rol.ToString() };

            asignacion.rol = nuevoRol.Rol;
            await _context.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Grupos_Voluntarios",
                EntityId = $"{grupoId}_{voluntarioId}",
                Action = "Update",
                ActorId = voluntarioId,
                Antes = antes,
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Rol actualizado correctamente."));
        }


        [HttpPatch("rol/grupo/{grupoId}/voluntario/{voluntarioId}")]
        [Authorize]
        public async Task<IActionResult> UpdateRolMiembro(Guid grupoId, Guid voluntarioId, RolDTO nuevoRol)
        {
            if (nuevoRol == null)
                return BadRequest(ApiResponse<string>.Fail("Validation", "Debe enviar el rol."));

            if (!Enum.IsDefined(typeof(Rol), nuevoRol.Rol))
                return BadRequest(ApiResponse<string>.Fail("Validation", "El rol proporcionado no es válido."));

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "No se pudo determinar el usuario."));

            var lider = await _context.grupos_voluntarios
                .FirstOrDefaultAsync(gv => gv.grupo_id == grupoId && gv.voluntario_id == userId);

            if (lider == null || lider.rol != Rol.Lider)
                return StatusCode(403, ApiResponse<string>.Fail("Forbidden", "Solo el líder puede cambiar roles de otros miembros."));

            var miembro = await _context.grupos_voluntarios
                .FirstOrDefaultAsync(gv => gv.grupo_id == grupoId && gv.voluntario_id == voluntarioId);

            if (miembro == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Voluntario no asignado a ese grupo."));

            var antes = new Dictionary<string, object> { ["rol"] = miembro.rol };
            var despues = new Dictionary<string, object> { ["rol"] = nuevoRol.Rol };

            miembro.rol = nuevoRol.Rol;
            await _context.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Grupos_Voluntarios",
                EntityId = $"{grupoId}_{voluntarioId}",
                Action = "Update",
                ActorId = userId,
                Antes = antes,
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Rol actualizado correctamente."));
        }



        [HttpDelete("grupo/{grupoId}")]
        [Authorize]
        public async Task<IActionResult> RemoveMeFromGroup(Guid grupoId)
        {
            var voluntarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (voluntarioId == Guid.Empty)
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Identidad de usuario no válida."));

            var asignacion = await _context.grupos_voluntarios
                .FirstOrDefaultAsync(gv => gv.grupo_id == grupoId && gv.voluntario_id == voluntarioId);

            if (asignacion == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "No estás asignado a ese grupo."));

            var eventos = new List<BitacoraEvent>();
            var totalVoluntarios = await _context.grupos_voluntarios.CountAsync(gv => gv.grupo_id == grupoId);
            bool soyLider = asignacion.rol == Rol.Lider;
            var tipoEliminacion = totalVoluntarios <= 1 ? TipoEliminacion.EliminarGrupos : TipoEliminacion.EliminarRecursos;

            var deletegroup = new GroupDeleteDTO
            {
                id_usuario = voluntarioId,
                id_group = new List<Guid> { grupoId },
                tipoEliminacion = tipoEliminacion
            };

            if (tipoEliminacion == TipoEliminacion.EliminarGrupos)
            {
                await _rabbitMQProducer.SendMessageToQueueAsync("group_unsubscribe_queue", deletegroup, "task_ServiceConnection");

                var tareasDelGrupo = await _context.tareas_grupos
                    .Where(tg => tg.grupo_id == grupoId)
                    .ToListAsync();

                if (tareasDelGrupo.Count == 1)
                {
                    var tareaId = tareasDelGrupo[0].tarea_id;
                    var cantidadGrupos = await _context.tareas_grupos.CountAsync(tg => tg.tarea_id == tareaId);

                    if (cantidadGrupos <= 1)
                    {
                        var estadoDTO = new TaskUpdateDTO
                        {
                            tarea_id = tareaId,
                            id_usuario = voluntarioId,
                            estado = "SinEmpezar"
                        };

                        await _rabbitMQProducer.SendMessageToQueueAsync("task_update_queue", estadoDTO, "assigment_ServiceConnection");

                        eventos.Add(new BitacoraEvent
                        {
                            EntityType = "Grupos_Voluntarios",
                            EntityId = $"{grupoId}_{voluntarioId}",
                            Action = "Delete",
                            ActorId = voluntarioId,
                            Antes = new Dictionary<string, object>
                            {
                                { "tarea_id", tareaId },
                                { "grupoId", grupoId },
                                { "estado", tareasDelGrupo[0].estado.ToString() }
                            },
                            Despues = new Dictionary<string, object>(),
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }

                _context.tareas_grupos.RemoveRange(tareasDelGrupo);
            }
            else
            {
                if (soyLider)
                {
                    return BadRequest(ApiResponse<string>.Fail("Leadership", "Antes de darte de baja, debes asignar otro líder al grupo."));
                }

                await _rabbitMQProducer.SendMessageToQueueAsync("group_unsubscribe_queue", deletegroup, "task_ServiceConnection");
            }

            var antes = new Dictionary<string, object>
            {
                { "grupo_id", grupoId },
                { "rol", asignacion.rol }
            };

            _context.grupos_voluntarios.Remove(asignacion);
            await _context.SaveChangesAsync();

            eventos.Add(new BitacoraEvent
            {
                EntityType = "Grupos_Voluntarios",
                EntityId = $"{grupoId}_{voluntarioId}",
                Action = "Delete",
                ActorId = voluntarioId,
                Antes = antes,
                Despues = new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            });

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", eventos, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Te has dado de baja del grupo correctamente."));
        }


    }
}
