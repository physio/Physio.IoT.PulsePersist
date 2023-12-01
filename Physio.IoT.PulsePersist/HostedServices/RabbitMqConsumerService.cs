using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Data.Sqlite;
using System.Text;
using Physio.IoT.PulsePersist.Configuration;

namespace Physio.IoT.PulsePersist.HostedServices
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly string _rabbitMqHost;
        private readonly string[] _queues;
        private readonly string _sqliteConnectionString;
        private readonly ILogger<RabbitMqConsumerService> _logger;


        public RabbitMqConsumerService(AppConfig appConfig, ILogger<RabbitMqConsumerService> logger)
        {
            _rabbitMqHost = appConfig.MessageBroker.Hostname;
            _queues = appConfig.MessageBroker.Queues;
            _sqliteConnectionString = appConfig.SqliteConnectionString!;
            _logger = logger;
            _logger.LogInformation($"RabbitMqConsumerService created with host {_rabbitMqHost}, and sqlite connection string {_sqliteConnectionString}");
        }

        private async Task InitializeDatabaseAsync()
        {
            if (!File.Exists(_sqliteConnectionString.Split('=')[1]))
            {
                using (var connection = new SqliteConnection(_sqliteConnectionString))
                {
                    await connection.OpenAsync();
                    var command = new SqliteCommand("CREATE TABLE IF NOT EXISTS Messages (Id INTEGER PRIMARY KEY AUTOINCREMENT, Content TEXT NOT NULL)", connection);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize database: {ex.Message}");
                return;
            }

            var factory = new ConnectionFactory() { HostName = _rabbitMqHost };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            for (int i = 0; i < _queues.Length; i++)
            {
                channel.QueueDeclare(queue: _queues[i], durable: false, exclusive: false, autoDelete: false, arguments: null);
            }

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("Received {0}", message);

                bool success = await SaveMessageToDatabaseAsync(message);
                if (success)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                }
            };

            for (int i = 0; i < _queues.Length; i++)
            {
                channel.BasicConsume(queue: _queues[i], autoAck: false, consumer: consumer);
            }

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task<bool> SaveMessageToDatabaseAsync(string message)
        {
            try
            {
                using (var connection = new SqliteConnection(_sqliteConnectionString))
                {
                    await connection.OpenAsync();
                    var command = new SqliteCommand("INSERT INTO Messages (Content) VALUES (@message)", connection);
                    command.Parameters.AddWithValue("@message", message);
                    await command.ExecuteNonQueryAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to database: {ex.Message}");
                return false;
            }
        }
        public override void Dispose()
        {
    
            base.Dispose();
        }
    }
}
