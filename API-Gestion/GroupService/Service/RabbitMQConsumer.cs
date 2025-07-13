using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GroupService.Context;
using GroupService.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GroupService.Service
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectionFactory _factory;
        private readonly RabbitMQProducer _rabbitMQProducer;

        public RabbitMQConsumer(IServiceProvider serviceProvider, IOptions<RabbitMQSettings> settings, RabbitMQProducer rabbitMQProducer)
        {
            _serviceProvider = serviceProvider;
            var config = settings.Value;
            _rabbitMQProducer = rabbitMQProducer;

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
                using var channel = await connection.CreateChannelAsync();
                Console.WriteLine("Conexion y Canal creado");

                const string exchangeName = "user_delete_broadcast";
                const string queueBroadcastDelete = "user_delete_queue_group"; // cola para UserDeleteDTO
                const string queueGroupDelete = "group_unsubscribe_queue";     // cola para GroupDeleteDTO

                // 1. Declarar el exchange fanout para borrado de usuarios
                await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Fanout, durable: true);

                // 2. Declarar y enlazar la cola para UserDeleteDTO (broadcast)
                await channel.QueueDeclareAsync(queue: queueBroadcastDelete, durable: true, exclusive: false, autoDelete: false);
                await channel.QueueBindAsync(queue: queueBroadcastDelete, exchange: exchangeName, routingKey: "");

                // 3. Declarar la cola directa para GroupDeleteDTO (no se une al exchange)
                await channel.QueueDeclareAsync(queue: queueGroupDelete, durable: true, exclusive: false, autoDelete: false);


                // 4. Consumidor para eliminar los grupos asignados de una tarea
                var consumerGroupDelete = new AsyncEventingBasicConsumer(channel);
                consumerGroupDelete.ReceivedAsync += async (model, ea) =>
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    Console.WriteLine("\n======================================");
                    Console.WriteLine("📥 NUEVO MENSAJE RECIBIDO - GROUP SERVICE");
                    Console.WriteLine("--------------------------------------");
                    Console.WriteLine($"🧾 Cola de origen : {ea.RoutingKey}");
                    Console.WriteLine($"📨 Contenido      :\n{message}");
                    Console.WriteLine("======================================\n");

                    try
                    {
                        var dto = JsonSerializer.Deserialize<GroupDeleteDTO>(message);

                        if (dto != null && dto.id_group != null && dto.id_group.Any() && dto.tipoEliminacion.HasValue)
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var context = scope.ServiceProvider.GetRequiredService<AppDBContext>();
                            var actorId = dto.id_usuario ?? Guid.Empty;

                            var eventosBitacora = new List<BitacoraEvent>();

                            switch (dto.tipoEliminacion)
                            {
                                case TipoEliminacion.EliminarGrupos:
                                    var grupos = await context.grupos
                                        .Where(g => dto.id_group.Contains(g.Id))
                                        .Include(g => g.Recursos)
                                        .ToListAsync();

                                    foreach (var grupo in grupos)
                                    {
                                        var antes = new Dictionary<string, object>
                                        {
                                            { "nombre", grupo.nombre },
                                            { "zona_asignada", grupo.zona_asignada },
                                            { "Recursos", grupo.Recursos.Select(r => new {
                                                r.Id,
                                                r.Nombre,
                                                r.VoluntarioId
                                            }).ToList() }
                                        };

                                        eventosBitacora.Add(new BitacoraEvent
                                        {
                                            EntityType = "Grupo y Recursos",
                                            EntityId = grupo.Id.ToString(),
                                            Action = "Delete",
                                            ActorId = actorId,
                                            Antes = antes,
                                            Despues = new Dictionary<string, object>(),
                                            Timestamp = DateTime.UtcNow
                                        });
                                    }

                                    var recursosGrupo = grupos.SelectMany(g => g.Recursos).ToList();
                                    context.recursos.RemoveRange(recursosGrupo);
                                    context.grupos.RemoveRange(grupos);
                                    await context.SaveChangesAsync();

                                    Console.WriteLine($"🗑 Eliminados {grupos.Count} grupos y {recursosGrupo.Count} recursos.");
                                    break;

                                case TipoEliminacion.EliminarRecursos:
                                    var recursosUsuario = await context.recursos
                                        .Where(r => dto.id_group.Contains(r.GrupoId) && r.VoluntarioId == actorId)
                                        .ToListAsync();

                                    foreach (var recurso in recursosUsuario)
                                    {
                                        var antes = new Dictionary<string, object>
                                        {
                                            { "GrupoId", recurso.GrupoId },
                                            { "Nombre", recurso.Nombre },
                                            { "VoluntarioId", recurso.VoluntarioId }
                                        };

                                        eventosBitacora.Add(new BitacoraEvent
                                        {
                                            EntityType = "Recurso",
                                            EntityId = recurso.Id.ToString(),
                                            Action = "Delete",
                                            ActorId = actorId,
                                            Antes = antes,
                                            Despues = new Dictionary<string, object>(),
                                            Timestamp = DateTime.UtcNow
                                        });
                                    }

                                    context.recursos.RemoveRange(recursosUsuario);
                                    await context.SaveChangesAsync();

                                    Console.WriteLine($" 🗑 Eliminados {recursosUsuario.Count} recursos del usuario {actorId}.");
                                    break;
                            }

                            if (eventosBitacora.Any())
                            {
                                await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", eventosBitacora, "audit_ServiceConnection");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ DTO inválido: falta información esencial.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error procesando mensaje: {ex.Message}");
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                };



                // 5. Consumidor para eliminación de recursos del usuario que se da de baja en el sistema
                var consumerUserDelete = new AsyncEventingBasicConsumer(channel);
                consumerUserDelete.ReceivedAsync += async (model, ea) =>
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    Console.WriteLine("\n======================================");
                    Console.WriteLine("📥 NUEVO MENSAJE RECIBIDO - GROUP SERVICE");
                    Console.WriteLine("--------------------------------------");
                    Console.WriteLine($"🧾 Cola de origen : {ea.RoutingKey}");
                    Console.WriteLine($"📨 Contenido      :\n{message}");
                    Console.WriteLine("======================================\n");

                    try
                    {
                        var deleteDto = JsonSerializer.Deserialize<UserDeleteDTO>(message);
                        if (deleteDto != null && deleteDto.id_usuario != Guid.Empty)
                        {
                            Console.WriteLine("🗑 Procesando eliminación recursos del usuario...");

                            using var scope = _serviceProvider.CreateScope();
                            var context = scope.ServiceProvider.GetRequiredService<AppDBContext>();

                            var recursos = context.recursos.Where(r => r.VoluntarioId == deleteDto.id_usuario).ToList();
                            var eventosBitacora = new List<BitacoraEvent>();

                            foreach (var recurso in recursos)
                            {
                                var antes = new Dictionary<string, object>
                                {
                                    { "GrupoId", recurso.GrupoId },
                                    { "Nombre", recurso.Nombre },
                                    { "VoluntarioId", recurso.VoluntarioId }
                                };

                                var evento = new BitacoraEvent
                                {
                                    EntityType = "Recurso",
                                    EntityId = recurso.Id.ToString(),
                                    Action = "Delete",
                                    ActorId = deleteDto.id_usuario,
                                    Antes = antes,
                                    Despues = new Dictionary<string, object>(),
                                    Timestamp = DateTime.UtcNow
                                };

                                eventosBitacora.Add(evento);
                            }

                        }
                        else
                        {
                            Console.WriteLine("❌ Error: deleteDto es null o id inválido");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error procesando mensaje de eliminación: {ex.Message}");
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                };

                // 6. Iniciar consumo
                await channel.BasicConsumeAsync(queue: queueGroupDelete, autoAck: false, consumer: consumerGroupDelete);
                await channel.BasicConsumeAsync(queue: queueBroadcastDelete, autoAck: false, consumer: consumerUserDelete);

                Console.WriteLine("🔄 Consumo iniciado en GroupService para la eliminacion de grupos y recursos");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en RabbitMqConsumer: {ex}");
            }
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
