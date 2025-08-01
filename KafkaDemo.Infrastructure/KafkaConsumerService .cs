using Confluent.Kafka;
using KafkaDemo.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly KafkaSettings _kafkaSettings;

    public KafkaConsumerService(IOptions<KafkaSettings> kafkaSettings, ILogger<KafkaConsumerService> logger)
    {
        _kafkaSettings = kafkaSettings.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                GroupId = _kafkaSettings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    _logger.LogError($"Kafka error: {e.Reason}");
                    Console.WriteLine($"Kafka error: {e.Reason}");
                })
                .Build();

            consumer.Subscribe(_kafkaSettings.Topic);
            _logger.LogInformation($"Kafka consumer subscribed to topic: {_kafkaSettings.Topic}");
            Console.WriteLine("Consumer {0} started...", _kafkaSettings.Topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result != null)
                    {
                        _logger.LogInformation($"Received message: {result.Message.Value}");
                        Console.WriteLine("Received message {0}", result.Message.Value);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Stopping Kafka consumer...");
                Console.WriteLine("Stopping Kafka consumer...");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kafka consumer exception: {ex.Message}");
                Console.WriteLine($"Kafka consumer exception: {ex.Message}");
            }
            finally
            {
                consumer.Close();
            }
        });
    }
}
