using KafkaDemo.Core.Interfaces;
using KafkaDemo.Core.Models;
using KafkaDemo.Infrastructure;
using KafkaDemo.Infrastructure.Admin;
using Microsoft.AspNetCore.Mvc;

namespace KafkaDemo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KafkaController : ControllerBase
    {
        private readonly IKafkaProducer _producer;
        private readonly ILogger<KafkaController> _logger;
        private readonly string _bootstrapServers;

        public KafkaController(IKafkaProducer producer, ILogger<KafkaController> logger, IConfiguration config)
        {
            _producer = producer;
            _logger = logger;
            _bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
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

        /// <summary>
        /// Task 1.1: List all topics
        /// GET /api/kafka/topics
        /// </summary>
        [HttpGet("topics")]
        public async Task<IActionResult> GetTopics()
        {
            try
            {
                var adminService = new KafkaAdminService(_bootstrapServers, _logger);
                var topics = await adminService.ListTopicsAsync();
                return Ok(new { topics, count = topics.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing topics");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Task 1.1: Get topic metadata (partitions, leaders, replicas)
        /// GET /api/kafka/topics/{topicName}/metadata
        /// </summary>
        [HttpGet("topics/{topicName}/metadata")]
        public async Task<IActionResult> GetTopicMetadata(string topicName)
        {
            try
            {
                var adminService = new KafkaAdminService(_bootstrapServers, _logger);
                var metadata = await adminService.GetTopicMetadataAsync(topicName);
                
                var partitionInfo = metadata.Partitions.Select(p => new
                {
                    PartitionId = p.PartitionId,
                    Leader = p.Leader,
                    Replicas = p.Replicas,
                    InSyncReplicas = p.InSyncReplicas
                }).ToList();

                return Ok(new
                {
                    topic = metadata.Topic,
                    partitionCount = metadata.Partitions.Count,
                    partitions = partitionInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting metadata for topic {topicName}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Task 1.1: Create a topic with custom configuration
        /// POST /api/kafka/topics
        /// Body: { "name": "my-topic", "numPartitions": 3, "replicationFactor": 1 }
        /// </summary>
        [HttpPost("topics")]
        public async Task<IActionResult> CreateTopic([FromBody] CreateTopicRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { error = "Topic name is required" });

                var adminService = new KafkaAdminService(_bootstrapServers, _logger);
                
                // Check if topic already exists
                var topics = await adminService.ListTopicsAsync();
                if (topics.Contains(request.Name))
                    return Conflict(new { error = $"Topic '{request.Name}' already exists" });

                await adminService.CreateTopicAsync(
                    request.Name,
                    request.NumPartitions ?? 3,
                    request.ReplicationFactor ?? 1,
                    request.Configs ?? new Dictionary<string, string>());

                return Created($"/api/kafka/topics/{request.Name}", 
                    new { message = $"Topic '{request.Name}' created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating topic");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Task 1.1: Initialize Module A demo topics
        /// POST /api/kafka/init-module-a-topics
        /// </summary>
        [HttpPost("init-module-a-topics")]
        public async Task<IActionResult> InitializeModuleATopics()
        {
            try
            {
                var adminService = new KafkaAdminService(_bootstrapServers, _logger);
                var existingTopics = await adminService.ListTopicsAsync();
                
                var modulaATopics = ModuleATopics.GetAllTopics();
                var results = new List<object>();

                foreach (var topicConfig in modulaATopics)
                {
                    if (existingTopics.Contains(topicConfig.Name))
                    {
                        results.Add(new
                        {
                            topic = topicConfig.Name,
                            status = "already_exists",
                            message = $"Topic '{topicConfig.Name}' already exists"
                        });
                    }
                    else
                    {
                        try
                        {
                            await adminService.CreateTopicAsync(
                                topicConfig.Name,
                                topicConfig.NumPartitions,
                                topicConfig.ReplicationFactor,
                                topicConfig.Configs);

                            results.Add(new
                            {
                                topic = topicConfig.Name,
                                status = "created",
                                partitions = topicConfig.NumPartitions,
                                replicationFactor = topicConfig.ReplicationFactor
                            });
                        }
                        catch (Exception ex)
                        {
                            results.Add(new
                            {
                                topic = topicConfig.Name,
                                status = "failed",
                                error = ex.Message
                            });
                        }
                    }
                }

                return Ok(new
                {
                    message = "Module A topics initialization completed",
                    results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Module A topics");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class CreateTopicRequest
    {
        public string Name { get; set; } = string.Empty;
        public int? NumPartitions { get; set; }
        public short? ReplicationFactor { get; set; }
        public Dictionary<string, string>? Configs { get; set; }
    }
}
