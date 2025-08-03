using Confluent.Kafka;

namespace KafkaDemo.Tests
{
    public class KafkaProducerTests
    {
        [Fact]
        public async void SendMessage_Should_Publish_Message_To_Kafka()
        {
            var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
            using var producer = new ProducerBuilder<Null, string>(config).Build();

            var result = await producer.ProduceAsync("demo-topic", new Message<Null, string> { Value = "test" });

            Assert.Equal(PersistenceStatus.Persisted, result.Status);
        }
    }
}