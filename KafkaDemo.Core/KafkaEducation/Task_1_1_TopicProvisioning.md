# Task 1.1: Tạo Topic Demo với Multiple Partitions

## 📋 Mục tiêu
Implement tự động tạo topics để học về Kafka partitioning strategy và ordering guarantees.

## ✅ Những gì đã được implement

### 1. **KafkaTopicConfig.cs** - Topic Configuration Definition
- `KafkaTopicConfig` class: Mô tả một topic (name, partitions, replication factor, configs)
- `ModuleATopics` class: Định nghĩa 5 topics demo:

```
user-events        (3 partitions) - Learn partitioning by userId key
orders             (3 partitions) - Learn ordering per orderId  
payments           (5 partitions) - Learn higher throughput
notifications      (1 partition)  - Learn no-key publishing
order-processing.DLQ (3 partitions) - Learn DLQ pattern
```

### 2. **KafkaTopicProvisioningService.cs** - Auto-Create Topics on Startup
- Implements `IHostedService` => tự động chạy khi app startup
- Kiểm tra topics đã tồn tại => bỏ qua (idempotent)
- Tạo topics không tồn tại với config từ `ModuleATopics`
- Log chi tiết partition metadata

### 3. **Program.cs** - Register Service
```csharp
builder.Services.AddHostedService(sp => 
    new KafkaTopicProvisioningService(
        kafkaBootstrapServers,
        sp.GetRequiredService<ILogger<KafkaTopicProvisioningService>>()));
```

### 4. **KafkaController.cs** - REST Endpoints cho Task 1.1

#### Endpoint 1: List all topics
```http
GET /api/kafka/topics
```

**Response:**
```json
{
  "topics": ["user-events", "orders", "payments", "notifications", "order-processing.DLQ"],
  "count": 5
}
```

#### Endpoint 2: Get topic metadata (partitions, leaders, replicas)
```http
GET /api/kafka/topics/{topicName}/metadata
```

**Example:** `GET /api/kafka/topics/user-events/metadata`

**Response:**
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

#### Endpoint 3: Create custom topic
```http
POST /api/kafka/topics
Content-Type: application/json

{
  "name": "my-custom-topic",
  "numPartitions": 3,
  "replicationFactor": 1,
  "configs": {
    "retention.ms": "604800000",
    "compression.type": "snappy"
  }
}
```

#### Endpoint 4: Initialize all Module A topics
```http
POST /api/kafka/init-module-a-topics
```

**Response:**
```json
{
  "message": "Module A topics initialization completed",
  "results": [
    {
      "topic": "user-events",
      "status": "created",
      "partitions": 3,
      "replicationFactor": 1
    },
    {
      "topic": "orders",
      "status": "created",
      "partitions": 3,
      "replicationFactor": 1
    },
    ...
  ]
}
```

## 🚀 Cách sử dụng

### Option 1: Automatic on Startup (Recommended)
1. Start the API application
2. Check logs for topic creation messages
3. Endpoints tự động available

### Option 2: Manual via REST API
1. Mở Swagger UI: `http://localhost:5224/swagger`
2. Navigate to `KafkaController`
3. Test endpoints:
   - `POST /api/kafka/init-module-a-topics` (tạo tất cả)
   - `GET /api/kafka/topics` (list)
   - `GET /api/kafka/topics/{topicName}/metadata` (xem chi tiết)

### Option 3: Using cURL
```bash
# List topics
curl http://localhost:5224/api/kafka/topics

# Get topic metadata
curl http://localhost:5224/api/kafka/topics/user-events/metadata

# Create custom topic
curl -X POST http://localhost:5224/api/kafka/topics \
  -H "Content-Type: application/json" \
  -d '{
    "name": "test-topic",
    "numPartitions": 3,
    "replicationFactor": 1
  }'

# Initialize Module A topics
curl -X POST http://localhost:5224/api/kafka/init-module-a-topics
```

## 📖 Kiến thức học được

### 1. Topic Structure
```
Topic: user-events (3 partitions)
│
├── Partition 0 (Leader: Broker 1)
│   └── Messages: [offset 0, 1, 2, ...]
├── Partition 1 (Leader: Broker 1)
│   └── Messages: [offset 0, 1, 2, ...]
└── Partition 2 (Leader: Broker 1)
    └── Messages: [offset 0, 1, 2, ...]
```

### 2. Partitioning Strategy
```
Message với Key
├─ Key="user-1" => hash(user-1) % 3 = Partition 0
├─ Key="user-2" => hash(user-2) % 3 = Partition 1
└─ Key="user-3" => hash(user-3) % 3 = Partition 0 (có thể cùng partition)

=> Messages từ cùng user đi vào cùng partition
=> Đảm bảo ordering per user (trong 1 partition)
```

### 3. Ordering Guarantees
```
Partition 0: [User1_Event1, User1_Event2, User1_Event3]
             => Xử lý tuần tự, ordering guaranteed

Partition 1: [User2_Event1, User2_Event2]
Partition 2: [User3_Event1]

=> Partitions khác nhau có thể xử lý song song
=> Nhưng User1 events luôn ordered
```

## ❓ Self-Check Questions

1. **Partition là gì?**
   - A) Một segment file
   - B) Đơn vị song parallel, append-only log ✓
   - C) Một consumer
   - D) Một broker

2. **Key dùng để làm gì?**
   - A) Encrypt message
   - B) Xác định partition (hash(key) % partition_count) ✓
   - C) Identify consumer
   - D) Validate message

3. **Ordering được đảm bảo ở level nào?**
   - A) Topic level
   - B) Consumer level
   - C) Partition level ✓
   - D) Message level

4. **3 partitions topic có mấy consumer để đạt parallelism tối đa?**
   - A) 1
   - B) 2
   - C) 3 ✓
   - D) 5

## 🔧 Troubleshooting

### Topic creation fails with "Topic already exists"
```
Solution: Delete topic first or check topic list
curl http://localhost:5224/api/kafka/topics
```

### Broker not reachable
```
Kiểm tra Docker:
docker ps | grep kafka
docker logs <kafka-container-id>

Kiểm tra appsettings.json:
"Kafka:BootstrapServers": "localhost:9092"  
// hoặc "kafka:9092" nếu từ Docker
```

### Partitions = 0 hoặc không thấy partitions
```
Topic có thể chưa fully propagate
Đợi vài giây rồi retry
curl http://localhost:5224/api/kafka/topics/user-events/metadata
```

## 📊 Output Log Example

```
🚀 [Task 1.1] Starting Kafka Topic Provisioning...

📋 Existing topics: 

📝 Creating topic: user-events
   └─ Partitions: 3
   └─ Replication Factor: 1
   └─ Configs: retention.ms=604800000, compression.type=snappy
✅ Topic 'user-events' created successfully

📝 Creating topic: orders
   └─ Partitions: 3
   └─ Replication Factor: 1
   └─ Configs: retention.ms=2592000000, compression.type=snappy
✅ Topic 'orders' created successfully

... (more topics) ...

✅ [Task 1.1] Topic Provisioning completed successfully!
```

## 🎯 Next Step: Module A - Task 1.2
Implement consumer demo để:
1. Consume từ `user-events` topic
2. Log offset, partition, key information
3. Verify ordering per key

---

**Task 1.1 Status: ✅ COMPLETE**
- [x] Topic config definition
- [x] Auto-provisioning service
- [x] REST endpoints
- [x] Logging & monitoring
