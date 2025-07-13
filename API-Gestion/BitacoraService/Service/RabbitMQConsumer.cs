using System.Text;
using System.Text.Json;
using System.Threading;
using BitacoraService.Context;
using BitacoraService.Data;
using BitacoraService.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace BitacoraService.Service
{
    public class RabbitMQConsumer : BackgroundService
    {

        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectionFactory _factory;

        public RabbitMQConsumer(IServiceProvider serviceProvider, IOptions<RabbitMQSettings> settings)
        {
            _serviceProvider = serviceProvider;
            var config = settings.Value;

            _factory = new ConnectionFactory()
            {
                HostName = config.HostName,
                UserName = config.UserName,
                Password = config.Password,
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            try
            {
                using var connection = await RetryCreateConnectionAsync();
                Console.WriteLine("Conexión creada");

                using var channel = await connection.CreateChannelAsync();
                Console.WriteLine("Canal creado");

                // Asegurar que la cola existe
                await channel.QueueDeclareAsync(
                    queue: "audit_log_queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                );

                Console.WriteLine("Cola declarada: audit_log_queue");


                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    Console.WriteLine("\n===== Nuevo mensaje recibido por RabbitMQConsumer =====");
                    Console.WriteLine($"Contenido del mensaje:\n{message}");


                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDBContext>();

                        // Detectar si el mensaje es una lista o un único objeto
                        using var doc = JsonDocument.Parse(message);

                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            var eventos = JsonSerializer.Deserialize<List<BitacoraEventDTO>>(message);

                            if (eventos != null)
                            {
                                foreach (var bitacora in eventos)
                                {
                                    if (!Enum.TryParse<Action_Evento>(bitacora.Action, ignoreCase: true, out var actionEnum))
                                    {
                                        Console.WriteLine($"Acción no válida: {bitacora.Action}");
                                        continue;
                                    }

                                    await GuardarEventoAsync(bitacora, actionEnum, dbContext, stoppingToken);

                                    Console.WriteLine("✔ Lista de eventos registrada en la bitácora");
                                }
                            }
                        }
                        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            var bitacora = JsonSerializer.Deserialize<BitacoraEventDTO>(message);

                            if (bitacora != null)
                            {
                                if (!Enum.TryParse<Action_Evento>(bitacora.Action, ignoreCase: true, out var actionEnum))
                                {
                                    Console.WriteLine($"Acción no válida: {bitacora.Action}");
                                    return;
                                }

                                await GuardarEventoAsync(bitacora, actionEnum, dbContext, stoppingToken);

                                Console.WriteLine("✔ Evento único registrado en la bitácora");
                            }
                        }
                        else
                        {
                            Console.WriteLine("" +
                                " El mensaje recibido no es ni un objeto JSON ni un array válido.");
                        }
                    }
                    
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"Error deserializando el mensaje: {jsonEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error inesperado deserializando el mensaje: {ex.Message}");
                    }

                    //Confirmar a RabbitMQ que el mensaje se ha procesado correctamente,
                    // para que pueda eliminarlo de la cola y no sea reenviado.

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                };



                await channel.BasicConsumeAsync(
                    queue: "audit_log_queue",
                    autoAck: false,
                    consumer: consumer
                );

                Console.WriteLine("Consumo iniciado");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                // Registra el error para diagnóstico
                Console.WriteLine($"Error en RabbitMqConsumer: {ex}");
            }
        }
    


        private async Task GuardarEventoAsync(BitacoraEventDTO bitacora, Action_Evento actionEnum, AppDBContext dbContext, CancellationToken cancellationToken)
        {
            var payload = new Payload
            {
                Id = bitacora.EntityId,
                Antes = bitacora.Antes,
                Despues = bitacora.Despues
            };

            string descripcion = actionEnum switch
            {
                Action_Evento.Create => $"Se creó {bitacora.EntityType} con ID {bitacora.EntityId}",
                Action_Evento.Update => $"Se actualizó {bitacora.EntityType} con ID {bitacora.EntityId}",
                Action_Evento.Delete => $"Se eliminó {bitacora.EntityType} con ID {bitacora.EntityId}",
                Action_Evento.Access => $"Se accedió a {bitacora.EntityType}",
                _ => "Acción desconocida"
            };

            var evento = new Evento
            {
                Id = Guid.NewGuid(),
                EntityType = bitacora.EntityType,
                Action = actionEnum,
                ActorId = bitacora.ActorId,
                Timestamp = bitacora.Timestamp,
                Payload = payload,
                Descripcion = descripcion
            };

            dbContext.Evento.Add(evento);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<IConnection> RetryCreateConnectionAsync()
        {
            var tiemposEspera = new[] { 5, 10, 15, 20, 25, 30 }; // segundos
            int totalIntentos = tiemposEspera.Length;

            for (int attempt = 1; attempt <= totalIntentos; attempt++)
            {
                try
                {
                    var connection = await _factory.CreateConnectionAsync("assign_ServiceConnection");
                    Console.WriteLine($"✅ Conexión a RabbitMQ establecida en el intento {attempt}");
                    return connection;
                }
                catch (BrokerUnreachableException ex)
                {
                    Console.WriteLine($"❌ Intento {attempt}: error de conexión a RabbitMQ ({ex.Message})");

                    if (attempt == totalIntentos)
                    {
                        Console.WriteLine("⛔ Se alcanzó el máximo de intentos sin éxito.");
                        throw new Exception("No se pudo conectar a RabbitMQ tras múltiples intentos.");
                    }

                    int espera = tiemposEspera[attempt - 1];
                    Console.WriteLine($"⏳ Esperando {espera} segundos antes del siguiente intento...");
                    await Task.Delay(TimeSpan.FromSeconds(espera));
                }
            }

            throw new InvalidOperationException("Ruta de recuperación no alcanzada.");
        }


    }
}
