# 🎓 KAFKA LEARNING PATH - TỪ CƠ BẢN ĐẾN EXPERT

## 📚 MỤC LỤC

1. [Kiến trúc Kafka](#1-kiến-trúc-kafka)
2. [Core Concepts](#2-core-concepts)
3. [Producer Deep Dive](#3-producer-deep-dive)
4. [Consumer Deep Dive](#4-consumer-deep-dive)
5. [Partitioning & Replication](#5-partitioning--replication)
6. [Performance Tuning](#6-performance-tuning)
7. [Monitoring & Operations](#7-monitoring--operations)
8. [Best Practices](#8-best-practices)
9. [Hands-on Exercises](#9-hands-on-exercises)

---

## 1. KIẾN TRÚC KAFKA

### 1.1. Kafka Cluster Components

```
┌─────────────────────────────────────────────────────────────┐
│                      KAFKA CLUSTER                          │
│                                                             │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐            │
│  │ Broker 1 │    │ Broker 2 │    │ Broker 3 │            │
│  │  Leader  │    │ Follower │    │ Follower │            │
│  │  Port:   │    │  Port:   │    │  Port:   │            │
│  │  19092   │    │  29092   │    │  39092   │            │
│  └──────────┘    └──────────┘    └──────────┘            │
│       │               │               │                     │
│       └───────────────┴───────────────┘                     │
│                       │                                     │
│           ┌───────────▼───────────┐                        │
│           │   ZOOKEEPER ENSEMBLE  │                        │
│           │  (Coordination Layer) │                        │
│           │   Zk1   Zk2   Zk3    │                        │
│           │  2181  2182  2183     │                        │
│           └───────────────────────┘                        │
└─────────────────────────────────────────────────────────────┘
```

**Components:**
- **Broker**: Kafka server lưu trữ và serve data
- **Zookeeper**: Quản lý cluster metadata, leader election
- **Topic**: Category/feed name để publish messages
- **Partition**: Ordered, immutable sequence of records
- **Producer**: Application gửi messages vào Kafka
- **Consumer**: Application đọc messages từ Kafka

### 1.2. Data Flow

```
Producer → [Topic] → Partition 0 → Consumer Group 1 (Consumer A)
                   → Partition 1 → Consumer Group 1 (Consumer B)
                   → Partition 2 → Consumer Group 2 (Consumer C)
```

---

## 2. CORE CONCEPTS

### 2.1. Topics & Partitions

**Topic**: Logical channel để organize messages
**Partition**: Physical division của topic để parallel processing

```
Topic: "orders"
├── Partition 0: [msg1, msg4, msg7, ...]
├── Partition 1: [msg2, msg5, msg8, ...]
└── Partition 2: [msg3, msg6, msg9, ...]
```

**Key Points:**
- Mỗi partition là một ordered log
- Messages trong partition có offset (unique ID)
- Ordering guarantee chỉ có trong 1 partition
- Số partitions = Max parallelism level

### 2.2. Replication

**Replication Factor (RF)**: Số copies của mỗi partition

```
Topic: "orders" (RF=3)
├── Partition 0
│   ├── Leader:   Broker 1
│   ├── Replica:  Broker 2
│   └── Replica:  Broker 3
```

**Key Points:**
- Leader: Handle all reads/writes
- Followers: Replicate leader's data
- ISR (In-Sync Replicas): Followers that are caught up
- Min ISR: Minimum replicas cần có để accept writes

### 2.3. Consumer Groups

```
Consumer Group "service-a":
├── Consumer 1 → Partition 0, 1
├── Consumer 2 → Partition 2, 3
└── Consumer 3 → (idle)

Consumer Group "service-b":
├── Consumer 1 → Partition 0, 1, 2, 3
```

**Key Points:**
- Mỗi partition chỉ được consume bởi 1 consumer trong group
- Multiple groups có thể consume cùng 1 topic
- Rebalancing xảy ra khi consumer join/leave

---

## 3. PRODUCER DEEP DIVE

### 3.1. Producer Configuration

```csharp
var config = new ProducerConfig
{
    // CONNECTION
    BootstrapServers = "localhost:19092,localhost:29092,localhost:39092",
    
    // DURABILITY (⭐ QUAN TRỌNG)
    Acks = Acks.All,              // -1: Đợi all ISR replicas
                                   //  1: Chỉ đợi leader
                                   //  0: Không đợi (fastest, unsafe)
    
    EnableIdempotence = true,      // Exactly-once semantics
    
    // BATCHING & PERFORMANCE
    LingerMs = 10,                 // Đợi 10ms để batch messages
    BatchSize = 16384,             // 16KB batch size
    CompressionType = Snappy,      // Compression algorithm
    
    // RETRY
    MessageTimeoutMs = 30000,      // Tổng timeout
    RetryBackoffMs = 100,          // Delay giữa retries
};
```

### 3.2. Partitioning Strategies

#### A. Key-based Partitioning (Default)
```csharp
// Cùng key → cùng partition → ordering guarantee
await producer.ProduceAsync("orders", new Message<string, string>
{
    Key = "customer-123",  // Hash(key) % numPartitions
    Value = orderJson
});
```

#### B. Custom Partitioner
```csharp
.SetPartitioner((topic, partitionCount, keyData, keyIsNull) =>
{
    if (keyIsNull)
        return Random.Next(0, partitionCount);
    
    // VIP customers → Partition 0
    if (IsVipCustomer(key))
        return 0;
    
    return Hash(key) % partitionCount;
})
```

#### C. Explicit Partition
```csharp
// Gửi thẳng vào partition cụ thể
await producer.ProduceAsync(
    new TopicPartition("orders", 2),
    new Message<string, string> { Value = data });
```

### 3.3. Delivery Guarantees

| Acks | Min ISR | Idempotence | Guarantee | Use Case |
|------|---------|-------------|-----------|----------|
| 0 | - | No | At-most-once | Metrics, logs |
| 1 | - | No | At-least-once | General apps |
| All | 2+ | Yes | Exactly-once | Financial, critical |

### 3.4. Transactions (Exactly-Once)

```csharp
var producer = new ProducerBuilder<string, string>(config).Build();
producer.InitTransactions();

producer.BeginTransaction();
try
{
    await producer.ProduceAsync("topic-a", msg1);
    await producer.ProduceAsync("topic-b", msg2);
    await producer.ProduceAsync("topic-c", msg3);
    
    producer.CommitTransaction();  // All or nothing
}
catch
{
    producer.AbortTransaction();
}
```

---

## 4. CONSUMER DEEP DIVE

### 4.1. Consumer Configuration

```csharp
var config = new ConsumerConfig
{
    BootstrapServers = "localhost:19092",
    GroupId = "my-consumer-group",
    
    // OFFSET MANAGEMENT
    EnableAutoCommit = false,           // Manual commit cho control tốt hơn
    AutoOffsetReset = Earliest,         // Earliest | Latest
    
    // REBALANCING
    SessionTimeoutMs = 45000,           // 45s không heartbeat → rebalance
    HeartbeatIntervalMs = 3000,         // Heartbeat mỗi 3s
    MaxPollIntervalMs = 300000,         // 5 phút không poll → rebalance
    
    // FETCH OPTIMIZATION
    FetchMinBytes = 1024,               // Tối thiểu 1KB
    FetchWaitMaxMs = 500,               // Max wait 500ms
    
    // TRANSACTION SUPPORT
    IsolationLevel = ReadCommitted,     // Chỉ đọc committed messages
};
```

### 4.2. Offset Management

**Offset**: Position của consumer trong partition

```
Partition 0: [0][1][2][3][4][5][6][7][8][9]
                            ↑
                     Current Offset = 5
```

#### Auto Commit (Easy, Risky)
```csharp
EnableAutoCommit = true,
AutoCommitIntervalMs = 5000  // Commit mỗi 5s
```
⚠️ Risk: At-least-once (có thể duplicate nếu crash)

#### Manual Commit (Recommended)
```csharp
EnableAutoCommit = false;

while (true)
{
    var result = consumer.Consume();
    ProcessMessage(result);
    
    consumer.Commit(result);  // Commit sau khi process thành công
}
```

#### Async Commit
```csharp
consumer.Commit();  // Async, không đợi
```

### 4.3. Rebalancing

**Triggers:**
- Consumer join/leave group
- Consumer timeout (không heartbeat)
- Partition count changed

**Types:**
1. **Eager Rebalancing**: Stop-the-world, tất cả consumers stop
2. **Cooperative Rebalancing** (Recommended): Chỉ revoke affected partitions

```csharp
PartitionAssignmentStrategy = CooperativeSticky
```

**Callbacks:**
```csharp
.SetPartitionsAssignedHandler((c, partitions) =>
{
    // Partitions được assign
    Console.WriteLine($"Assigned: {partitions}");
})
.SetPartitionsRevokedHandler((c, partitions) =>
{
    // Commit trước khi revoke
    c.Commit(partitions);
})
```

---

## 5. PARTITIONING & REPLICATION

### 5.1. Partition Distribution

**Balanced Distribution:**
```
Topic: "orders" (6 partitions, RF=3)

Broker 1: P0(L), P1(F), P2(F), P3(L), P4(F), P5(F)
Broker 2: P0(F), P1(L), P2(F), P3(F), P4(L), P5(F)
Broker 3: P0(F), P1(F), P2(L), P3(F), P4(F), P5(L)

L = Leader, F = Follower
```

### 5.2. ISR (In-Sync Replicas)

```
Partition 0:
├── Leader: Broker 1 (Offset: 1000)
├── ISR 1:  Broker 2 (Offset: 1000) ✅
└── ISR 2:  Broker 3 (Offset: 995)  ⚠️ Lagging
```

**Min ISR = 2**:
- Accept write nếu có >= 2 ISR
- Reject write nếu < 2 ISR (availability trade-off)

### 5.3. Leader Election

**Scenario**: Leader crashes
```
Before:
P0: Leader=B1, ISR=[B1, B2, B3]

B1 crashes:
P0: Leader=B2, ISR=[B2, B3]  (Zookeeper elects new leader)
```

---

## 6. PERFORMANCE TUNING

### 6.1. Producer Tuning

```csharp
// THROUGHPUT OPTIMIZATION
LingerMs = 100,              // Batch for 100ms
BatchSize = 1048576,         // 1MB batches
CompressionType = Lz4,       // Fast compression
BufferMemory = 67108864,     // 64MB buffer

// LATENCY OPTIMIZATION
LingerMs = 0,                // Send immediately
BatchSize = 1,               // No batching
CompressionType = None,
```

### 6.2. Consumer Tuning

```csharp
// HIGH THROUGHPUT
FetchMinBytes = 1048576,     // 1MB min fetch
FetchMaxBytes = 52428800,    // 50MB max fetch
MaxPartitionFetchBytes = 10485760,  // 10MB per partition

// PARALLEL PROCESSING
// Tạo nhiều consumers = số partitions
Consumers = NumPartitions
```

### 6.3. Partition Strategy

**Công thức:**
```
Throughput = Producer Rate / Number of Partitions
Max Parallelism = Number of Partitions
```

**Recommendations:**
- Start: 3-6 partitions per topic
- Scale: Add partitions khi throughput tăng
- Max: Partitions = Expected max consumers

---

## 7. MONITORING & OPERATIONS

### 7.1. Key Metrics

**Producer:**
- `record-send-rate`: Messages/sec
- `request-latency-avg`: Latency trung bình
- `record-error-rate`: Failed messages

**Consumer:**
- `records-consumed-rate`: Messages/sec consumed
- `records-lag`: Consumer lag (messages behind)
- `commit-latency-avg`: Commit latency

**Broker:**
- `UnderReplicatedPartitions`: Partitions thiếu ISR
- `OfflinePartitionsCount`: Partitions offline
- `ActiveControllerCount`: Controller availability

### 7.2. Tools

**Kafka UI**: http://localhost:8080
- Topic management
- Message browser
- Consumer group monitoring

**AKHQ**: http://localhost:8082
- Advanced management
- Schema registry
- Kafka Connect

**Grafana**: http://localhost:3000
- Metrics dashboards
- Alerting

### 7.3. CLI Commands

```bash
# List topics
docker exec kafka-tools kafka-topics --list --bootstrap-server kafka1:9092

# Create topic
docker exec kafka-tools kafka-topics --create \
  --topic test-topic \
  --partitions 3 \
  --replication-factor 3 \
  --bootstrap-server kafka1:9092

# Describe topic
docker exec kafka-tools kafka-topics --describe \
  --topic test-topic \
  --bootstrap-server kafka1:9092

# Consumer groups
docker exec kafka-tools kafka-consumer-groups --list \
  --bootstrap-server kafka1:9092

# Consumer lag
docker exec kafka-tools kafka-consumer-groups --describe \
  --group my-group \
  --bootstrap-server kafka1:9092
```

---

## 8. BEST PRACTICES

### 8.1. Producer

✅ **DO:**
- Enable idempotence cho critical data
- Use key-based partitioning cho ordering
- Batch messages với LingerMs
- Handle ProduceException properly
- Use compression (snappy/lz4)

❌ **DON'T:**
- Send synchronously (await mỗi message)
- Use Acks=0 cho critical data
- Ignore delivery reports
- Create producer per message

### 8.2. Consumer

✅ **DO:**
- Manual commit sau khi process xong
- Handle rebalancing callbacks
- Implement retry logic
- Use consumer groups
- Monitor consumer lag

❌ **DON'T:**
- Auto commit cho critical processing
- Process too slow (rebalancing risk)
- Store state in consumer (stateless)
- Consume từ nhiều threads (not thread-safe)

### 8.3. Topics

✅ **DO:**
- Plan partitions trước (hard to change)
- Use replication factor >= 3
- Set retention based on use case
- Monitor disk usage
- Use topic naming convention

❌ **DON'T:**
- Too many topics (overhead)
- Too many partitions (Zookeeper overhead)
- RF = 1 in production
- Unlimited retention

---

## 9. HANDS-ON EXERCISES

### Exercise 1: Start Kafka Cluster

```powershell
cd d:\Projects\KafkaDemo\kafka
docker-compose up -d

# Check health
docker-compose ps

# Access Kafka UI
http://localhost:8080
```

### Exercise 2: Create Topic

```powershell
# Via CLI
docker exec kafka-tools kafka-topics --create \
  --topic learning-topic \
  --partitions 3 \
  --replication-factor 3 \
  --bootstrap-server kafka1:9092

# Or use Admin Service in .NET
```

### Exercise 3: Test Producer

```csharp
var producer = new AdvancedKafkaProducer(
    "localhost:19092", logger);

// Test 1: Simple publish
await producer.PublishAsync("learning-topic", new KafkaMessage
{
    Id = Guid.NewGuid(),
    Content = "Hello Kafka",
    CreatedAt = DateTime.UtcNow,
    Type = "test"
});

// Test 2: Key-based partitioning
await producer.PublishWithKeyAsync("learning-topic", "customer-123", message);

// Test 3: Batch publish
await producer.PublishBatchAsync("learning-topic", messages);
```

### Exercise 4: Test Consumer

```csharp
var consumer = new AdvancedKafkaConsumer(
    "localhost:19092",
    "test-group",
    "learning-topic",
    logger);

await consumer.StartAsync(CancellationToken.None);
```

### Exercise 5: Simulate Failures

```powershell
# Stop broker 1
docker stop kafka1

# Check in Kafka UI:
# - Leader election
# - ISR changes
# - Messages still available

# Restart broker
docker start kafka1
```

### Exercise 6: Monitor Consumer Lag

```bash
docker exec kafka-tools kafka-consumer-groups --describe \
  --group test-group \
  --bootstrap-server kafka1:9092
```

---

## 📖 RECOMMENDED LEARNING PATH

**Week 1: Basics**
- Setup cluster
- Create topics
- Simple producer/consumer
- Understand partitions

**Week 2: Intermediate**
- Consumer groups
- Offset management
- Rebalancing
- Monitoring

**Week 3: Advanced**
- Transactions
- Custom partitioners
- Performance tuning
- Failure scenarios

**Week 4: Expert**
- Schema Registry
- Kafka Connect
- Streams API
- Production deployment

---

## 🎯 CERTIFICATION GOALS

1. ✅ Hiểu kiến trúc Kafka cluster
2. ✅ Master producer configurations
3. ✅ Master consumer patterns
4. ✅ Understand replication & ISR
5. ✅ Performance tuning
6. ✅ Monitoring & troubleshooting
7. ✅ Production best practices

---

## 📚 RESOURCES

- **Official Docs**: https://kafka.apache.org/documentation/
- **Confluent Platform**: https://docs.confluent.io/
- **This Project**: Complete hands-on examples in .NET

**Happy Learning! 🚀**
