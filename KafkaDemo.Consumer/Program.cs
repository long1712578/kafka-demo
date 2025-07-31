using Confluent.Kafka;
using KafkaDemo.Core.Models;
using System.Text.Json;

var config = new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "demo-group-2",
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnablePartitionEof = true
};

using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
consumer.Subscribe("demo-topic");

Console.WriteLine("Consumer started...");
while (true)
{
    try
    {
        var result = consumer.Consume();
        if (result?.Message?.Value != null)
        {
            var message = JsonSerializer.Deserialize<KafkaMessage>(result.Message.Value);
            Console.WriteLine($"[Partition:{result.Partition.Value}] Received: {message.Content}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}