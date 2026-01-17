using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace KafkaDemo.Infrastructure.Admin
{
    /// <summary>
    /// Kafka Admin Client - Quản lý Topics, Partitions, Configs, Consumer Groups
    /// Học về: Cluster management, Topic operations, Consumer group monitoring
    /// </summary>
    public class KafkaAdminService
    {
        private readonly IAdminClient _adminClient;
        private readonly ILogger<KafkaAdminService> _logger;

        public KafkaAdminService(string bootstrapServers, ILogger<KafkaAdminService> logger)
        {
            _logger = logger;
            var config = new AdminClientConfig
            {
                BootstrapServers = bootstrapServers,
                ClientId = $"admin-client-{Guid.NewGuid().ToString()[..8]}"
            };

            _adminClient = new AdminClientBuilder(config).Build();
            _logger.LogInformation("✅ Kafka Admin Client initialized");
        }

        #region Topic Operations

        /// <summary>
        /// Tạo topic mới với custom configuration
        /// </summary>
        public async Task CreateTopicAsync(
            string topicName,
            int numPartitions = 3,
            short replicationFactor = 3,
            Dictionary<string, string>? configs = null)
        {
            try
            {
                var topicSpec = new TopicSpecification
                {
                    Name = topicName,
                    NumPartitions = numPartitions,
                    ReplicationFactor = replicationFactor,
                    Configs = configs ?? new Dictionary<string, string>()
                };

                await _adminClient.CreateTopicsAsync(new[] { topicSpec });
                _logger.LogInformation($"✅ Topic created: {topicName} ({numPartitions} partitions, RF={replicationFactor})");
            }
            catch (CreateTopicsException ex)
            {
                _logger.LogError($"❌ Failed to create topic {topicName}: {ex.Results[0].Error.Reason}");
                throw;
            }
        }

        /// <summary>
        /// Xóa topic
        /// </summary>
        public async Task DeleteTopicAsync(string topicName)
        {
            try
            {
                await _adminClient.DeleteTopicsAsync(new[] { topicName });
                _logger.LogInformation($"✅ Topic deleted: {topicName}");
            }
            catch (DeleteTopicsException ex)
            {
                _logger.LogError($"❌ Failed to delete topic {topicName}: {ex.Results[0].Error.Reason}");
                throw;
            }
        }

        /// <summary>
        /// List tất cả topics
        /// </summary>
        public async Task<List<string>> ListTopicsAsync()
        {
            try
            {
                var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                var topics = metadata.Topics
                    .Where(t => !t.Topic.StartsWith("__"))  // Exclude internal topics
                    .Select(t => t.Topic)
                    .ToList();

                _logger.LogInformation($"📋 Found {topics.Count} topics");
                return topics;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Failed to list topics: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get topic metadata (partitions, leaders, replicas)
        /// </summary>
        public async Task<TopicMetadata> GetTopicMetadataAsync(string topicName)
        {
            try
            {
                var metadata = _adminClient.GetMetadata(topicName, TimeSpan.FromSeconds(10));
                var topicMetadata = metadata.Topics.First();

                _logger.LogInformation(
                    $"📊 Topic: {topicMetadata.Topic}" +
                    $"\n   Partitions: {topicMetadata.Partitions.Count}" +
                    $"\n   Error: {topicMetadata.Error}");

                foreach (var partition in topicMetadata.Partitions)
                {
                    _logger.LogInformation(
                        $"   Partition {partition.PartitionId}:" +
                        $"\n      Leader: {partition.Leader}" +
                        $"\n      Replicas: [{string.Join(", ", partition.Replicas)}]" +
                        $"\n      ISR: [{string.Join(", ", partition.InSyncReplicas)}]");
                }

                return topicMetadata;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Failed to get topic metadata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tăng số partitions cho topic (chỉ tăng, không giảm được)
        /// </summary>
        public async Task IncreasePartitionsAsync(string topicName, int newPartitionCount)
        {
            try
            {
                var partitionsSpec = new PartitionsSpecification
                {
                    Topic = topicName,
                    IncreaseTo = newPartitionCount
                };

                await _adminClient.CreatePartitionsAsync(new[] { partitionsSpec });
                _logger.LogInformation($"✅ Increased partitions for {topicName} to {newPartitionCount}");
            }
            catch (CreatePartitionsException ex)
            {
                _logger.LogError($"❌ Failed to increase partitions: {ex.Results[0].Error.Reason}");
                throw;
            }
        }

        /// <summary>
        /// Update topic configuration
        /// </summary>
        public async Task AlterTopicConfigAsync(string topicName, Dictionary<string, string> configs)
        {
            try
            {
                var configEntries = configs.Select(kvp => 
                    new ConfigEntry { Name = kvp.Key, Value = kvp.Value }).ToList();

                var configResource = new ConfigResource
                {
                    Type = ResourceType.Topic,
                    Name = topicName
                };

                var alterConfigs = new Dictionary<ConfigResource, List<ConfigEntry>>
                {
                    { configResource, configEntries }
                };

                await _adminClient.AlterConfigsAsync(alterConfigs);
                _logger.LogInformation($"✅ Updated configuration for topic: {topicName}");
            }
            catch (AlterConfigsException ex)
            {
                _logger.LogError($"❌ Failed to alter topic config: {ex.Results[0].Error.Reason}");
                throw;
            }
        }

        #endregion

        #region Consumer Group Operations

        /// <summary>
        /// List tất cả consumer groups
        /// NOTE: Confluent.Kafka .NET client không có API để list ALL groups
        /// Bạn cần biết trước group IDs hoặc dùng kafka-consumer-groups CLI
        /// Alternative: Use Kafka UI (http://localhost:8080) to see all groups
        /// </summary>
        public Task<List<string>> ListConsumerGroupsAsync()
        {
            try
            {
                // ✅ LEARNING POINT: 
                // Kafka .NET Admin Client không có method để list ALL consumer groups
                // Workaround: Use metadata or keep track of your groups
                
                _logger.LogWarning("⚠️ ListConsumerGroups not available in .NET client");
                _logger.LogInformation("💡 Use Kafka UI at http://localhost:8080 to see all consumer groups");
                _logger.LogInformation("💡 Or use CLI: docker exec kafka-tools kafka-consumer-groups --list --bootstrap-server kafka1:9092");
                
                return Task.FromResult(new List<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Failed to list consumer groups: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get consumer group details (members, assignments, lag)
        /// </summary>
        public Task<GroupInfo?> DescribeConsumerGroupAsync(string groupId)
        {
            try
            {
                // ✅ FIX: Use ListGroup with group ID parameter
                return Task.Run(() =>
                {
                    var group = _adminClient.ListGroup(groupId, TimeSpan.FromSeconds(10));

                    if (group != null)
                    {
                        _logger.LogInformation(
                            $"👥 Consumer Group: {group.Group}" +
                            $"\n   Protocol Type: {group.ProtocolType}" +
                            $"\n   State: {group.State}" +
                            $"\n   Members: {group.Members?.Count ?? 0}");
                    }

                    return group;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Failed to describe consumer group: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Delete consumer group (must be empty)
        /// </summary>
        public async Task DeleteConsumerGroupAsync(string groupId)
        {
            try
            {
                await _adminClient.DeleteGroupsAsync(new[] { groupId });
                _logger.LogInformation($"✅ Consumer group deleted: {groupId}");
            }
            catch (DeleteGroupsException ex)
            {
                _logger.LogError($"❌ Failed to delete consumer group: {ex.Results[0].Error.Reason}");
                throw;
            }
        }

        /// <summary>
        /// Get consumer group offsets (lag monitoring)
        /// </summary>
        public async Task<Dictionary<TopicPartition, long>> GetConsumerGroupOffsetsAsync(string groupId, string topicName)
        {
            try
            {
                var consumer = new ConsumerBuilder<Ignore, Ignore>(new ConsumerConfig
                {
                    BootstrapServers = _adminClient.Name,
                    GroupId = groupId
                }).Build();

                var partitions = consumer.Assignment;
                var offsets = new Dictionary<TopicPartition, long>();

                foreach (var partition in partitions)
                {
                    var committed = consumer.Committed(new[] { partition }, TimeSpan.FromSeconds(5));
                    if (committed != null && committed.Any())
                    {
                        var offset = committed.First().Offset.Value;
                        offsets[partition] = offset;
                        _logger.LogInformation($"   {partition.Topic}[{partition.Partition}] -> Offset: {offset}");
                    }
                }

                consumer.Close();
                return offsets;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Failed to get consumer group offsets: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Cluster Operations

        /// <summary>
        /// Get cluster metadata (brokers, controller)
        /// </summary>
        public Metadata GetClusterMetadata()
        {
            try
            {
                var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));

                _logger.LogInformation($"🏢 Cluster Information:");
                _logger.LogInformation($"   Original Broker Name: {metadata.OriginatingBrokerName}");
                _logger.LogInformation($"   Brokers: {metadata.Brokers.Count}");

                foreach (var broker in metadata.Brokers)
                {
                    _logger.LogInformation(
                        $"   Broker {broker.BrokerId}:" +
                        $"\n      Host: {broker.Host}:{broker.Port}");
                }

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Failed to get cluster metadata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check cluster health
        /// </summary>
        public async Task<bool> IsClusterHealthyAsync()
        {
            try
            {
                var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                var isHealthy = metadata.Brokers.Count > 0;

                _logger.LogInformation($"🏥 Cluster Health: {(isHealthy ? "✅ Healthy" : "❌ Unhealthy")}");
                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Failed to check cluster health: {ex.Message}");
                return false;
            }
        }

        #endregion

        public void Dispose()
        {
            _adminClient?.Dispose();
            _logger.LogInformation("✅ Admin client disposed");
        }
    }
}
