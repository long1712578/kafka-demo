namespace KafkaDemo.Core.Models
{
    public class ChatMessage
    {
        public string User { get; set; } = default!;
        public string Text { get; set; } = default!;
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
