# 🎨 Visual Architecture - Task 1.1

## Kafka Cluster Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     KAFKA CLUSTER                           │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │   Broker 1   │  │   Broker 2   │  │   Broker 3   │     │
│  │  (Leader)    │  │              │  │              │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
│                                                             │
│  ZooKeeper/KRaft Coordinating Leadership                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Topic: user-events (3 partitions)

```
Topic: user-events
│
├─ Partition 0 ────────────────────────────────────────┐
│  ┌─────────────────────────────────────────────────┐ │
│  │ Segment 0 (0-999)          Segment 1 (1000+)   │ │
│  ├─────────────────────────────────────────────────┤ │
│  │ Offset 0: {Key: user-1, Value: {"...}}         │ │
│  │ Offset 1: {Key: user-3, Value: {"...}}         │ │
│  │ Offset 2: {Key: user-1, Value: {"...}}         │ │
│  │ Offset 3: {Key: user-1, Value: {"...}}         │ │
│  │ Offset 4: {Key: user-2, Value: {"...}}         │ │
│  │ ... (append only, monotonically increasing)    │ │
│  └─────────────────────────────────────────────────┘ │
│                                                        │
│  Leader: Broker 1                                     │
│  Replicas: [1]                                        │
│  ISR: [1] (In Sync Replicas)                         │
│                                                        │
├─ Partition 1 ────────────────────────────────────────┐
│  ┌─────────────────────────────────────────────────┐ │
│  │ Offset 0: {Key: user-2, Value: {"...}}         │ │
│  │ Offset 1: {Key: user-1, Value: {"...}}         │ │
│  │ Offset 2: {Key: user-3, Value: {"...}}         │ │
│  │ Offset 3: {Key: user-2, Value: {"...}}         │ │
│  │ ...                                             │ │
│  └─────────────────────────────────────────────────┘ │
│                                                        │
│  Leader: Broker 1                                     │
│  Replicas: [1]                                        │
│  ISR: [1]                                             │
│                                                        │
└─ Partition 2 ────────────────────────────────────────┐
   ┌─────────────────────────────────────────────────┐ │
   │ Offset 0: {Key: user-3, Value: {"...}}         │ │
   │ Offset 1: {Key: user-1, Value: {"...}}         │ │
   │ Offset 2: {Key: user-2, Value: {"...}}         │ │
   │ ...                                             │ │
   └─────────────────────────────────────────────────┘ │
                                                        │
   Leader: Broker 1                                     │
   Replicas: [1]                                        │
   ISR: [1]                                             │
```

---

## Message Partitioning by Key

```
Producer sends messages:
│
├─ Message {Key: "user-1", Value: "..."}
│  ├─ hash("user-1") = 12345
│  ├─ 12345 % 3 = 0
│  └─ → Partition 0
│
├─ Message {Key: "user-2", Value: "..."}
│  ├─ hash("user-2") = 12346
│  ├─ 12346 % 3 = 1
│  └─ → Partition 1
│
├─ Message {Key: "user-3", Value: "..."}
│  ├─ hash("user-3") = 12347
│  ├─ 12347 % 3 = 2
│  └─ → Partition 2
│
└─ Message {Key: "user-1", Value: "..."} (again)
   ├─ hash("user-1") = 12345 (SAME)
   ├─ 12345 % 3 = 0 (SAME)
   └─ → Partition 0 (SAME - ordering guaranteed!)
```

---

## Parallel Processing of Partitions

```
Producer                     Kafka Topic                Consumers
┌──────┐                                         ┌──────────────┐
│      │  Messages with                          │  Consumer 1  │
│      │  different keys                         │ (Partition 0)│
│      ├──────────────────────┐                  └──────────────┘
│      │                      ▼
│      │  ┌─────────────────────────┐            ┌──────────────┐
│      │  │ Topic: user-events      │            │  Consumer 2  │
│      ├─►│                         │           │ (Partition 1)│
│      │  │ ┌─────┬─────┬─────┐    │            └──────────────┘
│      │  │ │  P0 │  P1 │  P2 │    │
│      │  │ │     │     │     │    │            ┌──────────────┐
│      │  │ └─────┴─────┴─────┘    │            │  Consumer 3  │
│      └─►│                         │           │ (Partition 2)│
│         │   (3 partitions)        │            └──────────────┘
│         │   (parallel logs)       │
│         │   (independent)         │            Processing:
│         └─────────────────────────┘            • In parallel
│                                               • Per partition
└──────┐                                         • Ordered within P
       │                                         • No ordering across P
       │
       Throughput:
       1 partition:  ~100k msg/s
       3 partitions: ~300k msg/s (3x faster!)
```

---

## Ordering Guarantee

```
Scenario A: Same Key (Ordering ✅)
┌─────────────────────────────────────────────────────┐
│ Partition 0 (Contains all user-1 messages)          │
├─────────────────────────────────────────────────────┤
│ Offset 0: User-1 login                             │
│ Offset 1: User-1 view product A                    │
│ Offset 2: User-1 add to cart                       │
│ Offset 3: User-1 checkout                          │
│ Offset 4: User-1 payment confirm                   │
│                                                     │
│ Processing: ALWAYS in order 0→1→2→3→4            │
│ Guarantee: Ordering within partition ✅             │
└─────────────────────────────────────────────────────┘

Scenario B: Different Keys (No Global Ordering ❌)
┌─────────────────────┬──────────────┬─────────────────┐
│ Partition 0         │ Partition 1  │ Partition 2     │
│ (user-1 messages)   │ (user-2)     │ (user-3)        │
├─────────────────────┼──────────────┼─────────────────┤
│ O0: user-1 login    │ O0: user-2   │ O0: user-3      │
│ O1: user-1 view     │ O1: user-2   │ O1: user-3      │
│ O2: user-1 add      │ O2: user-2   │ O2: user-3      │
│                     │              │                 │
│ Processing:         │ Processing:  │ Processing:     │
│ Sequential          │ Sequential   │ Sequential      │
│ BUT: P0, P1, P2     │              │                 │
│      processed in   │              │ May finish      │
│      parallel!      │              │ out of order!   │
│ RESULT: No global   │              │                 │
│         ordering    │              │                 │
└─────────────────────┴──────────────┴─────────────────┘

✅ Ordering within partition (same key)
❌ NO global ordering across partitions (different keys)
```

---

## Auto-Provisioning Flow

```
Application Start
│
├─ Program.cs: Register KafkaTopicProvisioningService
│
├─ Host.StartAsync()
│  │
│  └─ KafkaTopicProvisioningService.StartAsync()
│     │
│     ├─ Initialize KafkaAdminService
│     │
│     ├─ Check existing topics
│     │  ├─ "user-events"? ← YES, skip
│     │  ├─ "orders"? ← NO, create
│     │  ├─ "payments"? ← NO, create
│     │  ├─ "notifications"? ← NO, create
│     │  └─ "order-processing.DLQ"? ← NO, create
│     │
│     ├─ Create topics with configs
│     │  ├─ Retention: 604800000 ms (7 days)
│     │  ├─ Compression: snappy
│     │  └─ Partitions: 1, 3, 3, 5 (per topic)
│     │
│     └─ Log partition metadata
│        ├─ Topic: user-events, Partitions: 3
│        │  ├─ P0: Leader=1, Replicas=[1], ISR=[1]
│        │  ├─ P1: Leader=1, Replicas=[1], ISR=[1]
│        │  └─ P2: Leader=1, Replicas=[1], ISR=[1]
│        └─ ... (repeat for others)
│
└─ Application Running
   ├─ REST endpoints available
   ├─ Can produce to topics
   ├─ Can consume from topics
   └─ Ready for learning tasks
```

---

## REST API Endpoints

```
┌──────────────────────────────────────┐
│        KafkaController API           │
└──────────────────────────────────────┘

1️⃣  GET /api/kafka/topics
    │
    └─→ Response:
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

2️⃣  GET /api/kafka/topics/{topicName}/metadata
    │
    └─→ Example: /api/kafka/topics/user-events/metadata
        Response:
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
            {...P1...},
            {...P2...}
          ]
        }

3️⃣  POST /api/kafka/topics
    │
    └─→ Request:
        {
          "name": "my-topic",
          "numPartitions": 3,
          "replicationFactor": 1
        }
        Response: 201 Created

4️⃣  POST /api/kafka/init-module-a-topics
    │
    └─→ Response:
        {
          "message": "Module A topics initialization completed",
          "results": [
            {"topic": "user-events", "status": "created"...},
            {"topic": "orders", "status": "created"...},
            ...
          ]
        }
```

---

## 5 Demo Topics Configuration

```
┌─────────────────────────────────────────────────────────┐
│ user-events                                             │
├─────────────────────────────────────────────────────────┤
│ Partitions: 3                                           │
│ Key: userId (for ordering per user)                    │
│ Retention: 7 days (604800000 ms)                       │
│ Compression: snappy                                     │
│ Use case: User activity tracking                        │
│ Learning: Key-based partitioning                        │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ orders                                                  │
├─────────────────────────────────────────────────────────┤
│ Partitions: 3                                           │
│ Key: orderId (for ordering per order)                  │
│ Retention: 30 days (2592000000 ms)                     │
│ Compression: snappy                                     │
│ Use case: Order lifecycle events                        │
│ Learning: Exactly-once processing per order            │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ payments                                                │
├─────────────────────────────────────────────────────────┤
│ Partitions: 5 (higher throughput)                      │
│ Key: userId or transactionId                           │
│ Retention: 30 days                                      │
│ Compression: snappy                                     │
│ Use case: Payment processing (high volume)             │
│ Learning: Throughput scaling with partitions           │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ notifications                                           │
├─────────────────────────────────────────────────────────┤
│ Partitions: 1 (no ordering needed)                     │
│ Key: null (round-robin)                                │
│ Retention: 1 day (86400000 ms)                         │
│ Compression: snappy                                     │
│ Use case: Push notifications (fire-and-forget)        │
│ Learning: When NOT to use multiple partitions          │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ order-processing.DLQ (Dead Letter Queue)               │
├─────────────────────────────────────────────────────────┤
│ Partitions: 3                                           │
│ Key: orderId                                            │
│ Retention: 90 days (7776000000 ms) - longer!          │
│ Compression: snappy                                     │
│ Use case: Failed order messages for debugging          │
│ Learning: Error handling & observability               │
└─────────────────────────────────────────────────────────┘
```

---

## Partition Assignment Example

```
When Messages Arrive:

Messages to produce:
  1. {key: "order-100", value: "Order created"}
  2. {key: "order-101", value: "Order paid"}
  3. {key: "order-100", value: "Order shipped"}
  4. {key: "order-102", value: "Order created"}
  5. {key: "order-100", value: "Order delivered"}

Partitioning Logic:
  Msg 1: hash("order-100") % 3 = 0 → Partition 0 ✓
  Msg 2: hash("order-101") % 3 = 1 → Partition 1 ✓
  Msg 3: hash("order-100") % 3 = 0 → Partition 0 ✓ (same partition!)
  Msg 4: hash("order-102") % 3 = 2 → Partition 2 ✓
  Msg 5: hash("order-100") % 3 = 0 → Partition 0 ✓ (same partition again!)

Result:

Partition 0 (Order-100):          │ Partition 1 (Order-101):      │ Partition 2 (Order-102):
┌──────────────────────────────┐ │ ┌────────────────────────────┐ │ ┌────────────────────────┐
│ Offset 0: Order created       │ │ │ Offset 0: Order paid        │ │ │ Offset 0: Order created │
│ Offset 1: Order shipped   ✓✓  │ │ │                             │ │ │                        │
│ Offset 2: Order delivered ✓✓  │ │ │                             │ │ │                        │
│                              │ │ │                             │ │ │                        │
│ ORDERING: YES              │ │ │ ORDERING: N/A               │ │ │ ORDERING: N/A          │
│ (3 messages for order-100) │ │ │ (1 message)                 │ │ │ (1 message)            │
└──────────────────────────────┘ │ └────────────────────────────┘ │ └────────────────────────┘

All Partitions Processed In Parallel!
```

---

## Module A Task Progression

```
Task 1.1: Topic Provisioning ✅ COMPLETE
├─ Learn: Topic, Partition, Segment
├─ Learn: Key-based routing formula
├─ Learn: Offset and log model
└─ Implement: Auto-provisioning, REST API

Task 1.2: Producer with Keys ⏳ NEXT
├─ Learn: Producer API
├─ Learn: Partition distribution
├─ Learn: Key selection strategy
└─ Implement: Produce 30 messages, verify distribution

Task 1.3: Consumer with Logging ⏳ FUTURE
├─ Learn: Consumer API
├─ Learn: Offset tracking
├─ Learn: Ordering verification
└─ Implement: Log partition/offset/key info

Task 1.4: Rebalancing & Scaling ⏳ FUTURE
├─ Learn: Consumer group protocol
├─ Learn: Rebalance impact
├─ Learn: Lag measurement
└─ Implement: Scale consumers, observe rebalance

Task 1.5: Offset Semantics ⏳ FUTURE
├─ Learn: At-most-once vs At-least-once
├─ Learn: Exactly-once semantics
├─ Learn: Commit strategies
└─ Implement: Compare delivery guarantees
```

---

