using Autenticacion.Service;
using System.Security.Claims;
using AuthService.Custom;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Interfaz;
using AuthService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using AuthService.Config;
using System.Data;
using AuthService.ApiResponse;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAppDBContext _dbContext;
        private readonly Utilidades _utilidades;
        private readonly RabbitMQProducer _rabbitMQProducer;
        private readonly HttpService _httpService;


        public AuthController(IAppDBContext dbContext, Utilidades utilidades, RabbitMQProducer rabbitMQProducer, HttpService httpService)
        {
            _dbContext = dbContext;
            _utilidades = utilidades;
            _rabbitMQProducer = rabbitMQProducer;
            _httpService = httpService;
        }

        [HttpGet]
        [Route("me")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Token inválido."));
            }

            Console.WriteLine("El user id es: " + userId);

            var authUser = await _dbContext.Auth_User
                .Where(u => u.Id.ToString() == userId)
                .FirstOrDefaultAsync();

            if (authUser == null)
            {
                return NotFound(ApiResponse<string>.Fail("NotFound", "Usuario no encontrado en el servicio de autenticación."));
            }

            var token = HttpContext.Request.Headers["Authorization"].ToString();

            // Cambia la llamada para que reciba ApiResponse<UserDTO>
            var response = await _httpService.GetAsync<ApiResponse<UserDTO>>($"http://nginx-balanceador:80/user/{userId}", token);


            if (response == null || !response.Success || response.Data == null)
            {
                return NotFound(ApiResponse<string>.Fail("NotFound", "Usuario no encontrado en el servicio de Usuarios."));
            }

            var userInfo = response.Data;


            var usuarioDTO = new UserInfoDTO
            {
                Correo = _utilidades.DescifrarAES(authUser.Email),
                Name = userInfo.Name,
                Telf = userInfo.Telf,
                Disponibilidad = userInfo.Disponibilidad,
                Habilidades = userInfo.Habilidades
            };

            return Ok(ApiResponse<UserInfoDTO>.Ok(usuarioDTO));
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(LoginDTO userlogin)
        {
            var userFind = await _dbContext.Auth_User
                .FirstOrDefaultAsync(u => u.Email == _utilidades.CifrarAES(userlogin.Correo));

            if (userFind == null || !_utilidades.VerificarHash(userlogin.Clave, userFind.ClaveSalt, userFind.ClaveHash))
            {
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Credenciales incorrectas."));
            }

            if (userFind.Estado != Estado_User.activo)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<string>.Fail("Forbidden", "Usuario no autorizado o suspendido."));
            }

            var refreshToken = _utilidades.GenerateRefreshToken();
            userFind.RefreshToken = _utilidades.HashConSaltOpcional(refreshToken);
            userFind.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);

            await _dbContext.SaveChangesAsync();

            var token = _utilidades.generarJWT(new TokenDTO
            {
                Id = userFind.Id,
                Rol = userFind.Rol.ToString()
            }, 15);

            var respuesta = new TokenResponseDTO
            {
                Token = token,
                RefreshToken = refreshToken
            };

            return Ok(ApiResponse<TokenResponseDTO>.Ok(respuesta));
        }


        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(ApiResponse<string>.Fail("Validation", "Refresh token requerido."));

            var hashRefreshToken = _utilidades.HashConSaltOpcional(request.RefreshToken);

            var user = await _dbContext.Auth_User
                .FirstOrDefaultAsync(u => u.RefreshToken == hashRefreshToken);

            if (user == null)
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Refresh token inválido."));

            if (!user.RefreshTokenExpiry.HasValue || user.RefreshTokenExpiry.Value < DateTime.UtcNow)
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Refresh token expirado."));

            if (user.Estado != Estado_User.activo)
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<string>.Fail("Forbidden", "Usuario inactivo o suspendido."));

            var nuevoToken = _utilidades.generarJWT(
                new TokenDTO { Id = user.Id, Rol = user.Rol.ToString() }, 15);

            var resultado = new TokenResponseDTO
            {
                Token = nuevoToken
            };

            return Ok(ApiResponse<TokenResponseDTO>.Ok(resultado));
        }

        [HttpPatch("cambio-credenciales")]
        [Authorize]
        public async Task<IActionResult> CambiarCredenciales(LoginDTO credenciales)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Token inválido o usuario no identificado."));
            }

            var userId = Guid.Parse(userIdClaim.Value);
            var user = await _dbContext.Auth_User.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(ApiResponse<string>.Fail("NotFound", "Usuario no encontrado."));
            }

            bool seActualizoAlgo = false;
            Dictionary<string, object> antes = new();
            Dictionary<string, object> despues = new();

            if (!string.IsNullOrWhiteSpace(credenciales.Correo))
            {
                if (!credenciales.Correo.Contains("@"))
                {
                    return BadRequest(ApiResponse<string>.Fail("Validation", "Correo inválido."));
                }

                var correoEnUso = await _dbContext.Auth_User
                    .AnyAsync(u => u.Email == _utilidades.CifrarAES(credenciales.Correo) && u.Id != userId);

                if (correoEnUso)
                {
                    return Conflict(ApiResponse<string>.Fail("Conflict", "El correo ya está registrado por otro usuario."));
                }

                if (credenciales.Correo != _utilidades.DescifrarAES(user.Email))
                {
                    antes["email"] = _utilidades.CifrarAES(user.Email);
                    despues["email"] = _utilidades.CifrarAES(credenciales.Correo);
                    user.Email = _utilidades.CifrarAES(credenciales.Correo);
                    seActualizoAlgo = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(credenciales.Clave))
            {
                if (credenciales.Clave.Length < ReglasSeguridad.LongitudMinimaContrasena)
                {
                    return BadRequest(ApiResponse<string>.Fail("Validation", $"La clave debe tener al menos {ReglasSeguridad.LongitudMinimaContrasena} caracteres."));
                }

                string saltNueva = _utilidades.GenerarSalt();
                string hashNuevo = _utilidades.HashConSaltOpcional(credenciales.Clave, saltNueva);
                user.ClaveSalt = saltNueva;
                user.ClaveHash = hashNuevo;
                seActualizoAlgo = true;
            }

            if (!seActualizoAlgo)
            {
                return BadRequest(ApiResponse<string>.Fail("Validation", "No se proporcionó ningún dato para actualizar."));
            }

            await _dbContext.SaveChangesAsync();

            if (antes.Count > 0 && despues.Count > 0)
            {
                var evento = new BitacoraEvent
                {
                    EntityType = "Auth y Usuario",
                    EntityId = user.Id.ToString(),
                    Action = "Update",
                    ActorId = user.Id,
                    Antes = antes,
                    Despues = despues,
                    Timestamp = DateTime.UtcNow
                };

                await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection", false);
            }

            return Ok(ApiResponse<string>.Ok("Credenciales actualizadas correctamente."));
        }

        [HttpDelete("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Token inválido o usuario no identificado."));
            }

            var userId = Guid.Parse(userIdClaim.Value);

            var user = await _dbContext.Auth_User.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.Fail("NotFound", "Usuario no encontrado."));
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;

            await _dbContext.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("Sesión cerrada correctamente."));
        }

        [HttpDelete("delete")]
        [Authorize]
        public async Task<IActionResult> DeleteMe()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Token inválido o usuario no identificado."));
            }

            var userId = Guid.Parse(userIdClaim);

            var user = await _dbContext.Auth_User.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.Fail("NotFound", "Usuario no encontrado."));
            }

            if (user.Rol == Rol_User.Security_Ops)
            {
                var securityOpsCount = await _dbContext.Auth_User.CountAsync(u => u.Rol == Rol_User.Security_Ops);
                if (securityOpsCount <= 1)
                {
                    return BadRequest(ApiResponse<string>.Fail("Constraint", "No se puede eliminar al último usuario con rol Security_Ops."));
                }
            }

            var evento = new BitacoraEvent
            {
                EntityType = "Auth y Usuario",
                EntityId = user.Id.ToString(),
                Action = "Delete",
                ActorId = user.Id,
                Antes = new Dictionary<string, object>
                {
                    { "email", _utilidades.CifrarAES(user.Email) }
                },
                        Despues = new Dictionary<string, object>(),
                        Timestamp = DateTime.UtcNow
            };

            _dbContext.Auth_User.Remove(user);
            await _dbContext.SaveChangesAsync();

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection", false);

            var usurDelete = new UserDeleteDTO
            {
                id_usuario = user.Id
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("user_delete_broadcast", usurDelete, "auth_ServiceConnection", true);

            return Ok(ApiResponse<string>.Ok("Usuario eliminado correctamente."));
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Registro(UserRegistrationDto userAuth)
        {
            var existingUser = await _dbContext.Auth_User
                .FirstOrDefaultAsync(u => u.Email == _utilidades.CifrarAES(userAuth.Correo));

            if (existingUser != null)
            {
                return BadRequest(ApiResponse<string>.Fail("Validation", "El correo ya está en uso."));
            }

            string salt = _utilidades.GenerarSalt();
            string claveHash = _utilidades.HashConSaltOpcional(userAuth.Clave, salt);
            var refreshToken = _utilidades.GenerateRefreshToken();

            var user = new Auth_User
            {
                Id = Guid.NewGuid(),
                Email = _utilidades.CifrarAES(userAuth.Correo),
                ClaveHash = claveHash,
                ClaveSalt = salt,
                Rol = Rol_User.Voluntario,
                Estado = Estado_User.activo,
                RefreshToken = _utilidades.HashConSaltOpcional(refreshToken),
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(30)
            };

            _dbContext.Auth_User.Add(user);
            await _dbContext.SaveChangesAsync();

            var userDto = new UserDTO
            {
                Id = user.Id,
                Name = userAuth.Nombre,
                Telf = userAuth.Telf,
                Disponibilidad = userAuth.Disponibilidad,
                Habilidades = userAuth.Habilidades
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("user_create_queue", userDto, "auth_ServiceConnection", false);

            var evento = new BitacoraEvent
            {
                EntityType = "Auth y Usuario",
                EntityId = user.Id.ToString(),
                Action = "Create",
                ActorId = user.Id,
                Antes = new Dictionary<string, object>(),
                Despues = new Dictionary<string, object>
        {
            { "email", _utilidades.CifrarAES(user.Email) }
        },
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection", false);

            var tokenDto = new TokenDTO
            {
                Id = user.Id,
                Rol = user.Rol.ToString()
            };

            var response = new TokenResponseDTO
            {
                Token = _utilidades.generarJWT(tokenDto, 15),
                RefreshToken = refreshToken
            };

            return Ok(ApiResponse<TokenResponseDTO>.Ok(response));
        }

        [HttpPatch("cambiarRol")]
        [Authorize(Roles = "Security_Ops")]
        public async Task<IActionResult> CambiarRolUsuario(CambioRolDTO dto)
        {
            var actorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var rolSolicitante = User.FindFirstValue(ClaimTypes.Role);

            if (actorIdClaim == null || rolSolicitante == null)
            {
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Token inválido."));
            }

            if (dto.UsuarioId == Guid.Empty || string.IsNullOrWhiteSpace(dto.NuevoRol))
            {
                return BadRequest(ApiResponse<string>.Fail("Validation", "Debe proporcionarse un ID de usuario y un nuevo rol."));
            }

            if (!Enum.TryParse<Rol_User>(dto.NuevoRol, ignoreCase: true, out var nuevoRol))
            {
                return BadRequest(ApiResponse<string>.Fail("Validation", "Rol inválido. Valores válidos: Voluntario, Security_Ops."));
            }

            var user = await _dbContext.Auth_User.FirstOrDefaultAsync(u => u.Id == dto.UsuarioId);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.Fail("NotFound", "Usuario no encontrado."));
            }

            if (user.Rol == nuevoRol)
            {
                return BadRequest(ApiResponse<string>.Fail("Validation", "El usuario ya tiene ese rol asignado."));
            }

            var antes = new Dictionary<string, object> { { "rol", user.Rol.ToString() } };
            user.Rol = nuevoRol;
            var despues = new Dictionary<string, object> { { "rol", user.Rol.ToString() } };

            await _dbContext.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Auth",
                EntityId = user.Id.ToString(),
                Action = "Update",
                ActorId = Guid.Parse(actorIdClaim),
                Antes = antes,
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection", false);

            return Ok(ApiResponse<string>.Ok($"Rol de usuario actualizado a {nuevoRol} correctamente."));
        }


        [HttpPatch("estado/{id}")]
        [Authorize(Roles = "Security_Ops")]
        public async Task<IActionResult> ToggleEstado(Guid id)
        {
            var actorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (actorIdClaim == null || !Guid.TryParse(actorIdClaim, out var actorId))
            {
                return Unauthorized(ApiResponse<string>.Fail("Unauthorized", "Token inválido o usuario no identificado."));
            }

            var user = await _dbContext.Auth_User.FindAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.Fail("NotFound", "Usuario no encontrado."));
            }

            var antes = new Dictionary<string, object> { { "estado", user.Estado.ToString() } };

            user.Estado = user.Estado == Estado_User.activo
                ? Estado_User.suspendido
                : Estado_User.activo;

            var despues = new Dictionary<string, object> { { "estado", user.Estado.ToString() } };

            await _dbContext.SaveChangesAsync();

            var evento = new BitacoraEvent
            {
                EntityType = "Auth",
                EntityId = user.Id.ToString(),
                Action = "Update",
                ActorId = actorId,
                Antes = antes,
                Despues = despues,
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection", false);

            return Ok(ApiResponse<string>.Ok($"Estado actualizado correctamente. Nuevo estado: {user.Estado}."));
        }

        [HttpGet]
        [Route("usuarios-simples")]
        [Authorize(Roles = "Security_Ops")]
        public async Task<IActionResult> GetAuthUsersFiltered(
             [FromQuery] Guid? id,
             [FromQuery] Rol_User? rol,
             [FromQuery] Estado_User? estado,
             [FromQuery] string? email,
             [FromQuery] string? ordenEstado,
             [FromQuery] int page = 1,
             [FromQuery] int pageSize = 50
         )
        {
            var query = _dbContext.Auth_User.AsQueryable();

            if (id.HasValue)
                query = query.Where(u => u.Id == id.Value);

            if (rol.HasValue)
                query = query.Where(u => u.Rol == rol.Value);

            if (estado.HasValue)
                query = query.Where(u => u.Estado == estado.Value);

            // Caso especial: búsqueda parcial por email descifrado
            if (!string.IsNullOrWhiteSpace(email))
            {
                var todos = await query.ToListAsync();

                var filtrados = todos
                    .Where(u => _utilidades.DescifrarAES(u.Email)
                        .Contains(email, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!string.IsNullOrEmpty(ordenEstado))
                {
                    filtrados = ordenEstado.ToLower() == "asc"
                        ? filtrados.OrderBy(u => u.Estado).ToList()
                        : filtrados.OrderByDescending(u => u.Estado).ToList();
                }

                var totalItems = filtrados.Count;

                var resultadoPaginado = filtrados
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new AuthUserSimpleDTO
                    {
                        Id = u.Id,
                        Email = _utilidades.DescifrarAES(u.Email),
                        Rol = u.Rol.ToString(),
                        Estado = u.Estado.ToString()
                    })
                    .ToList();

                var pagination = new PaginationMetadata
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                };

                return Ok(ApiResponse<List<AuthUserSimpleDTO>>.Ok(resultadoPaginado, pagination));
            }

            // Si no hay búsqueda por email => se puede optimizar con LINQ puro
            if (!string.IsNullOrEmpty(ordenEstado))
            {
                query = ordenEstado.ToLower() == "asc"
                    ? query.OrderBy(u => u.Estado)
                    : query.OrderByDescending(u => u.Estado);
            }

            var total = await query.CountAsync();

            var paged = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var resultado = paged
                .Select(u => new AuthUserSimpleDTO
                {
                    Id = u.Id,
                    Email = _utilidades.DescifrarAES(u.Email),
                    Rol = u.Rol.ToString(),
                    Estado = u.Estado.ToString()
                })
                .ToList();

            var paginationFinal = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            };

            return Ok(ApiResponse<List<AuthUserSimpleDTO>>.Ok(resultado, paginationFinal));
        }




    }

}
