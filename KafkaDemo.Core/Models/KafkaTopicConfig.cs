namespace KafkaDemo.Core.Models
{
    /// <summary>
    /// Module A - Task 1.1: Topic configuration definition
    /// Định nghĩa topics cho demo learning
    /// </summary>
    public class KafkaTopicConfig
    {
        public string Name { get; set; } = string.Empty;
        public int NumPartitions { get; set; } = 1;
        public short ReplicationFactor { get; set; } = 1;
        public Dictionary<string, string> Configs { get; set; } = new();
    }

    /// <summary>
    /// Module A Topics - Để học về Partitioning & Ordering
    /// </summary>
    public static class ModuleATopics
    {
        /// <summary>
        /// Topic: user-events
        /// - 3 partitions => học về partitioning by key
        /// - Key: userId => cùng user messages đi vào cùng partition
        /// - Học: Ordering per user, partitioning strategy
        /// </summary>
        public static KafkaTopicConfig UserEvents => new()
        {
            Name = "user-events",
            NumPartitions = 3,
            ReplicationFactor = 1,
            Configs = new Dictionary<string, string>
            {
                { "retention.ms", "604800000" },  // 7 days
                { "compression.type", "snappy" }
            }
        };

        /// <summary>
        /// Topic: orders
        /// - 3 partitions => học về key-based routing
        /// - Key: orderId => cùng order lifecycle đi vào cùng partition
        /// - Học: Exactly-once processing per order
        /// </summary>
        public static KafkaTopicConfig Orders => new()
        {
            Name = "orders",
            NumPartitions = 3,
            ReplicationFactor = 1,
            Configs = new Dictionary<string, string>
            {
                { "retention.ms", "2592000000" },  // 30 days
                { "compression.type", "snappy" }
            }
        };

        /// <summary>
        /// Topic: payments
        /// - 5 partitions => higher throughput
        /// - Key: userId => routing by user
        /// - Học: Throughput vs ordering tradeoff
        /// </summary>
        public static KafkaTopicConfig Payments => new()
        {
            Name = "payments",
            NumPartitions = 5,
            ReplicationFactor = 1,
            Configs = new Dictionary<string, string>
            {
                { "retention.ms", "2592000000" },  // 30 days
                { "compression.type", "snappy" }
            }
        };

        /// <summary>
        /// Topic: notifications
        /// - 1 partition => no ordering requirement, high throughput
        /// - Key: null => round-robin distribution
        /// - Học: No-key publishing
        /// </summary>
        public static KafkaTopicConfig Notifications => new()
        {
            Name = "notifications",
            NumPartitions = 1,
            ReplicationFactor = 1,
            Configs = new Dictionary<string, string>
            {
                { "retention.ms", "86400000" }  // 1 day
            }
        };

        /// <summary>
        /// Topic: order-processing.DLQ (Dead Letter Queue)
        /// - 3 partitions
        /// - Học: Error handling & poison message pattern
        /// </summary>
        public static KafkaTopicConfig OrderProcessingDLQ => new()
        {
            Name = "order-processing.DLQ",
            NumPartitions = 3,
            ReplicationFactor = 1,
            Configs = new Dictionary<string, string>
            {
                { "retention.ms", "7776000000" }  // 90 days (keep for analysis)
            }
        };

        /// <summary>
        /// Lấy tất cả topics cần tạo
        /// </summary>
        public static List<KafkaTopicConfig> GetAllTopics()
        {
            return new List<KafkaTopicConfig>
            {
                UserEvents,
                Orders,
                Payments,
                Notifications,
                OrderProcessingDLQ
            };
        }
    }
}
