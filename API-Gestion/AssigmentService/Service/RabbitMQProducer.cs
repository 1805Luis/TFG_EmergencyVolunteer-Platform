using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client;

namespace AssigmentService.Service
{
    public class RabbitMQProducer
    {
        private readonly ConnectionFactory _factory;

        public RabbitMQProducer(IOptions<RabbitMQSettings> rabbitMqOptions)
        {
            var settings = rabbitMqOptions.Value;
            _factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                UserName = settings.UserName,
                Password = settings.Password
            };
        }

        public async Task SendMessageToQueueAsync<T>(string queueName, T message, string connectionName)
        {
            try
            {
                using var connection = await RetryCreateConnectionAsync(connectionName);
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                );

                var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: queueName,
                    body: messageBody
                );

                Console.WriteLine($"✅ Mensaje enviado a la cola '{queueName}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [RabbitMQ] Error al enviar mensaje: {ex.Message}");
            }
        }

        private async Task<IConnection> RetryCreateConnectionAsync(string connectionName)
        {
            var tiemposEspera = new[] { 5, 10, 15, 20, 25, 30 }; // segundos
            int totalIntentos = tiemposEspera.Length;

            for (int attempt = 1; attempt <= totalIntentos; attempt++)
            {
                try
                {
                    var connection = await _factory.CreateConnectionAsync(connectionName);
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
