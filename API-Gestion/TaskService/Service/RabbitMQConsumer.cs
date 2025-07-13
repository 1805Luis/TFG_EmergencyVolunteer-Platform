using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using TaskService.Context;
using TaskService.Data;
using TaskService.DTOs;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TaskService.Service
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

                const string createQueue = "task_create_queue";
                const string deleteQueue = "emergency_delete_queue";
                const string updateQueue = "task_update_queue";

                await channel.QueueDeclareAsync(createQueue, durable: true, exclusive: false, autoDelete: false);
                await channel.QueueDeclareAsync(deleteQueue, durable: true, exclusive: false, autoDelete: false);
                await channel.QueueDeclareAsync(updateQueue, durable: true, exclusive: false, autoDelete: false);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    Console.WriteLine("\n======================================");
                    Console.WriteLine("📥 NUEVO MENSAJE RECIBIDO - TASK SERVICE");
                    Console.WriteLine("--------------------------------------");
                    Console.WriteLine($"🧾 Cola de origen : {ea.RoutingKey}");
                    Console.WriteLine($"📨 Contenido      :\n{message}");
                    Console.WriteLine("======================================\n");

                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDBContext>();

                    try
                    {
                        if (ea.RoutingKey == deleteQueue)
                        {
                            var data = JsonSerializer.Deserialize<BorrarTareasPorEmergenciaDTO>(message);
                            if (data == null) return;

                            var tareas = context.Tareas.Where(t => t.emergencia_id == data.id_emergencia).ToList();
                            var eventos = new List<BitacoraEvent>();
                            var idsEliminados = new List<Guid>();

                            foreach (var tarea in tareas)
                            {
                                idsEliminados.Add(tarea.Id);

                                var antes = new Dictionary<string, object>
                                {
                                    { "Nombre", tarea.Nombre },
                                    { "Ubicacion", tarea.Ubicacion },
                                    { "Descripcion", tarea.Descripcion },
                                    { "EmergenciaId", tarea.emergencia_id },
                                    { "Estado", tarea.estado.ToString() },
                                    { "Gravedad", tarea.gravedad.ToString() },
                                    { "HabilidadesMin", tarea.HabilidadesMin }
                                };

                                eventos.Add(new BitacoraEvent
                                {
                                    EntityType = "Tarea",
                                    EntityId = tarea.Id.ToString(),
                                    Action = "Delete",
                                    ActorId = data.id_usuario,
                                    Antes = antes,
                                    Despues = new Dictionary<string, object>(),
                                    Timestamp = DateTime.UtcNow
                                });
                            }

                            context.Tareas.RemoveRange(tareas);
                            await context.SaveChangesAsync();

                            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", eventos, "audit_ServiceConnection");
                            var tareasDTO = new TaskDeleteDTO
                            {
                                id_Usuario = data.id_usuario,   
                                id_task = idsEliminados
                            };
                            await _rabbitMQProducer.SendMessageToQueueAsync("task_delete_queue", tareasDTO, "task_ServiceConnection");
                            Console.WriteLine($"🗑 Eliminadas {tareas.Count} tareas de la emergencia {data.id_emergencia}");
                        }
                        else if (ea.RoutingKey == createQueue)
                        {
                            var data = JsonSerializer.Deserialize<InsertarTareasDTO>(message);
                            if (data?.tareas == null || !data.tareas.Any()) return;

                            var nuevasTareas = new List<Tarea>();
                            var eventos = new List<BitacoraEvent>();

                            foreach (var dto in data.tareas)
                            {
                                if (!Enum.TryParse<Gravedad>(dto.gravedad, true, out var gravedadEnum))
                                {
                                    Console.WriteLine($"⚠️ Gravedad inválida para la tarea: {dto.Nombre}");
                                    continue;
                                }

                                var tarea = new Tarea
                                {
                                    Id = Guid.NewGuid(),
                                    Nombre = dto.Nombre,
                                    Ubicacion = dto.Ubicacion,
                                    Descripcion = dto.Descripcion,
                                    emergencia_id = dto.emergencia_id,
                                    gravedad = gravedadEnum,
                                    estado = Estado.SinEmpezar,
                                    HabilidadesMin = dto.HabilidadesMin
                                };

                                nuevasTareas.Add(tarea);

                                var despues = new Dictionary<string, object>
                                {
                                    { "Nombre", tarea.Nombre },
                                    { "Ubicacion", tarea.Ubicacion },
                                    { "Descripcion", tarea.Descripcion },
                                    { "EmergenciaId", tarea.emergencia_id },
                                    { "Estado", tarea.estado.ToString() },
                                    { "Gravedad", tarea.gravedad.ToString() },
                                    { "HabilidadesMin", tarea.HabilidadesMin }
                                };

                                eventos.Add(new BitacoraEvent
                                {
                                    EntityType = "Tarea",
                                    EntityId = tarea.Id.ToString(),
                                    Action = "Create",
                                    ActorId = data.idUsuario,
                                    Antes = new Dictionary<string, object>(),
                                    Despues = despues,
                                    Timestamp = DateTime.UtcNow
                                });
                            }

                            await context.Tareas.AddRangeAsync(nuevasTareas);
                            await context.SaveChangesAsync();
                            await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", eventos, "audit_ServiceConnection");

                            Console.WriteLine($"✅ Insertadas {nuevasTareas.Count} tareas por el usuario {data.idUsuario}");

                        }else if (ea.RoutingKey == updateQueue)
                        {
                            var estadoDto = JsonSerializer.Deserialize<TaskUpdateDTO>(message, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (estadoDto != null)
                            {
                                var tarea = await context.Tareas.FindAsync(estadoDto.tarea_id);
                                if (tarea != null)
                                {
                                    if (Enum.TryParse<Estado>(estadoDto.estado, out var nuevoEstado))
                                    {
                                        var antes = new Dictionary<string, object>
                                        {

                                            { "Estado", tarea.estado.ToString() }

                                        };
                                        tarea.estado = nuevoEstado;
                                        var despues = new Dictionary<string, object>
                                        {
                                    
                                            { "Estado", nuevoEstado.ToString() }

                                        };
                                        await context.SaveChangesAsync();

                                        var evento = new BitacoraEvent
                                        {
                                            EntityType = "Tarea",
                                            EntityId = tarea.Id.ToString(),
                                            Action = "Update",
                                            ActorId = estadoDto.id_usuario,
                                            Antes = antes,
                                            Despues = despues,
                                            Timestamp = DateTime.UtcNow
                                        };
                                        await _rabbitMQProducer.SendMessageToQueueAsync("audit_log_queue", evento, "audit_ServiceConnection");

                                        Console.WriteLine($"✅ Estado de la tarea {tarea.Id} actualizado a {nuevoEstado}");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"⚠️ No se pudo convertir '{estadoDto.estado}' a enum Estado");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"❌ No se encontró la tarea con ID {estadoDto.tarea_id}");
                                }
                            }
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"❌ Error deserializando el mensaje: {jsonEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error inesperado: {ex.Message}");
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                };

                await channel.BasicConsumeAsync(queue: createQueue, autoAck: false, consumer: consumer);
                await channel.BasicConsumeAsync(queue: deleteQueue, autoAck: false, consumer: consumer);
                await channel.BasicConsumeAsync(queue: updateQueue, autoAck: false, consumer: consumer);

                Console.WriteLine("🔄 Consumo iniciado en TaskService");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en RabbitMqConsumer: {ex.Message}");
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
