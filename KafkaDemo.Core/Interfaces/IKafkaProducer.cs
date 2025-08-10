using KafkaDemo.Core.Models;
using System.Threading.Tasks;

namespace KafkaDemo.Core.Interfaces
{
    public interface IKafkaProducer
    {
        Task PublishAsync(string topic, KafkaMessage message);
        Task PublishAsync(string topic, string message);
        Task PublishAsync(string topic, ChatMessage message);
    }
}
