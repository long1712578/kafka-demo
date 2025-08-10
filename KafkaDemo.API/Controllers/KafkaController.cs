using KafkaDemo.Core.Interfaces;
using KafkaDemo.Core.Models;
using KafkaDemo.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace KafkaDemo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class KafkaController : ControllerBase
    {
        private readonly IKafkaProducer _producer;

        public KafkaController(IKafkaProducer producer)
        {
            _producer = producer;
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] KafkaMessage message)
        {
            message.Id = Guid.NewGuid();
            message.CreatedAt = DateTime.UtcNow;
            await _producer.PublishAsync("demo-topic", message);
            return Ok("Message published!");
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessage message)
        {
            await _producer.PublishAsync("chat-topic", message);
            return Ok("Message queued to Kafka");
        }

    }
}
