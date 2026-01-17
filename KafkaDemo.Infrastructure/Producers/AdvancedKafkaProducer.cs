using Confluent.Kafka;
using KafkaDemo.Core.Interfaces;
using KafkaDemo.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KafkaDemo.Infrastructure.Producers
{
    /// <summary>
    /// Advanced Kafka Producer with Custom Partitioning Strategy
    /// Học về: Custom Partitioner, Sticky Partitioning, Key-based routing
    /// </summary>
    public class AdvancedKafkaProducer : IKafkaProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<AdvancedKafkaProducer> _logger;

        public AdvancedKafkaProducer(string bootstrapServers, ILogger<AdvancedKafkaProducer> logger)
        {
            _logger = logger;
            
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                
                // DURABILITY & RELIABILITY
                Acks = Acks.All,                        // Đợi all ISR replicas acknowledge (-1)
                EnableIdempotence = true,               // Tránh duplicate messages (exactly-once semantics)
                MaxInFlight = 5,                        // Số requests đồng thời (with idempotence, có thể > 1)
                
                // RETRY & TIMEOUT
                MessageTimeoutMs = 30000,               // Tổng timeout cho message
                RequestTimeoutMs = 30000,               // Timeout cho từng request
                MessageSendMaxRetries = 3,              // Deprecated, dùng retries
                RetryBackoffMs = 100,                   // Delay giữa các lần retry
                
                // BATCHING & PERFORMANCE
                LingerMs = 10,                          // Đợi 10ms để batch messages (tăng throughput)
                BatchSize = 16384,                      // 16KB batch size
                CompressionType = CompressionType.Snappy, // Compression algorithm
                
                // PARTITIONING
                Partitioner = Partitioner.Murmur2Random, // Default partitioner
                
                // BUFFER MANAGEMENT
                QueueBufferingMaxMessages = 100000,     // Max messages in buffer
                QueueBufferingMaxKbytes = 1048576,      // 1GB buffer
                
                // MONITORING
                StatisticsIntervalMs = 60000,           // Statistics mỗi 60s
                
                // CLIENT ID for tracking
                ClientId = $"advanced-producer-{Guid.NewGuid().ToString()[..8]}"
            };

            _producer = new ProducerBuilder<string, string>(config)
                // NOTE: Custom partitioner removed - Kafka's built-in Murmur2 partitioner works well
                // If you need custom partitioning, use: producer.Produce(new TopicPartition(topic, partition), ...)
                .SetErrorHandler((_, e) =>
                {
                    _logger.LogError($"Kafka Error: {e.Reason}, Code: {e.Code}, IsFatal: {e.IsFatal}");
                })
                .SetStatisticsHandler((_, json) =>
                {
                    // Parse và log statistics
                    _logger.LogInformation($"Producer Statistics: {json}");
                })
                .Build();

            _logger.LogInformation($"Advanced Kafka Producer initialized: {config.ClientId}");
        }

        /// <summary>
        /// Publish với automatic partitioning (round-robin nếu key = null)
        /// </summary>
        public async Task PublishAsync(string topic, KafkaMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            await PublishWithRetryAsync(topic, message.Id.ToString(), json);
        }

        /// <summary>
        /// Publish với explicit key cho partition routing
        /// Key giống nhau -> cùng partition -> ordering guarantee
        /// </summary>
        public async Task PublishWithKeyAsync(string topic, string key, KafkaMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            await PublishWithRetryAsync(topic, key, json);
        }

        /// <summary>
        /// Publish với explicit partition (bypass partitioner)
        /// </summary>
        public async Task PublishToPartitionAsync(string topic, int partition, KafkaMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            try
            {
                var result = await _producer.ProduceAsync(
                    new TopicPartition(topic, new Partition(partition)),
                    new Message<string, string>
                    {
                        Key = message.Id.ToString(),
                        Value = json,
                        Timestamp = Timestamp.Default,
                        Headers = new Headers
                        {
                            { "message-type", System.Text.Encoding.UTF8.GetBytes(message.Type) },
                            { "created-at", System.Text.Encoding.UTF8.GetBytes(message.CreatedAt.ToString("o")) }
                        }
                    });

                _logger.LogInformation($"✅ Published to {result.TopicPartitionOffset}, Partition: {result.Partition.Value}");
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError($"❌ Failed to publish to partition {partition}: {ex.Error.Reason}");
                throw;
            }
        }

        /// <summary>
        /// Batch publishing cho high throughput
        /// </summary>
        public async Task PublishBatchAsync(string topic, IEnumerable<KafkaMessage> messages)
        {
            var tasks = new List<Task>();
            
            foreach (var message in messages)
            {
                var json = JsonSerializer.Serialize(message);
                var task = _producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = message.Id.ToString(),
                    Value = json
                });
                tasks.Add(task);
            }

            try
            {
                await Task.WhenAll(tasks);
                _logger.LogInformation($"✅ Batch published {tasks.Count} messages to {topic}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Batch publish failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Private helper: Publish with automatic retry
        /// </summary>
        private async Task PublishWithRetryAsync(string topic, string key, string value)
        {
            try
            {
                var message = new Message<string, string>
                {
                    Key = key,
                    Value = value,
                    Timestamp = Timestamp.Default,
                    Headers = new Headers
                    {
                        { "producer-id", System.Text.Encoding.UTF8.GetBytes(_producer.Name) },
                        { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()) }
                    }
                };

                var deliveryResult = await _producer.ProduceAsync(topic, message);
                
                _logger.LogInformation(
                    $"✅ Message delivered to {deliveryResult.TopicPartitionOffset}" +
                    $"\n   Partition: {deliveryResult.Partition.Value}" +
                    $"\n   Offset: {deliveryResult.Offset.Value}" +
                    $"\n   Status: {deliveryResult.Status}" +
                    $"\n   Latency: {deliveryResult.Message.Timestamp.UtcDateTime}");
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(
                    $"❌ Delivery failed: {ex.Error.Reason}" +
                    $"\n   Error Code: {ex.Error.Code}" +
                    $"\n   Is Fatal: {ex.Error.IsFatal}");
                throw;
            }
        }

        /// <summary>
        /// Flush pending messages (blocking operation)
        /// </summary>
        public void Flush(TimeSpan timeout)
        {
            _logger.LogInformation("Flushing producer...");
            _producer.Flush(timeout);
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing producer...");
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();
        }

        public Task PublishAsync(string topic, string message)
        {
            return _producer.ProduceAsync(topic, new Message<string, string> { Value = message });
        }

        public Task PublishAsync(string topic, ChatMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            return PublishWithRetryAsync(topic, message.User, json);
        }
    }
}
