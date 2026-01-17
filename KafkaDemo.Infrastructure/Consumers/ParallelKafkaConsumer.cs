using Confluent.Kafka;
using KafkaDemo.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace KafkaDemo.Infrastructure.Consumers
{
    /// <summary>
    /// Multi-threaded Kafka Consumer với Parallel Processing
    /// Học về: Concurrent message processing, Thread safety, Partition parallelism
    /// </summary>
    public class ParallelKafkaConsumer : BackgroundService
    {
        private readonly ILogger<ParallelKafkaConsumer> _logger;
        private readonly string _bootstrapServers;
        private readonly string _groupId;
        private readonly string _topic;
        private readonly int _degreeOfParallelism;
        private IConsumer<string, string>? _consumer;
        private readonly ConcurrentDictionary<TopicPartitionOffset, ConsumeResult<string, string>> _pendingCommits;

        public ParallelKafkaConsumer(
            string bootstrapServers,
            string groupId,
            string topic,
            int degreeOfParallelism,
            ILogger<ParallelKafkaConsumer> logger)
        {
            _bootstrapServers = bootstrapServers;
            _groupId = groupId;
            _topic = topic;
            _degreeOfParallelism = degreeOfParallelism;
            _logger = logger;
            _pendingCommits = new ConcurrentDictionary<TopicPartitionOffset, ConsumeResult<string, string>>();
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
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                SessionTimeoutMs = 45000,
                HeartbeatIntervalMs = 3000,
                MaxPollIntervalMs = 300000,
                
                // Optimize for parallel processing
                FetchMinBytes = 1024,        // Fetch more data per request
                FetchMaxBytes = 52428800,    // 50MB max fetch
                MaxPartitionFetchBytes = 1048576, // 1MB per partition
                
                PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky,
                IsolationLevel = IsolationLevel.ReadCommitted,
                ClientId = $"parallel-consumer-{_groupId}-{Guid.NewGuid().ToString()[..8]}"
            };

            _consumer = new ConsumerBuilder<string, string>(config)
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    _logger.LogInformation($"🔄 Assigned {partitions.Count} partitions for parallel processing");
                    // ✅ FIX: Handler should be void - no return statement
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    _logger.LogWarning($"⚠️  Revoking {partitions.Count} partitions - committing pending offsets");
                    CommitPendingOffsets();
                })
                .SetErrorHandler((c, e) =>
                {
                    _logger.LogError($"❌ Error: {e.Reason}");
                })
                .Build();

            _consumer.Subscribe(_topic);
            _logger.LogInformation($"✅ Parallel consumer started with {_degreeOfParallelism} threads");

            // Create semaphore to limit concurrent processing
            var semaphore = new SemaphoreSlim(_degreeOfParallelism, _degreeOfParallelism);
            var activeTasks = new List<Task>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult?.Message == null)
                    {
                        // No message - clean up completed tasks
                        CleanupCompletedTasks(activeTasks);
                        continue;
                    }

                    // Wait for available slot
                    semaphore.Wait(stoppingToken);

                    // Process message in parallel
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessMessageAsync(consumeResult, stoppingToken);
                            
                            // Track for commit
                            _pendingCommits.TryAdd(consumeResult.TopicPartitionOffset, consumeResult);
                            
                            // Commit in batches (every 100 messages or 5 seconds)
                            if (_pendingCommits.Count >= 100)
                            {
                                CommitPendingOffsets();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"❌ Processing failed: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, stoppingToken);

                    activeTasks.Add(task);
                    CleanupCompletedTasks(activeTasks);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError($"❌ Consume error: {ex.Error.Reason}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            // Wait for all tasks to complete
            _logger.LogInformation("Waiting for pending tasks to complete...");
            Task.WaitAll(activeTasks.ToArray(), TimeSpan.FromSeconds(30));
            CommitPendingOffsets();
        }

        private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
        {
            try
            {
                var message = JsonSerializer.Deserialize<KafkaMessage>(result.Message.Value);
                
                _logger.LogInformation(
                    $"📨 [Thread {Thread.CurrentThread.ManagedThreadId}] Processing:" +
                    $"\n   Partition: {result.Partition.Value}" +
                    $"\n   Offset: {result.Offset.Value}" +
                    $"\n   Content: {message?.Content}");

                // Simulate processing time
                await Task.Delay(Random.Shared.Next(100, 500), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Message processing error: {ex.Message}");
                throw;
            }
        }

        private void CommitPendingOffsets()
        {
            if (_pendingCommits.IsEmpty || _consumer == null)
                return;

            try
            {
                // Get latest offset per partition
                var offsetsToCommit = _pendingCommits.Values
                    .GroupBy(r => new { r.Topic, r.Partition })
                    .Select(g => g.OrderByDescending(r => r.Offset).First())
                    .Select(r => new TopicPartitionOffset(r.TopicPartition, r.Offset + 1))
                    .ToList();

                _consumer.Commit(offsetsToCommit);
                _logger.LogInformation($"✅ Committed {offsetsToCommit.Count} partition offsets");

                _pendingCommits.Clear();
            }
            catch (KafkaException ex)
            {
                _logger.LogError($"❌ Commit failed: {ex.Error.Reason}");
            }
        }

        private void CleanupCompletedTasks(List<Task> tasks)
        {
            tasks.RemoveAll(t => t.IsCompleted);
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing parallel consumer...");
            CommitPendingOffsets();
            _consumer?.Close();
            _consumer?.Dispose();
            base.Dispose();
        }
    }
}
