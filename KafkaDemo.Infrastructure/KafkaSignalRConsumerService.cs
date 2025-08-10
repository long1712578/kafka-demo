using Confluent.Kafka;
using KafkaDemo.Core.Models;
using KafkaDemo.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KafkaDemo.Infrastructure
{
    public class KafkaSignalRConsumerService(IHubContext<ChatHub> _hubContext, ILogger<KafkaConsumerService> _logger) : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await foreach (var msg in consumer.ConsumeMessages("chat-topic", stoppingToken))
            //{
            //    await hubContext.Clients.All.SendAsync("ReceiveMessage", msg.User, msg.Text);
            //}

            return Task.Run(() =>
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = KafkaConfig.BootstrapServers,
                    GroupId = KafkaConfig.GroupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false,
                };

                using var consumer = new ConsumerBuilder<Ignore, string>(config)
                    .SetErrorHandler((_, e) =>
                    {
                        _logger.LogError($"Kafka error: {e.Reason}");
                        Console.WriteLine($"Kafka error: {e.Reason}");
                    })
                    .Build();

                consumer.Subscribe(KafkaConfig.TopicOfChat);
                _logger.LogInformation($"Kafka consumer subscribed to topic: {KafkaConfig.TopicOfChat}");
                Console.WriteLine("Consumer {0} started...", KafkaConfig.TopicOfChat);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(stoppingToken);
                        if (result != null)
                        {
                            _logger.LogInformation($"Received message: {result.Message.Value}");

                            // Giả sử message là JSON có User và Text
                            var message = JsonSerializer.Deserialize<ChatMessage>(result.Message.Value);

                            // Broadcast tới tất cả client SignalR
                            _hubContext.Clients.All.SendAsync(
                                "ReceiveMessage",
                                message?.User,
                                message?.Text,
                                cancellationToken: stoppingToken
                            );

                            consumer.Commit(result);
                        }
                        }
                    catch (ConsumeException e)
                    {
                        _logger.LogError($"Error consuming message: {e.Error.Reason}");
                        var result = consumer.Consume(stoppingToken);
                    }
                }
            });
        }
    }
}
