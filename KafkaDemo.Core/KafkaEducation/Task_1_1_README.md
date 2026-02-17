# 🎯 Task 1.1: Tạo Topic Demo với Multiple Partitions - COMPLETE ✅

## 📖 Quick Summary

Task 1.1 đã được hoàn thành với **5 topics demo** được tự động tạo khi API application khởi động:

```
✅ user-events           (3 partitions)  - Key-based partitioning demo
✅ orders                (3 partitions)  - Ordering guarantee demo  
✅ payments              (5 partitions)  - Throughput scaling demo
✅ notifications         (1 partition)   - No-key publishing demo
✅ order-processing.DLQ  (3 partitions)  - Error handling / DLQ demo
```

---

## 🚀 Quick Start

### 1. Start Application
```bash
cd KafkaDemo.API
dotnet run

# Expected log:
# 🚀 [Task 1.1] Starting Kafka Topic Provisioning...
# ✅ Topic 'user-events' created successfully
# ✅ Topic 'orders' created successfully
# ... etc ...
# ✅ [Task 1.1] Topic Provisioning completed successfully!
```

### 2. Test Endpoints

**Option A: Swagger UI**
```
http://localhost:5224/swagger
=> KafkaController => Test endpoints
```

**Option B: REST Client**
```
Open: KafkaDemo.API/Task_1_1.http
Click "Send Request" on each test
```

**Option C: cURL**
```bash
# List all topics
curl http://localhost:5224/api/kafka/topics

# Get topic metadata
curl http://localhost:5224/api/kafka/topics/user-events/metadata

# Initialize topics manually (if needed)
curl -X POST http://localhost:5224/api/kafka/init-module-a-topics
```

---

## 📂 Files Created/Modified

### New Files
```
✅ KafkaDemo.Core/Models/KafkaTopicConfig.cs
   └─ Topic configuration definitions

✅ KafkaDemo.Infrastructure/KafkaTopicProvisioningService.cs
   └─ Auto-create topics on startup (IHostedService)

✅ KafkaDemo.API/Task_1_1.http
   └─ REST API test requests

✅ KafkaDemo.Core/KafkaEducation/Task_1_1_TopicProvisioning.md
   └─ Implementation documentation

✅ KafkaDemo.Core/KafkaEducation/ModuleA_Task_1_1_Detailed.md
   └─ Complete learning guide

✅ KafkaDemo.Core/KafkaEducation/verify-task-1-1.sh
   └─ Verification script
```

### Modified Files
```
✅ KafkaDemo.API/Program.cs
   └─ Registered KafkaTopicProvisioningService

✅ KafkaDemo.API/Controllers/KafkaController.cs
   └─ Added 4 new endpoints for topic management

✅ KafkaDemo.Infrastructure/Admin/KafkaAdminService.cs
   └─ Changed logger from ILogger<T> to ILogger
```

---

## 📚 REST API Endpoints

### 1. List Topics
```http
GET /api/kafka/topics
```
```json
{ "topics": ["user-events", "orders", ...], "count": 5 }
```

### 2. Get Topic Metadata
```http
GET /api/kafka/topics/{topicName}/metadata
```
Returns partition details, leaders, replicas

### 3. Create Custom Topic
```http
POST /api/kafka/topics
Content-Type: application/json

{ "name": "my-topic", "numPartitions": 3, "replicationFactor": 1 }
```

### 4. Initialize Module A Topics
```http
POST /api/kafka/init-module-a-topics
```
Automatically creates all 5 demo topics

---

## 🎓 Key Learning Points

### What is a Partition?
- Independent append-only log
- Unit of parallelism
- Ordering guaranteed **within partition only**
- Messages with same key go to same partition

### Why Multiple Partitions?
```
1 Partition:  Sequential, ~100k msg/s (bottleneck)
3 Partitions: Parallel, ~300k msg/s (3x throughput!)
```

### How Does Partitioning Work?
```
partition_id = hash(message_key) % num_partitions

Example:
  Key="user-1" => partition 0
  Key="user-1" => partition 0 (same user, same partition)
  Key="user-2" => partition 1 (different key, different partition)
```

### What is a Segment?
- Physical file on disk (1GB or 7 days default)
- Retention policy controls deletion
- Transparent to producers/consumers

---

## ✅ Verification

### Verify Topics Created
```bash
# Option 1: API endpoint
curl http://localhost:5224/api/kafka/topics

# Option 2: Docker/Kafka CLI
docker exec kafka kafka-topics \
  --list --bootstrap-server kafka:9092
```

### Verify Partitions
```bash
# Get detailed partition info
curl http://localhost:5224/api/kafka/topics/user-events/metadata

# Expected: 3 partitions (0, 1, 2) each with leader and replicas
```

### Run Verification Script
```bash
bash KafkaDemo.Core/KafkaEducation/verify-task-1-1.sh
```

---

## 🎯 Topic Configuration Reference

| Topic | Partitions | Use Case | Retention |
|-------|-----------|----------|-----------|
| `user-events` | 3 | User activity, partitioned by userId | 7 days |
| `orders` | 3 | Order events, partitioned by orderId | 30 days |
| `payments` | 5 | Payment processing, high throughput | 30 days |
| `notifications` | 1 | Notifications, no ordering required | 1 day |
| `order-processing.DLQ` | 3 | Dead-letter queue for failed orders | 90 days |

---

## 🔄 Next Steps

### Task 1.2: Produce Messages with Keys
- Produce to `user-events` with different userId keys
- Verify messages are partitioned correctly
- **Expected Learning**: Key-based routing, partition assignment

### Task 1.3: Consume and Log Partition Info
- Consume from `user-events` topic
- Log: offset, partition, key, value
- Verify messages with same key are in same partition
- **Expected Learning**: Offset semantics, ordering per key

### Task 1.4: Test Rebalancing
- Start 2 consumers with same group
- Watch rebalance happen (lag spike)
- Add/remove consumers dynamically
- **Expected Learning**: Consumer group protocol, rebalance impact

### Task 1.5: Produce High Throughput
- Produce 10k messages rapidly
- Monitor partition distribution
- Verify no single partition bottleneck (with 3 partitions)
- **Expected Learning**: Throughput vs latency tradeoffs

---

## 💡 Interview Questions You Can Now Answer

✅ **Q: How does Kafka ensure ordering?**
- A: Ordering is guaranteed within a single partition
- Messages with same key hash to same partition via: `partition = hash(key) % partitions`
- Different partitions process in parallel (no global ordering)

✅ **Q: Why would you increase partitions?**
- A: For higher throughput and parallelism
- But: Causes key remapping and rebalancing

✅ **Q: What is a Segment?**
- A: Physical file on disk (rotated by size/time)
- Retention policy deletes old segments

✅ **Q: How does a consumer know which partition to read?**
- A: Consumer group coordinator assigns partitions
- One consumer per partition (or group rebalance)

---

## 🐛 Troubleshooting

### Topics Not Created
```
1. Check logs: dotnet run output should show creation messages
2. Verify Kafka is running: docker ps
3. Check connectivity: curl localhost:9092 (should timeout gracefully)
4. Manually trigger: POST http://localhost:5224/api/kafka/init-module-a-topics
```

### Cannot Get Metadata
```
1. Topic might still be propagating (wait 1-2 seconds)
2. Topic name might be wrong (check case-sensitivity)
3. Broker unreachable (check Kafka status)
```

### Partition Count = 0
```
New topics need metadata refresh
Wait a moment then retry GET /api/kafka/topics/{name}/metadata
```

---

## 📖 Additional Resources

### Files to Read
- `ModuleA_Task_1_1_Detailed.md` - Complete learning guide
- `Task_1_1_TopicProvisioning.md` - Implementation details
- `KafkaTopicConfig.cs` - Topic configuration code

### Kafka CLI Commands
```bash
# List topics
kafka-topics --list --bootstrap-server localhost:9092

# Describe topic (detailed)
kafka-topics --describe --topic user-events --bootstrap-server localhost:9092

# Create topic manually
kafka-topics --create --topic test \
  --partitions 3 --replication-factor 1 \
  --bootstrap-server localhost:9092

# Delete topic
kafka-topics --delete --topic test --bootstrap-server localhost:9092
```

---

## ✨ Summary

**Task 1.1 Status: ✅ COMPLETE**

What you now understand:
- [x] Topic/Partition/Segment structure
- [x] Key-based partitioning strategy
- [x] Ordering guarantees per partition
- [x] How to create and configure topics
- [x] How to query partition metadata
- [x] Auto-provisioning pattern with IHostedService

**Ready for: Module A Task 1.2 - Producing Messages with Keys** 🚀

---

**Learning Time: ~30-45 minutes**  
**Difficulty: Beginner - Intermediate**  
**Key Concepts: 3 (Partition, Key, Offset)**

