using Confluent.Kafka;
using KafkaDemo.Core.Interfaces;
using KafkaDemo.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KafkaDemo.Infrastructure.Producers
{
    /// <summary>
    /// Transactional Kafka Producer - Exactly-Once Semantics (EOS)
    /// Học về: Transactions, Atomic writes, Read-committed isolation
    /// </summary>
    public class TransactionalKafkaProducer : IKafkaProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<TransactionalKafkaProducer> _logger;
        private readonly string _transactionalId;

        public TransactionalKafkaProducer(string bootstrapServers, ILogger<TransactionalKafkaProducer> logger)
        {
            _logger = logger;
            _transactionalId = $"transactional-producer-{Guid.NewGuid().ToString()[..8]}";

            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                
                // TRANSACTIONAL SETTINGS
                TransactionalId = _transactionalId,      // REQUIRED for transactions
                EnableIdempotence = true,                // REQUIRED (automatically enabled with TransactionalId)
                Acks = Acks.All,                         // REQUIRED for EOS
                MaxInFlight = 5,                         // Can be > 1 with idempotence
                
                // TIMEOUT & RETRY
                TransactionTimeoutMs = 60000,            // Transaction timeout (60s)
                MessageTimeoutMs = 30000,
                RequestTimeoutMs = 30000,
                
                // PERFORMANCE
                LingerMs = 10,
                BatchSize = 16384,
                CompressionType = CompressionType.Snappy,
                
                ClientId = _transactionalId
            };

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    _logger.LogError($"❌ Transaction Error: {e.Reason}, IsFatal: {e.IsFatal}");
                })
                .Build();

            // Initialize transactions
            _producer.InitTransactions(TimeSpan.FromSeconds(30));
            _logger.LogInformation($"✅ Transactional Producer initialized: {_transactionalId}");
        }

        /// <summary>
        /// Publish multiple messages in a single transaction (atomic)
        /// All-or-nothing: Either all messages commit or none
        /// </summary>
        public async Task PublishTransactionalBatchAsync(string topic, IEnumerable<KafkaMessage> messages)
        {
            _producer.BeginTransaction();
            _logger.LogInformation($"🔄 Transaction started for {messages.Count()} messages");

            try
            {
                foreach (var message in messages)
                {
                    var json = JsonSerializer.Serialize(message);
                    await _producer.ProduceAsync(topic, new Message<string, string>
                    {
                        Key = message.Id.ToString(),
                        Value = json,
                        Headers = new Headers
                        {
                            { "transaction-id", System.Text.Encoding.UTF8.GetBytes(_transactionalId) }
                        }
                    });
                }

                _producer.CommitTransaction();
                _logger.LogInformation($"✅ Transaction committed successfully - {messages.Count()} messages");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Transaction failed: {ex.Message}");
                _producer.AbortTransaction();
                _logger.LogWarning("⚠️  Transaction aborted - no messages committed");
                throw;
            }
        }

        /// <summary>
        /// Publish with consume-transform-produce pattern (read-process-write atomically)
        /// Useful for stream processing: read from topic A, process, write to topic B
        /// 
        /// NOTE: This method requires a Consumer instance to get ConsumerGroupMetadata.
        /// Use: consumer.ConsumerGroupMetadata property instead of creating new instance.
        /// Commented out để tránh compile error - see documentation for proper usage.
        /// </summary>
        /*
        public async Task PublishWithConsumerOffsetAsync(
            string outputTopic, 
            KafkaMessage message, 
            TopicPartitionOffset inputOffset,
            IConsumer<string, string> consumer)  // ✅ FIX: Need consumer instance
        {
            _producer.BeginTransaction();

            try
            {
                var json = JsonSerializer.Serialize(message);
                
                // 1. Produce to output topic
                await _producer.ProduceAsync(outputTopic, new Message<string, string>
                {
                    Key = message.Id.ToString(),
                    Value = json
                });

                // 2. Commit consumer offset (exactly-once consume-produce)
                // ✅ FIX: Get metadata from consumer instance
                _producer.SendOffsetsToTransaction(
                    new[] { new TopicPartitionOffset(inputOffset.Topic, inputOffset.Partition, inputOffset.Offset + 1) },
                    consumer.ConsumerGroupMetadata,
                    TimeSpan.FromSeconds(10));

                _producer.CommitTransaction();
                _logger.LogInformation($"✅ Transactional consume-produce completed");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Transactional consume-produce failed: {ex.Message}");
                _producer.AbortTransaction();
                throw;
            }
        }
        */

        public Task PublishAsync(string topic, KafkaMessage message)
        {
            return PublishTransactionalBatchAsync(topic, new[] { message });
        }

        public Task PublishAsync(string topic, string message)
        {
            var kafkaMessage = new KafkaMessage
            {
                Id = Guid.NewGuid(),
                Content = message,
                CreatedAt = DateTime.UtcNow,
                Type = "string"
            };
            return PublishAsync(topic, kafkaMessage);
        }

        public Task PublishAsync(string topic, ChatMessage message)
        {
            var kafkaMessage = new KafkaMessage
            {
                Id = Guid.NewGuid(),
                Content = message.Text,  // ✅ FIX: ChatMessage has 'Text' property, not 'Message'
                CreatedAt = message.Timestamp.UtcDateTime,  // ✅ FIX: Convert DateTimeOffset to DateTime
                Type = "chat"
            };
            return PublishAsync(topic, kafkaMessage);
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing transactional producer...");
            _producer.Dispose();
        }
    }
}
