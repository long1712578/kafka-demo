# 🎓 Module A - Task 1.1: Complete Implementation Guide

## 📚 Nội dung học

### Topic / Partition / Segment - Kafka Architecture

```
Kafka Cluster
├── Broker 1 (Leader for some partitions)
├── Broker 2
└── Broker 3

Topic: user-events
├── Partition 0 [Leader: Broker 1]
│   └── Segment: [0-999], [1000-1999], [2000-2999], ...
│       Messages: [K: user-1, K: user-2, K: user-1, ...] (append-only log)
├── Partition 1 [Leader: Broker 2]
│   └── Messages: [K: user-3, K: user-1, K: user-2, ...]
└── Partition 2 [Leader: Broker 3]
    └── Messages: [K: user-2, K: user-3, K: user-1, ...]
```

### Key Concepts

**Topic** = Logical stream of related messages (like a channel/queue)

**Partition** = Physical shards for parallelism
- Mỗi partition là một **append-only log**
- Tuần tự, persistent, distributed across brokers
- **Ordering được đảm bảo TRONG 1 partition**

**Segment** = File trên disk (rotate based on size/time)
- Default: 1GB hoặc 7 days
- Retention policy: delete old segments by time/size

**Offset** = Message position within partition
```
Partition 0:
│ Offset 0: Message 1
│ Offset 1: Message 2  ← Consumer @ offset 1 (đã xử lý 0-1)
│ Offset 2: Message 3  ← Next message (offset 2)
│ Offset 3: Message 4
```

---

## ✅ Implementation Checklist

### File 1: KafkaTopicConfig.cs
- [x] Define `KafkaTopicConfig` model
- [x] Define `ModuleATopics` static class with 5 demo topics
- [x] Topics:
  - `user-events` (3 partitions) - Learn key-based partitioning
  - `orders` (3 partitions) - Learn ordering per entity
  - `payments` (5 partitions) - Learn throughput scaling
  - `notifications` (1 partition) - Learn no-key publishing
  - `order-processing.DLQ` (3 partitions) - Learn error handling

### File 2: KafkaTopicProvisioningService.cs
- [x] Implement `IHostedService`
- [x] Auto-create topics on app startup
- [x] Idempotent: skip if topic exists
- [x] Log partition metadata after creation
- [x] Error handling: continue if one topic fails

### File 3: Program.cs (API)
- [x] Register `KafkaTopicProvisioningService` as hosted service
- [x] Service runs automatically on app startup

### File 4: KafkaController.cs
- [x] `GET /api/kafka/topics` - List all topics
- [x] `GET /api/kafka/topics/{topicName}/metadata` - View partition details
- [x] `POST /api/kafka/topics` - Create custom topic
- [x] `POST /api/kafka/init-module-a-topics` - Initialize all Module A topics

### File 5: Task_1_1.http
- [x] REST API test requests for all endpoints

---

## 🚀 How to Run

### Step 1: Start Kafka
```bash
# Ensure Docker Compose with Kafka is running
docker-compose up -d

# Verify Kafka is running
docker ps | grep kafka
```

### Step 2: Run API Application
```bash
# From project root
dotnet run --project KafkaDemo.API

# Expected log output:
# 🚀 [Task 1.1] Starting Kafka Topic Provisioning...
# 📋 Existing topics: 
# 📝 Creating topic: user-events
#    └─ Partitions: 3
#    └─ Replication Factor: 1
#    └─ Configs: retention.ms=604800000, compression.type=snappy
# ✅ Topic 'user-events' created successfully
# ... (more topics) ...
# ✅ [Task 1.1] Topic Provisioning completed successfully!
```

### Step 3: Test Endpoints

#### Option A: Using REST Client (Visual Studio Code)
1. Open `KafkaDemo.API/Task_1_1.http`
2. Run requests sequentially using "Send Request" button

#### Option B: Using Swagger UI
1. Navigate to: `http://localhost:5224/swagger`
2. Expand `KafkaController`
3. Click on each endpoint to test

#### Option C: Using cURL
```bash
# List topics
curl http://localhost:5224/api/kafka/topics

# Get topic metadata
curl http://localhost:5224/api/kafka/topics/user-events/metadata

# Create custom topic
curl -X POST http://localhost:5224/api/kafka/topics \
  -H "Content-Type: application/json" \
  -d '{"name":"test-topic","numPartitions":3,"replicationFactor":1}'

# Initialize all Module A topics
curl -X POST http://localhost:5224/api/kafka/init-module-a-topics
```

---

## 📊 Expected Output

### Response 1: List Topics
```json
{
  "topics": [
    "user-events",
    "orders", 
    "payments",
    "notifications",
    "order-processing.DLQ"
  ],
  "count": 5
}
```

### Response 2: Get Topic Metadata (user-events)
```json
{
  "topic": "user-events",
  "partitionCount": 3,
  "partitions": [
    {
      "partitionId": 0,
      "leader": 1,
      "replicas": [1],
      "inSyncReplicas": [1]
    },
    {
      "partitionId": 1,
      "leader": 1,
      "replicas": [1],
      "inSyncReplicas": [1]
    },
    {
      "partitionId": 2,
      "leader": 1,
      "replicas": [1],
      "inSyncReplicas": [1]
    }
  ]
}
```

### Observation
- 3 Partitions = 3 independent logs
- Each partition managed by a Leader broker
- Messages with same key go to same partition
- Example:
  ```
  Message(Key="user-1") -> hash("user-1") % 3 = Partition X
  Message(Key="user-1") -> hash("user-1") % 3 = Partition X (same!)
  Message(Key="user-2") -> hash("user-2") % 3 = Partition Y (different)
  ```

---

## 💡 Learning Insights

### Why Partitions?
```
Single partition (Throughput bottleneck):
┌─────────────────┐
│ Partition 0     │ <= All producers write here sequentially
│ [msg1, msg2..] │
└─────────────────┘
Throughput: ~100k msg/s per partition

3 partitions (Parallelism):
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ Partition 0  │  │ Partition 1  │  │ Partition 2  │
│ [msg1, ...]  │  │ [msg5, ...]  │  │ [msg9, ...]  │
└──────────────┘  └──────────────┘  └──────────────┘
Throughput: ~300k msg/s (parallel writes)
```

### Why Key?
```
Without Key (Round-robin):
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ Partition 0  │  │ Partition 1  │  │ Partition 2  │
│ Order 101    │  │ Order 102    │  │ Order 103    │
│ Order 104    │  │ Order 105    │  │ Order 106    │
└──────────────┘  └──────────────┘  └──────────────┘
=> Order 101 at P0, Order 104 at P0 = NO ORDERING GUARANTEE
   (If processed in parallel, no guaranteed order)

With Key=OrderId:
┌────────────────────┐
│ Partition 0        │
│ Order 101          │ ← key=101
│ Order 104          │ ← key=104 (different partition)
│ Order 107          │ ← key=107 (different partition)
└────────────────────┘

┌────────────────────┐
│ Partition 1        │
│ Order 102          │ ← key=102
│ Order 105          │ ← key=105
└────────────────────┘

=> All Order 101 messages in P0, all Order 104 in P1
=> Within each partition: ORDERING GUARANTEED
```

---

## 🔍 Kafka Partitioning Hash Formula

```csharp
partition = hash(key) % num_partitions

Example:
  Key="user-1"     => hash=12345  => 12345 % 3 = 0 => Partition 0
  Key="user-1"     => hash=12345  => 12345 % 3 = 0 => Partition 0 (same!)
  Key="user-2"     => hash=12346  => 12346 % 3 = 1 => Partition 1
  Key="user-3"     => hash=12347  => 12347 % 3 = 2 => Partition 2
  Key="user-1-new" => hash=12348  => 12348 % 3 = 0 => Partition 0 (same user!)
```

### Important: Scale Partitions = Rebalance
```
Before: 3 partitions
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ P0: user-1   │  │ P1: user-2   │  │ P2: user-3   │
└──────────────┘  └──────────────┘  └──────────────┘

After: Increase to 4 partitions
New formula: hash(key) % 4 (CHANGED!)

  user-1: hash % 4 = 2 (moved from P0 to P2!)
  user-2: hash % 4 = 0 (moved from P1 to P0!)
  user-3: hash % 4 = 3 (same P2)

=> ALL KEYS REPARTITIONED => Data rebalance required!
```

---

## ❓ Self-Assessment Questions

### Q1: Ordering
**Q: How does Kafka guarantee ordering?**

A: Ordering is guaranteed **within a single partition only**. 
- All messages with the same key hash to the same partition
- Within a partition, messages are sequentially numbered by offset
- BUT: different partitions are processed independently (no global ordering)

**Q: What if I need ordering across all messages?**

A: Use 1 partition (no parallelism) OR:
- Accept out-of-order processing
- Use application-level sequencing (version/timestamp)
- Reorder in consumer before processing

### Q2: Partitioning
**Q: What determines message partition assignment?**

A: `partition = hash(key) % num_partitions`
- If key=null => round-robin (default partitioner)
- If key exists => hash-based to same partition
- Custom partitioner can override behavior

### Q3: Rebalancing
**Q: What happens if I add more partitions?**

A:
1. New hash formula: `hash(key) % new_count` (different than old)
2. All messages get repartitioned (move from old to new partitions)
3. Consumer rebalance happens to assign partitions
4. Temporary lag spike as data is moved

---

## 🎯 Next Steps

1. **Verify topics created:**
   ```bash
   # Check via Kafka CLI
   docker exec kafka kafka-topics --list --bootstrap-server kafka:9092
   ```

2. **View detailed partition info:**
   ```bash
   docker exec kafka kafka-topics --describe \
     --topic user-events \
     --bootstrap-server kafka:9092
   ```

3. **Continue to Task 1.2:**
   - Produce messages to `user-events` with different keys
   - Verify messages are partitioned correctly
   - Observe ordering within partitions

4. **Continue to Task 1.3:**
   - Consume messages with offset/partition logging
   - Verify ordering per key
   - Test rebalance behavior with multiple consumers

---

## 📝 File Summary

| File | Purpose | Key Concepts |
|------|---------|--------------|
| `KafkaTopicConfig.cs` | Topic definitions | Topic, Partition, Config, Retention |
| `KafkaTopicProvisioningService.cs` | Auto-create topics | IHostedService, Idempotency, Logging |
| `Program.cs` | Register service | Dependency Injection, Startup |
| `KafkaController.cs` | REST API | HTTP endpoints, Admin operations |
| `Task_1_1.http` | API test requests | Testing, Validation |

---

## ✅ Completion Checklist

- [x] Topic configuration file created
- [x] Provisioning service implemented
- [x] REST endpoints for topic management
- [x] API test requests prepared
- [x] Build successful
- [x] Documentation complete

**Status: READY FOR EXECUTION** 🚀

---

## 🐛 Debugging Tips

### Issue: Broker connection fails
```
Check:
1. docker ps | grep kafka (is it running?)
2. localhost:9092 is accessible
3. appsettings.json: "BootstrapServers": "localhost:9092"
4. From Docker network: "kafka:9092"
```

### Issue: Topics not created
```
Check:
1. Application logs for errors
2. Broker logs: docker logs kafka
3. Topic already exists: GET /api/kafka/topics
4. Try manual creation: POST /api/kafka/topics
```

### Issue: Metadata shows 0 partitions
```
Topic created but metadata not yet propagated
Wait 1-2 seconds then retry GET /api/kafka/topics/{name}/metadata
```

---

