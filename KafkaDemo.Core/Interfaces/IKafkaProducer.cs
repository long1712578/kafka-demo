using KafkaDemo.Core.Models;

namespace KafkaDemo.Core.Interfaces
{
    public interface IKafkaProducer
    {
        Task PublishAsync(string topic, KafkaMessage message);

    }
}
