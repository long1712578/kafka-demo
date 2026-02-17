using KafkaDemo.Core.Models;
using KafkaDemo.Infrastructure.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaDemo.Infrastructure
{
    /// <summary>
    /// Task 1.1: Topic Provisioning Service
    /// Tự động tạo topics khi application startup
    /// 
    /// Cách hoạt động:
    /// 1. Khi app start => InitializeTopicsAsync được gọi
    /// 2. Kiểm tra topic đã tồn tại chưa
    /// 3. Nếu chưa => tạo topic với config từ ModuleATopics
    /// 4. Log output chi tiết về mỗi partition
    /// </summary>
    public class KafkaTopicProvisioningService : IHostedService
    {
        private readonly KafkaAdminService _adminService;
        private readonly ILogger<KafkaTopicProvisioningService> _logger;
        private readonly string _bootstrapServers;

        public KafkaTopicProvisioningService(
            string bootstrapServers,
            ILogger<KafkaTopicProvisioningService> logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapServers);
            _bootstrapServers = bootstrapServers;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _adminService = new KafkaAdminService(bootstrapServers, logger);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🚀 [Task 1.1] Starting Kafka Topic Provisioning...\n");

            try
            {
                await InitializeTopicsAsync(cancellationToken);
                _logger.LogInformation("✅ [Task 1.1] Topic Provisioning completed successfully!\n");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Task 1.1] Topic Provisioning failed!");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Kafka Topic Provisioning Service stopping...");
            return Task.CompletedTask;
        }

        private async Task InitializeTopicsAsync(CancellationToken cancellationToken)
        {
            var topics = ModuleATopics.GetAllTopics();
            var existingTopics = await _adminService.ListTopicsAsync();

            _logger.LogInformation($"📋 Existing topics: {string.Join(", ", existingTopics)}\n");

            foreach (var topicConfig in topics)
            {
                if (existingTopics.Contains(topicConfig.Name))
                {
                    _logger.LogInformation($"⏭️  Topic '{topicConfig.Name}' already exists, skipping creation");
                    
                    // Log partition information
                    try
                    {
                        await _adminService.GetTopicMetadataAsync(topicConfig.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"⚠️  Could not get metadata for '{topicConfig.Name}': {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogInformation($"\n📝 Creating topic: {topicConfig.Name}");
                    _logger.LogInformation($"   └─ Partitions: {topicConfig.NumPartitions}");
                    _logger.LogInformation($"   └─ Replication Factor: {topicConfig.ReplicationFactor}");
                    _logger.LogInformation($"   └─ Configs: {string.Join(", ", topicConfig.Configs.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

                    try
                    {
                        await _adminService.CreateTopicAsync(
                            topicConfig.Name,
                            topicConfig.NumPartitions,
                            topicConfig.ReplicationFactor,
                            topicConfig.Configs);

                        _logger.LogInformation($"✅ Topic '{topicConfig.Name}' created successfully\n");

                        // Get and log partition metadata
                        await Task.Delay(500);  // Wait for topic to be fully created
                        try
                        {
                            await _adminService.GetTopicMetadataAsync(topicConfig.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"⚠️  Could not get metadata immediately after creation: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"❌ Failed to create topic '{topicConfig.Name}': {ex.Message}");
                        // Continue with next topic instead of failing completely
                    }
                }
            }
        }
    }
}
