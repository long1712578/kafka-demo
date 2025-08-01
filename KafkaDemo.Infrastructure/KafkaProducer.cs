using Confluent.Kafka;
using KafkaDemo.Core.Interfaces;
using KafkaDemo.Core.Models;
using System.Text.Json;

namespace KafkaDemo.Infrastructure
{
    public class KafkaProducer : IKafkaProducer
    {
        private readonly IProducer<Null, string> _producer;

        public KafkaProducer(string bootstrapServers)
        {
            var config = new ProducerConfig 
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,                     // đảm bảo message được ghi đầy đủ
                MessageTimeoutMs = 5000,            // timeout gửi
                EnableIdempotence = true,           // tránh gửi trùng
                RetryBackoffMs = 100,               // delay giữa các lần retry
                MaxInFlight = 5,                    // tối đa message đang chờ ack
                CompressionType = CompressionType.Snappy, // tăng hiệu suất
            };
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task PublishAsync(string topic, KafkaMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            try
            {
                var result = await _producer.ProduceAsync(topic, new Message<Null, string> { Value = json });
                Console.WriteLine($"Produced message to {result.TopicPartitionOffset}: {json}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Producer error: {ex.Message}");
            }
        }
        public async Task PublishAsync(string topic, string message)
        {
            await _producer.ProduceAsync(topic, new Message<Null, string> { Value = message });
        }
    }
}
