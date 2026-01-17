using Confluent.Kafka;
using KafkaDemo.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KafkaDemo.Infrastructure.Consumers
{
    /// <summary>
    /// Advanced Kafka Consumer với Manual Commit và Error Handling
    /// Học về: Consumer Groups, Offset Management, Rebalancing, Partition Assignment
    /// </summary>
    public class AdvancedKafkaConsumer : BackgroundService
    {
        private readonly ILogger<AdvancedKafkaConsumer> _logger;
        private readonly string _bootstrapServers;
        private readonly string _groupId;
        private readonly string _topic;
        private IConsumer<string, string>? _consumer;

        public AdvancedKafkaConsumer(
            string bootstrapServers,
            string groupId,
            string topic,
            ILogger<AdvancedKafkaConsumer> logger)
        {
            _bootstrapServers = bootstrapServers;
            _groupId = groupId;
            _topic = topic;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => StartConsumer(stoppingToken), stoppingToken);
        }

        private void StartConsumer(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                
                // OFFSET MANAGEMENT
                EnableAutoCommit = false,                    // Manual commit for precise control
                AutoOffsetReset = AutoOffsetReset.Earliest,  // Start from earliest if no offset
                
                // SESSION & HEARTBEAT (for rebalancing)
                SessionTimeoutMs = 45000,                    // 45s session timeout
                HeartbeatIntervalMs = 3000,                  // 3s heartbeat
                MaxPollIntervalMs = 300000,                  // 5 min max poll interval
                
                // FETCH SETTINGS
                FetchMinBytes = 1,                           // Min bytes per fetch
                FetchWaitMaxMs = 500,                        // Max wait time for fetch
                
                // PARTITION ASSIGNMENT
                PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky, // Cooperative rebalancing
                
                // ISOLATION LEVEL (for transactional messages)
                IsolationLevel = IsolationLevel.ReadCommitted, // Only read committed messages
                
                ClientId = $"advanced-consumer-{_groupId}-{Guid.NewGuid().ToString()[..8]}"
            };

            _consumer = new ConsumerBuilder<string, string>(config)
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    _logger.LogInformation($"🔄 Partitions ASSIGNED: {string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition.Value}]"))}");
                    
                    // ✅ FIX: Handler should be void (no return) for default behavior
                    // If you need custom offsets, use c.Assign(customOffsets) inside handler
                    
                    // Example for custom offset initialization:
                    // var customOffsets = partitions.Select(p => 
                    //     new TopicPartitionOffset(p, Offset.Beginning)).ToList();
                    // c.Assign(customOffsets);
                    
                    // For default behavior (use stored offsets), just log - no return needed
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    _logger.LogWarning($"⚠️  Partitions REVOKED: {string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition.Value}]"))}");
                    
                    // Commit before rebalancing
                    try
                    {
                        c.Commit(partitions);
                        _logger.LogInformation("✅ Offsets committed before rebalancing");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"❌ Failed to commit before rebalancing: {ex.Message}");
                    }
                })
                .SetPartitionsLostHandler((c, partitions) =>
                {
                    _logger.LogError($"❌ Partitions LOST: {string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition.Value}]"))}");
                    // Partitions lost due to timeout - cannot commit
                })
                .SetErrorHandler((c, e) =>
                {
                    _logger.LogError($"❌ Consumer Error: {e.Reason}, Code: {e.Code}, IsFatal: {e.IsFatal}");
                    
                    if (e.IsFatal)
                    {
                        _logger.LogCritical("🔥 FATAL ERROR - Consumer will be recreated");
                    }
                })
                .SetOffsetsCommittedHandler((c, offsets) =>
                {
                    if (offsets.Error.IsError)
                    {
                        _logger.LogError($"❌ Offset commit error: {offsets.Error.Reason}");
                    }
                    else
                    {
                        _logger.LogDebug($"✅ Offsets committed: {string.Join(", ", offsets.Offsets.Select(o => $"{o.Topic}[{o.Partition.Value}]@{o.Offset.Value}"))}");
                    }
                })
                .SetStatisticsHandler((c, json) =>
                {
                    _logger.LogTrace($"📊 Consumer Statistics: {json}");
                })
                .Build();

            _consumer.Subscribe(_topic);
            _logger.LogInformation($"✅ Consumer subscribed to topic: {_topic} with group: {_groupId}");

            // Message processing loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult?.Message == null)
                        continue;

                    // Process message
                    // ✅ FIX: Cannot use await in non-async method
                    // Solution: Use .GetAwaiter().GetResult() or make StartConsumer async
                    ProcessMessageAsync(consumeResult, stoppingToken).GetAwaiter().GetResult();

                    // Manual commit after successful processing
                    try
                    {
                        _consumer.Commit(consumeResult);
                        _logger.LogDebug($"✅ Committed offset: {consumeResult.TopicPartitionOffset}");
                    }
                    catch (KafkaException ex)
                    {
                        _logger.LogError($"❌ Commit failed: {ex.Error.Reason}");
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError($"❌ Consume error at {ex.ConsumerRecord?.TopicPartitionOffset}: {ex.Error.Reason}");
                    
                    // Handle specific errors
                    if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                    {
                        _logger.LogWarning("⚠️  Topic doesn't exist - waiting...");
                        Thread.Sleep(5000);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Consumer operation cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Unexpected error: {ex.Message}");
                }
            }

            _logger.LogInformation("Consumer shutting down...");
        }

        private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
        {
            try
            {
                var message = JsonSerializer.Deserialize<KafkaMessage>(result.Message.Value);
                
                _logger.LogInformation(
                    $"📨 Received Message:" +
                    $"\n   Topic: {result.Topic}" +
                    $"\n   Partition: {result.Partition.Value}" +
                    $"\n   Offset: {result.Offset.Value}" +
                    $"\n   Key: {result.Message.Key}" +
                    $"\n   Timestamp: {result.Message.Timestamp.UtcDateTime}" +
                    $"\n   Content: {message?.Content}");

                // Read headers
                if (result.Message.Headers != null)
                {
                    foreach (var header in result.Message.Headers)
                    {
                        var value = System.Text.Encoding.UTF8.GetString(header.GetValueBytes());
                        _logger.LogDebug($"   Header: {header.Key} = {value}");
                    }
                }

                // Simulate processing
                await Task.Delay(100, cancellationToken);

                // TODO: Add your business logic here
            }
            catch (JsonException ex)
            {
                _logger.LogError($"❌ Failed to deserialize message: {ex.Message}");
                // Could publish to DLQ (Dead Letter Queue) here
            }
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing consumer...");
            
            if (_consumer != null)
            {
                // Commit final offsets
                try
                {
                    _consumer.Commit();
                    _logger.LogInformation("✅ Final offsets committed");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Failed to commit final offsets: {ex.Message}");
                }

                _consumer.Close();
                _consumer.Dispose();
            }

            base.Dispose();
        }
    }
}
