using System.Net;
using GroupService.ApiResponse;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GroupService.Middleware
{
    public class ExceptionHandlingMiddleware
    {

        private readonly RequestDelegate _next; // Middleware siguiente en la cadena

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next; // Inyectamos el siguiente middleware
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                // Ejecutamos el siguiente middleware en la tubería
                await _next(context);
            }
            catch (Exception ex)
            {
                // Si hay excepción, la manejamos aquí
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json"; // La respuesta será JSON

            // Código HTTP por defecto: 500 Internal Server Error
            var statusCode = HttpStatusCode.InternalServerError;
            string mensaje = "Ocurrió un error inesperado"; // Mensaje genérico

            switch (exception)
            {
                case ArgumentNullException argNullEx:
                    // Parámetro obligatorio faltante
                    statusCode = HttpStatusCode.BadRequest; // 400
                    mensaje = "Falta un parámetro obligatorio: " + argNullEx.ParamName;
                    break;

                case ArgumentException argEx:
                    // Argumento inválido en la petición
                    statusCode = HttpStatusCode.BadRequest; // 400
                    mensaje = "Solicitud inválida: " + argEx.Message;
                    break;

                case KeyNotFoundException:
                    // No se encontró el recurso solicitado
                    statusCode = HttpStatusCode.NotFound; // 404
                    mensaje = "Recurso no encontrado: " + exception.Message;
                    break;

                case UnauthorizedAccessException:
                    // Usuario no autorizado para la acción
                    statusCode = HttpStatusCode.Unauthorized; // 401
                    mensaje = "Acceso no autorizado";
                    break;

                case InvalidOperationException:
                    // Operación inválida en el contexto actual
                    statusCode = HttpStatusCode.Conflict; // 409
                    mensaje = "Operación no válida: " + exception.Message;
                    break;

                case DbUpdateException dbUpdateEx:
                    // Excepciones de actualización en base de datos

                    // SQL Server
                    if (dbUpdateEx.InnerException is SqlException sqlEx)
                    {
                        // Violación de clave primaria o índice único (códigos 2627 y 2601)
                        if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                        {
                            statusCode = HttpStatusCode.Conflict; // 409 Conflict
                            mensaje = "Ya existe un registro con la misma clave primaria o valor único (SQL Server).";
                            break;
                        }
                        // Errores comunes de conexión SQL Server (ejemplos)
                        else if (sqlEx.Number == -1 || sqlEx.Number == 4060 || sqlEx.Number == 18456)
                        {
                            statusCode = HttpStatusCode.ServiceUnavailable; // 503
                            mensaje = "No se pudo conectar a la base de datos (SQL Server).";
                            break;
                        }
                    }

                    break;

                case SqlException sqlConnEx:
                    // Excepciones directas de conexión SQL Server
                    if (sqlConnEx.Number == -1 || sqlConnEx.Number == 4060 || sqlConnEx.Number == 18456)
                    {
                        statusCode = HttpStatusCode.ServiceUnavailable; // 503
                        mensaje = "No se pudo conectar a la base de datos (SQL Server).";
                        break;
                    }
                    break;

                case HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.BadGateway:
                    statusCode = HttpStatusCode.BadGateway;
                    mensaje = "Error de gateway: problema al comunicarse con un servicio externo.";
                    break;

                // Timeout, otro caso común en comunicación con servicios externos
                case TimeoutException:
                    statusCode = HttpStatusCode.GatewayTimeout;
                    mensaje = "Tiempo de espera agotado al intentar comunicarse con el servicio externo.";
                    break;

                default:
                    // Para cualquier otra excepción no controlada
                    mensaje = "Error interno del servidor"; // Mensaje seguro para producción
                    break;
            }

            // Asignamos el código de estado HTTP a la respuesta
            response.StatusCode = (int)statusCode;

            // Creamos el objeto JSON con el código y el mensaje de error
            var errorResponse = ApiResponse<string>.Fail(statusCode.ToString(), mensaje);
            await response.WriteAsJsonAsync(errorResponse);
        }

    }
}
