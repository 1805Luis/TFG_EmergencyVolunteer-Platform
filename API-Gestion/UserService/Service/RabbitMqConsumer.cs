using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using UserService.Context;
using UserService.Data;
using UserService.DTOs;
using UserService.Service;


public class RabbitMqConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionFactory _factory;

    public RabbitMqConsumer(IServiceProvider serviceProvider, IOptions<RabbitMQSettings> settings)
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
            using var channel = await connection.CreateChannelAsync();
            Console.WriteLine("Conexion y Canal creado");

            const string exchangeName = "user_delete_broadcast";
            const string queueNameCreate = "user_create_queue";
            const string queueNameDelete = "user_delete_queue_user";

            // 1. Declarar el exchange tipo fanout para delete
            await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Fanout, durable: true);

            // 2. Declarar la cola para creación de usuarios
            await channel.QueueDeclareAsync(queue: queueNameCreate, durable: true, exclusive: false, autoDelete: false);

            // 3. Declarar la cola para eliminación de usuarios y enlazarla al exchange fanout
            await channel.QueueDeclareAsync(queue: queueNameDelete, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(queue: queueNameDelete, exchange: exchangeName, routingKey: "");

            // 4. Consumidor para creación de usuarios
            var consumerCreate = new AsyncEventingBasicConsumer(channel);
            consumerCreate.ReceivedAsync += async (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                Console.WriteLine("\n======================================");
                Console.WriteLine("📥 NUEVO MENSAJE RECIBIDO - USER SERVICE");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine($"🧾 Cola de origen : {ea.RoutingKey}");
                Console.WriteLine($"📨 Contenido      :\n{message}");
                Console.WriteLine("======================================\n");

                try
                {
                    var userDto = JsonSerializer.Deserialize<UserDTO>(message);
                    if (userDto != null)
                    {
                        Console.WriteLine("🆕 Procesando creación de usuario...");
                        await SaveUserToDatabase(userDto);
                    }
                    else
                    {
                        Console.WriteLine("❌ Error: userDto es null");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error procesando mensaje de creación: {ex.Message}");
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            // 5. Consumidor para eliminación de usuarios
            var consumerDelete = new AsyncEventingBasicConsumer(channel);
            consumerDelete.ReceivedAsync += async (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine("\n======================================");
                Console.WriteLine("📥 NUEVO MENSAJE RECIBIDO - USER SERVICE");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine($"🧾 Cola de origen : {ea.RoutingKey}");
                Console.WriteLine($"📨 Contenido      :\n{message}");
                Console.WriteLine("======================================\n");

                try
                {
                    var deleteDto = JsonSerializer.Deserialize<UserDeleteDTO>(message);
                    if (deleteDto != null && deleteDto.id_usuario != Guid.Empty)
                    {
                        Console.WriteLine("🗑 Procesando eliminación de usuario...");
                        await DeleteUserFromDatabase(deleteDto.id_usuario);
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

            // 6. Iniciar consumo de ambas colas
            await channel.BasicConsumeAsync(queue: queueNameCreate, autoAck: false, consumer: consumerCreate);
            await channel.BasicConsumeAsync(queue: queueNameDelete, autoAck: false, consumer: consumerDelete);

            Console.WriteLine("🔄 Consumo iniciado en UserService para creación y eliminación");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en RabbitMqConsumer: {ex}");
        }
    }

    private async Task SaveUserToDatabase(UserDTO userDto)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDBContext>();

            var user = new User
            {
                Id = userDto.Id,
                Name = userDto.Name,
                Telf = userDto.Telf,
                Disponibilidad = userDto.Disponibilidad,
                Habilidades = userDto.Habilidades,
            };

            dbContext.User.Add(user);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("Usuario guardado en la base de datos");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error guardando usuario en la base de datos: {ex.Message}");
        }
    }

    private async Task DeleteUserFromDatabase(Guid userId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDBContext>();
            var user = await dbContext.User.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                dbContext.User.Remove(user);
                await dbContext.SaveChangesAsync();
                Console.WriteLine($"Usuario con ID {userId} eliminado de la base de datos");
            }
            else
            {
                Console.WriteLine($"Usuario con ID {userId} no encontrado para eliminar");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error eliminando usuario en la base de datos: {ex.Message}");
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
