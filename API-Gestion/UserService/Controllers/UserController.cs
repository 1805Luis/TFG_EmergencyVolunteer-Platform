using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.ApiResponse;
using UserService.Custom;
using UserService.Data;
using UserService.DTOs;
using UserService.Interfaz;
using UserService.Service;

namespace UserService.Controllers
{

    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly IAppDBContext _context;
        private readonly RabbitMQProducer _rabbitMQProducer;
        private readonly Utilidades _utilidades;


        public UserController(IAppDBContext appDBContext, RabbitMQProducer rabbitMQProducer, Utilidades utilidades)
        {
            _context = appDBContext;
            _rabbitMQProducer = rabbitMQProducer;
            _utilidades = utilidades;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(
            string? name,
            string? telf,
            string? disponibilidad,
            [FromQuery(Name = "habilidades")] List<string>? habilidades,
            bool matchAll = false)
        {
            var query = _context.User.AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(u => u.Name != null && u.Name.Contains(name));

            if (!string.IsNullOrEmpty(telf))
                query = query.Where(u => u.Telf != null && u.Telf.Contains(telf));

            if (!string.IsNullOrEmpty(disponibilidad))
                query = query.Where(u => u.Disponibilidad != null && u.Disponibilidad.Contains(disponibilidad));

            var users = await query.ToListAsync();

            if (habilidades != null && habilidades.Any())
            {
                var habilidadesBuscadas = habilidades
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .Select(h => h.Trim().ToLower())
                    .ToList();

                users = users.Where(u =>
                {
                    var habilidadesUsuario = u.Habilidades?
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(h => h.Trim().ToLower())
                        .ToList() ?? new List<string>();

                    return matchAll
                        ? habilidadesBuscadas.All(h => habilidadesUsuario.Any(hab => hab.Contains(h)))
                        : habilidadesBuscadas.Any(h => habilidadesUsuario.Any(hab => hab.Contains(h)));
                }).ToList();
            }

            if (!users.Any())
                return NotFound(ApiResponse<string>.Fail("NotFound", "No se encontraron usuarios con los parámetros proporcionados."));

            var userDtos = users.Select(u => new UserDTO(u)).ToList();

            return Ok(ApiResponse<List<UserDTO>>.Ok(userDtos));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> Get(Guid id)
        {
            var user = await _context.User.FindAsync(id);
            if (user == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Usuario no encontrado."));

            var userDto = new UserDTO(user);
            return Ok(ApiResponse<UserDTO>.Ok(userDto));
        }


        [HttpPatch("modificar-perfil")]
        [Authorize]
        public async Task<IActionResult> ModificarPerfil(ModificarPerfilDTO nuevosDatos)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Token inválido o usuario no identificado."));

            var userId = Guid.Parse(userIdClaim.Value);
            var user = await _context.User.FindAsync(userId);

            if (user == null)
                return NotFound(ApiResponse<string>.Fail("NotFound", "Usuario no encontrado."));

            bool seActualizoAlgo = false;
            var antes = new Dictionary<string, object>();
            var despues = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(nuevosDatos.Name) && nuevosDatos.Name != user.Name)
            {
                antes["Name"] = _utilidades.CifrarAES(user.Name);
                despues["Name"] = _utilidades.CifrarAES(nuevosDatos.Name);
                user.Name = nuevosDatos.Name;
                seActualizoAlgo = true;
            }

            if (!string.IsNullOrWhiteSpace(nuevosDatos.Telf) && nuevosDatos.Telf != user.Telf)
            {
                antes["Telf"] = user.Telf;
                despues["Telf"] = nuevosDatos.Telf;
                user.Telf = nuevosDatos.Telf;
                seActualizoAlgo = true;
            }

            if (!string.IsNullOrWhiteSpace(nuevosDatos.Disponibilidad) && nuevosDatos.Disponibilidad != user.Disponibilidad)
            {
                antes["Disponibilidad"] = user.Disponibilidad;
                despues["Disponibilidad"] = nuevosDatos.Disponibilidad;
                user.Disponibilidad = nuevosDatos.Disponibilidad;
                seActualizoAlgo = true;
            }

            if (!string.IsNullOrWhiteSpace(nuevosDatos.Habilidades) && nuevosDatos.Habilidades != user.Habilidades)
            {
                antes["Habilidades"] = user.Habilidades;
                despues["Habilidades"] = nuevosDatos.Habilidades;
                user.Habilidades = nuevosDatos.Habilidades;
                seActualizoAlgo = true;
            }

            if (!seActualizoAlgo)
                return BadRequest(ApiResponse<string>.Fail("NoChanges", "No se realizó ningún cambio."));

            await _context.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "User",
                EntityId = user.Id.ToString(),
                Action = "Update",
                ActorId = user.Id,
                Antes = antes,
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

            return Ok(ApiResponse<string>.Ok("Perfil actualizado correctamente."));
        }



    }


}
