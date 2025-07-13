using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;

namespace AuthService.Service
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

        public async Task SendMessageToQueueAsync<T>(string destinationName, T message, string connectionName, bool isBroadcast)
        {
            try
            {
                using var connection = await RetryCreateConnectionAsync(connectionName);
                using var channel = await connection.CreateChannelAsync();

                var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                if (isBroadcast)
                {
                    // Broadcast → fanouto
                    await channel.ExchangeDeclareAsync(
                        exchange: destinationName,
                        type: ExchangeType.Fanout,
                        durable: true,
                        autoDelete: false
                    );

                    await channel.BasicPublishAsync(
                        exchange: destinationName,
                        routingKey: "", // Se ignora en fanout
                        body: messageBody
                    );

                    Console.WriteLine($"📣 Mensaje de broadcast enviado a exchange '{destinationName}'");
                }
                else
                {
                    //Unicast → cola directa 
                    await channel.QueueDeclareAsync(
                        queue: destinationName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false
                    );

                    await channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: destinationName, 
                        body: messageBody
                    );

                    Console.WriteLine($"📬 Mensaje directo enviado a la cola '{destinationName}'");
                }
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
