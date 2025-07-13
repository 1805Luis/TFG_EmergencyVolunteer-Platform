using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using AssigmentService.ApiResponse;

namespace AssigmentService.Middleware
{
	public class ExceptionHandlingMiddleware
	{
		private readonly RequestDelegate _next;

		public ExceptionHandlingMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task Invoke(HttpContext context)
		{
			try
			{
				// Pasar la ejecución al siguiente middleware
				await _next(context);
			}
			catch (Exception ex)
			{
				// Capturar y manejar cualquier excepción no gestionada
				await HandleExceptionAsync(context, ex);
			}
		}

		private async Task HandleExceptionAsync(HttpContext context, Exception exception)
		{
			var response = context.Response;
			response.ContentType = "application/json";

			// Por defecto, se considera error interno del servidor (500)
			var statusCode = HttpStatusCode.InternalServerError;
			string mensaje = "Ocurrió un error inesperado";

			switch (exception)
			{
                case ArgumentNullException argNullEx:
                    statusCode = HttpStatusCode.BadRequest;
                    mensaje = "Falta un parámetro obligatorio: " + argNullEx.ParamName;
                    break;

                case ArgumentException argEx:
                    statusCode = HttpStatusCode.BadRequest;
                    mensaje = "Solicitud inválida: " + argEx.Message;
                    break;

                case KeyNotFoundException:
					statusCode = HttpStatusCode.NotFound; // 404
					mensaje = "Recurso no encontrado: " + exception.Message;
					break;

				case UnauthorizedAccessException:
					statusCode = HttpStatusCode.Unauthorized; // 401
					mensaje = "Acceso no autorizado";
					break;

				case InvalidOperationException:
					statusCode = HttpStatusCode.Conflict; // 409
					mensaje = "Operación no válida: " + exception.Message;
					break;

				case DbUpdateException dbUpdateEx:
					// Excepciones específicas para violaciones de clave primaria/única

					// SQL Server
					if (dbUpdateEx.InnerException is SqlException sqlEx)
					{
						// Código 2627 o 2601: Violación de clave primaria o índice único
						if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
						{
							statusCode = HttpStatusCode.Conflict; // 409 Conflict
							mensaje = "Ya existe un registro con la misma clave primaria o valor único (SQL Server).";
							break;
						}
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
					// En producción no se debe enviar detalle de error para seguridad
					mensaje = "Error interno del servidor";
					break;
			}

			response.StatusCode = (int)statusCode;

            var errorResponse = ApiResponse<string>.Fail(statusCode.ToString(), mensaje);
            await response.WriteAsJsonAsync(errorResponse);
        }
	}
}
