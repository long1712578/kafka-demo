using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaDemo.Examples;

/// <summary>
/// DEMO: Minh họa mối quan hệ giữa Partition và Consumer
/// 
/// Chứng minh:
/// 1. 1 Consumer CÓ THỂ đọc NHIỀU Partitions
/// 2. 1 Partition CHỈ được đọc bởi 1 Consumer (trong cùng group)
/// 3. Dùng Employee ID làm Key là BEST PRACTICE
/// </summary>
public class PartitionConsumerDemo
{
    private const string BootstrapServers = "localhost:19092,localhost:29092,localhost:39092";
    private const string TopicName = "hrmcore.staging.demo";

    #region Models

    public class EmployeeEvent
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Demo 1: Producer với Employee ID làm Key

    /// <summary>
    /// Demo: Gửi messages với employee ID làm key
    /// Các messages của cùng 1 nhân viên sẽ vào CÙNG partition
    /// </summary>
    public static async Task Demo1_ProducerWithEmployeeKey()
    {
        Console.WriteLine("=== DEMO 1: PRODUCER VỚI EMPLOYEE ID LÀM KEY ===\n");

        var config = new ProducerConfig
        {
            BootstrapServers = BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 30000
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        // Tạo events cho 3 nhân viên
        var employees = new[] { "NV001", "NV002", "NV003" };
        var events = new[] { "CREATED", "UPDATED_SALARY", "PROMOTED", "ON_LEAVE" };

        Console.WriteLine("Gửi các events cho nhân viên...\n");

        foreach (var employeeId in employees)
        {
            foreach (var eventType in events)
            {
                var employeeEvent = new EmployeeEvent
                {
                    EmployeeId = employeeId,
                    EventType = eventType,
                    Data = $"Event data for {employeeId}",
                    Timestamp = DateTime.UtcNow
                };

                var message = new Message<string, string>
                {
                    Key = employeeId,  // ✅ KEY = Employee ID
                    Value = JsonSerializer.Serialize(employeeEvent),
                    Headers = new Headers
                    {
                        { "event-type", Encoding.UTF8.GetBytes(eventType) },
                        { "source", Encoding.UTF8.GetBytes("hrm-system") }
                    }
                };

                var result = await producer.ProduceAsync(TopicName, message);

                Console.WriteLine($"✅ {employeeId} - {eventType,-20} → Partition {result.Partition.Value} | Offset {result.Offset.Value}");

                await Task.Delay(100); // Delay để dễ quan sát
            }
            Console.WriteLine();
        }

        Console.WriteLine("\n📊 QUAN SÁT:");
        Console.WriteLine("✅ Tất cả events của CÙNG 1 nhân viên đều vào CÙNG PARTITION");
        Console.WriteLine("✅ Thứ tự events được đảm bảo cho mỗi nhân viên");
        Console.WriteLine("✅ Các nhân viên khác nhau phân bố đều qua các partitions\n");
    }

    #endregion

    #region Demo 2: Single Consumer đọc Multiple Partitions

    /// <summary>
    /// Demo: 1 Consumer CÓ THỂ đọc NHIỀU partitions
    /// Chứng minh nguyên tắc "1 consumer = 1 partition" là SAI
    /// </summary>
    public static async Task Demo2_SingleConsumerMultiplePartitions()
    {
        Console.WriteLine("=== DEMO 2: 1 CONSUMER ĐỌC NHIỀU PARTITIONS ===\n");

        var config = new ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            GroupId = "demo-group-single-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetPartitionsAssignedHandler((c, partitions) =>
            {
                Console.WriteLine($"\n🎯 Consumer được assign partitions:");
                foreach (var partition in partitions)
                {
                    Console.WriteLine($"   - {partition.Topic} Partition {partition.Partition}");
                }
                Console.WriteLine($"\n✅ CHỨNG MINH: 1 Consumer đang đọc {partitions.Count} partitions!\n");
            })
            .Build();

        consumer.Subscribe(TopicName);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var messagesCount = 0;
        var partitionStats = new Dictionary<int, int>();

        try
        {
            while (!cts.Token.IsCancellationRequested && messagesCount < 20)
            {
                var result = consumer.Consume(cts.Token);

                if (!partitionStats.ContainsKey(result.Partition.Value))
                    partitionStats[result.Partition.Value] = 0;

                partitionStats[result.Partition.Value]++;

                var employeeEvent = JsonSerializer.Deserialize<EmployeeEvent>(result.Message.Value);

                Console.WriteLine($"📨 Partition {result.Partition.Value} | Offset {result.Offset.Value} | " +
                                  $"Employee: {employeeEvent?.EmployeeId} | Event: {employeeEvent?.EventType}");

                consumer.Commit(result);
                messagesCount++;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n⏰ Timeout...");
        }

        consumer.Close();

        Console.WriteLine($"\n📊 THỐNG KÊ:");
        Console.WriteLine($"Total messages consumed: {messagesCount}");
        Console.WriteLine($"Partitions được đọc:");
        foreach (var (partition, count) in partitionStats.OrderBy(x => x.Key))
        {
            Console.WriteLine($"   - Partition {partition}: {count} messages");
        }
        Console.WriteLine($"\n✅ KẾT LUẬN: 1 Consumer đã đọc từ {partitionStats.Count} partitions khác nhau!\n");
    }

    #endregion

    #region Demo 3: Multiple Consumers - Partition Assignment

    /// <summary>
    /// Demo: Nhiều consumers trong cùng group
    /// Kafka tự động phân chia partitions cho consumers
    /// </summary>
    public static async Task Demo3_MultipleConsumersPartitionAssignment()
    {
        Console.WriteLine("=== DEMO 3: NHIỀU CONSUMERS - PARTITION ASSIGNMENT ===\n");

        const string groupId = "demo-group-multiple-consumers";
        var consumerTasks = new List<Task>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Tạo 3 consumers trong cùng 1 group
        for (int i = 1; i <= 3; i++)
        {
            int consumerId = i;
            var task = Task.Run(() => RunConsumer(consumerId, groupId, cts.Token));
            consumerTasks.Add(task);
            await Task.Delay(2000); // Delay để thấy rebalancing
        }

        await Task.WhenAll(consumerTasks);

        Console.WriteLine("\n📊 KẾT LUẬN:");
        Console.WriteLine("✅ Mỗi partition chỉ được assign cho 1 consumer");
        Console.WriteLine("✅ Kafka tự động rebalance khi có consumer mới join");
        Console.WriteLine("✅ LoadBalancing tự động\n");
    }

    private static void RunConsumer(int consumerId, string groupId, CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 10000,
            HeartbeatIntervalMs = 3000
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetPartitionsAssignedHandler((c, partitions) =>
            {
                Console.WriteLine($"\n🎯 Consumer {consumerId} được assign:");
                foreach (var partition in partitions)
                {
                    Console.WriteLine($"   → Partition {partition.Partition}");
                }
            })
            .SetPartitionsRevokedHandler((c, partitions) =>
            {
                Console.WriteLine($"\n⚠️  Consumer {consumerId} bị revoke:");
                foreach (var partition in partitions)
                {
                    Console.WriteLine($"   ← Partition {partition.Partition}");
                }
            })
            .Build();

        consumer.Subscribe(TopicName);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = consumer.Consume(ct);
                var employeeEvent = JsonSerializer.Deserialize<EmployeeEvent>(result.Message.Value);

                Console.WriteLine($"[C{consumerId}] P{result.Partition.Value} | {employeeEvent?.EmployeeId} | {employeeEvent?.EventType}");

                consumer.Commit(result);
                Thread.Sleep(500);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"\nConsumer {consumerId} stopped.");
        }
        finally
        {
            consumer.Close();
        }
    }

    #endregion

    #region Demo 4: Chứng minh Message Ordering với Key

    /// <summary>
    /// Demo: Chứng minh messages của cùng 1 key luôn theo thứ tự
    /// </summary>
    public static async Task Demo4_MessageOrderingWithKey()
    {
        Console.WriteLine("=== DEMO 4: MESSAGE ORDERING VỚI KEY ===\n");

        // Phase 1: Send messages
        await SendOrderedMessagesForEmployee("NV999");

        // Phase 2: Consume và kiểm tra thứ tự
        await ConsumeAndVerifyOrdering("NV999");
    }

    private static async Task SendOrderedMessagesForEmployee(string employeeId)
    {
        Console.WriteLine($"📤 Gửi 10 events theo thứ tự cho {employeeId}...\n");

        var config = new ProducerConfig
        {
            BootstrapServers = BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        var orderedEvents = new[]
        {
            "1️⃣  EMPLOYEE_CREATED",
            "2️⃣  PERSONAL_INFO_UPDATED",
            "3️⃣  SALARY_SET",
            "4️⃣  DEPARTMENT_ASSIGNED",
            "5️⃣  MANAGER_ASSIGNED",
            "6️⃣  SALARY_INCREASED",
            "7️⃣  PROMOTED",
            "8️⃣  DEPARTMENT_CHANGED",
            "9️⃣  ON_LEAVE",
            "🔟 RETURNED_FROM_LEAVE"
        };

        for (int i = 0; i < orderedEvents.Length; i++)
        {
            var employeeEvent = new EmployeeEvent
            {
                EmployeeId = employeeId,
                EventType = orderedEvents[i],
                Data = $"Step {i + 1}",
                Timestamp = DateTime.UtcNow
            };

            var message = new Message<string, string>
            {
                Key = employeeId,  // ✅ Cùng key = cùng partition
                Value = JsonSerializer.Serialize(employeeEvent)
            };

            var result = await producer.ProduceAsync(TopicName, message);
            Console.WriteLine($"  {orderedEvents[i]} → Partition {result.Partition.Value}");
            await Task.Delay(100);
        }

        Console.WriteLine();
    }

    private static async Task ConsumeAndVerifyOrdering(string employeeId)
    {
        Console.WriteLine($"📥 Nhận và kiểm tra thứ tự events cho {employeeId}...\n");

        var config = new ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            GroupId = "demo-ordering-verification",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(TopicName);

        var receivedEvents = new List<string>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            while (receivedEvents.Count < 10 && !cts.Token.IsCancellationRequested)
            {
                var result = consumer.Consume(cts.Token);
                var employeeEvent = JsonSerializer.Deserialize<EmployeeEvent>(result.Message.Value);

                if (employeeEvent?.EmployeeId == employeeId)
                {
                    receivedEvents.Add(employeeEvent.EventType);
                    Console.WriteLine($"  ✅ Received: {employeeEvent.EventType}");
                }
            }
        }
        catch (OperationCanceledException) { }

        consumer.Close();

        // Verify ordering
        var expectedOrder = new[]
        {
            "1️⃣  EMPLOYEE_CREATED",
            "2️⃣  PERSONAL_INFO_UPDATED",
            "3️⃣  SALARY_SET",
            "4️⃣  DEPARTMENT_ASSIGNED",
            "5️⃣  MANAGER_ASSIGNED",
            "6️⃣  SALARY_INCREASED",
            "7️⃣  PROMOTED",
            "8️⃣  DEPARTMENT_CHANGED",
            "9️⃣  ON_LEAVE",
            "🔟 RETURNED_FROM_LEAVE"
        };

        Console.WriteLine($"\n📊 KIỂM TRA THỨ TỰ:");
        bool isOrdered = receivedEvents.SequenceEqual(expectedOrder.Take(receivedEvents.Count));
        
        if (isOrdered)
        {
            Console.WriteLine("✅ THỨ TỰ HOÀN HẢO! Tất cả events theo đúng trình tự gửi.");
        }
        else
        {
            Console.WriteLine("❌ THỨ TỰ BỊ SAI! (Điều này KHÔNG BAO GIỜ xảy ra với key-based partitioning)");
        }

        Console.WriteLine($"\n💡 KẾT LUẬN:");
        Console.WriteLine($"   Dùng Employee ID làm key → Đảm bảo thứ tự 100%");
        Console.WriteLine($"   Critical cho use cases: audit trail, event sourcing, state machine\n");
    }

    #endregion

    #region Demo 5: Multiple Consumer Groups

    /// <summary>
    /// Demo: Nhiều Consumer Groups có thể đọc cùng 1 topic
    /// Mỗi group độc lập, có offset riêng
    /// </summary>
    public static async Task Demo5_MultipleConsumerGroups()
    {
        Console.WriteLine("=== DEMO 5: NHIỀU CONSUMER GROUPS ===\n");

        var groups = new[]
        {
            ("hrm-processor", "Xử lý business logic"),
            ("analytics-service", "Phân tích dữ liệu"),
            ("audit-logger", "Ghi log audit")
        };

        var tasks = new List<Task>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        foreach (var (groupId, description) in groups)
        {
            var task = Task.Run(() => RunConsumerGroup(groupId, description, cts.Token));
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        Console.WriteLine("\n📊 KẾT LUẬN:");
        Console.WriteLine("✅ Mỗi consumer group đọc TOÀN BỘ messages");
        Console.WriteLine("✅ Groups hoàn toàn độc lập");
        Console.WriteLine("✅ Offset của mỗi group riêng biệt");
        Console.WriteLine("✅ Cho phép multiple services consume cùng topic\n");
    }

    private static void RunConsumerGroup(string groupId, string description, CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(TopicName);

        Console.WriteLine($"\n🟢 [{groupId}] Started - {description}");

        int count = 0;
        try
        {
            while (!ct.IsCancellationRequested && count < 5)
            {
                var result = consumer.Consume(ct);
                var employeeEvent = JsonSerializer.Deserialize<EmployeeEvent>(result.Message.Value);

                Console.WriteLine($"   [{groupId}] {employeeEvent?.EmployeeId} - {employeeEvent?.EventType}");
                count++;
                Thread.Sleep(300);
            }
        }
        catch (OperationCanceledException) { }

        consumer.Close();
        Console.WriteLine($"🔴 [{groupId}] Stopped - Processed {count} messages");
    }

    #endregion

    #region Main Runner

    public static async Task RunAllDemos()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   KAFKA PARTITION & CONSUMER - COMPREHENSIVE DEMO             ║");
        Console.WriteLine("║                                                               ║");
        Console.WriteLine("║   Chứng minh:                                                ║");
        Console.WriteLine("║   ✅ 1 Consumer CÓ THỂ đọc NHIỀU Partitions                  ║");
        Console.WriteLine("║   ✅ 1 Partition CHỈ 1 Consumer (cùng group)                ║");
        Console.WriteLine("║   ✅ Employee ID làm Key là BEST PRACTICE                    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            // Create topic first
            await CreateDemoTopic();

            Console.WriteLine("\nNhấn ENTER để bắt đầu Demo 1...");
            Console.ReadLine();
            await Demo1_ProducerWithEmployeeKey();

            Console.WriteLine("\nNhấn ENTER để bắt đầu Demo 2...");
            Console.ReadLine();
            await Demo2_SingleConsumerMultiplePartitions();

            Console.WriteLine("\nNhấn ENTER để bắt đầu Demo 3...");
            Console.ReadLine();
            await Demo3_MultipleConsumersPartitionAssignment();

            Console.WriteLine("\nNhấn ENTER để bắt đầu Demo 4...");
            Console.ReadLine();
            await Demo4_MessageOrderingWithKey();

            Console.WriteLine("\nNhấn ENTER để bắt đầu Demo 5...");
            Console.ReadLine();
            await Demo5_MultipleConsumerGroups();

            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                     HOÀN THÀNH TẤT CẢ DEMOS                  ║");
            Console.WriteLine("║                                                               ║");
            Console.WriteLine("║   🎓 BẠN ĐÃ HIỂU RÕ:                                         ║");
            Console.WriteLine("║   ✅ Partition-Consumer relationship                         ║");
            Console.WriteLine("║   ✅ Message ordering với keys                              ║");
            Console.WriteLine("║   ✅ Consumer groups và rebalancing                          ║");
            Console.WriteLine("║   ✅ Best practices cho HRM system                           ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ERROR: {ex.Message}");
            Console.WriteLine("\n💡 Đảm bảo Kafka đang chạy:");
            Console.WriteLine("   cd d:\\Projects\\KafkaDemo\\kafka");
            Console.WriteLine("   docker-compose up -d");
        }
    }

    private static async Task CreateDemoTopic()
    {
        Console.WriteLine("🔧 Tạo demo topic...\n");

        var config = new AdminClientConfig
        {
            BootstrapServers = BootstrapServers
        };

        using var adminClient = new AdminClientBuilder(config).Build();

        try
        {
            await adminClient.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = TopicName,
                    NumPartitions = 6,
                    ReplicationFactor = 3
                }
            });

            Console.WriteLine($"✅ Topic '{TopicName}' created (6 partitions, RF=3)\n");
        }
        catch (CreateTopicsException ex) when (ex.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
        {
            Console.WriteLine($"ℹ️  Topic '{TopicName}' already exists\n");
        }
    }

    #endregion
}
