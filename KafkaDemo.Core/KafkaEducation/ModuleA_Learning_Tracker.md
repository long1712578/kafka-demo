# 🎓 Senior Kafka Developer - Module A Learning Tracker

## 📚 Module A: Core Architecture & Understanding

### Overview
Nắm vững các khái niệm cốt lõi về cách Kafka thật sự chạy (runtime behavior), không chỉ định nghĩa.

---

## 📋 Task Checklist

### ✅ Task 1.1: Topic Provisioning (COMPLETED)

**Status**: ✅ COMPLETE

**What You Built:**
- [x] Topic configuration definition (KafkaTopicConfig.cs)
- [x] Auto-provisioning service (KafkaTopicProvisioningService.cs)
- [x] REST API endpoints for topic management (KafkaController)
- [x] 5 demo topics with different partition counts
- [x] Comprehensive documentation

**Key Learnings:**
- [x] Topic = logical channel, Partition = physical shard
- [x] Partition = append-only log with offset
- [x] Segment = file on disk
- [x] Ordering guaranteed **within partition only**
- [x] Partitioning formula: `partition = hash(key) % num_partitions`

**Files to Review:**
1. `KafkaDemo.Core/Models/KafkaTopicConfig.cs` - Topic configs
2. `KafkaDemo.Infrastructure/KafkaTopicProvisioningService.cs` - Auto-creation logic
3. `KafkaDemo.Core/KafkaEducation/ModuleA_Task_1_1_Detailed.md` - Deep dive
4. `KafkaDemo.API/Task_1_1.http` - API tests

**How to Verify:**
```bash
# Option 1: Check logs on app startup
# Option 2: curl http://localhost:5224/api/kafka/topics
# Option 3: bash KafkaDemo.Core/KafkaEducation/verify-task-1-1.sh
```

---

### ⏳ Task 1.2: Producer with Key-Based Routing (TODO)

**Status**: ⏳ NOT STARTED

**Objectives:**
- [ ] Produce messages to `user-events` topic with userId keys
- [ ] Produce messages to `orders` topic with orderId keys
- [ ] Verify messages are routed to correct partitions
- [ ] Observe key-based partitioning in action

**What You Will Build:**
- Producer utility with key-based publishing
- Demo data generator (10+ messages per key)
- Visual verification of partition distribution

**Key Concepts to Learn:**
- [ ] How key affects partition assignment
- [ ] Hash function for key distribution
- [ ] Load distribution across partitions
- [ ] Partition skew (uneven distribution)

**Estimated Time**: 1-2 hours

---

### ⏳ Task 1.3: Consumer with Partition Logging (TODO)

**Status**: ⏳ NOT STARTED

**Objectives:**
- [ ] Consume from `user-events` topic
- [ ] Log: topic, partition, offset, key, value
- [ ] Verify ordering per key
- [ ] Observe different keys in different partitions

**What You Will Build:**
- Consumer service with detailed logging
- Log formatter showing partition/offset/key info
- Verification that same-key messages are in same partition

**Key Concepts to Learn:**
- [ ] Offset = position in partition log
- [ ] Consumer offset tracking
- [ ] Ordering guarantee per key
- [ ] How to debug partition assignment

**Estimated Time**: 1-2 hours

---

### ⏳ Task 1.4: Rebalance & Scalability (TODO)

**Status**: ⏳ NOT STARTED

**Objectives:**
- [ ] Start with 1 consumer (handles all 3 partitions)
- [ ] Add 2nd consumer (rebalance happens)
- [ ] Add 3rd consumer (rebalance happens again)
- [ ] Remove consumer (rebalance)
- [ ] Measure lag spike during rebalance

**What You Will Build:**
- Multiple consumer instances
- Monitoring/logging for rebalance events
- Lag measurement script

**Key Concepts to Learn:**
- [ ] Consumer group rebalancing
- [ ] Stop-the-world nature of rebalance
- [ ] Session timeout & heartbeat
- [ ] Lag spike impact
- [ ] Cooperative vs eager rebalancing

**Estimated Time**: 2-3 hours

---

### ⏳ Task 1.5: Offset Semantics (TODO)

**Status**: ⏳ NOT STARTED

**Objectives:**
- [ ] Implement: Auto-commit (at-most-once)
- [ ] Implement: Manual commit (at-least-once)
- [ ] Implement: Exactly-once with transactional processing
- [ ] Demonstrate duplicate message scenarios
- [ ] Demonstrate message loss scenarios

**What You Will Build:**
- 3 consumer implementations with different commit strategies
- Failure injection (consumer crash, slow processing)
- Verification of delivery semantics

**Key Concepts to Learn:**
- [ ] At-most-once = fast but may lose
- [ ] At-least-once = safe but may duplicate
- [ ] Exactly-once = complex but accurate
- [ ] Commit offset semantics
- [ ] AutoOffsetReset behavior

**Estimated Time**: 2-3 hours

---

## 🎯 Learning Path

```
Task 1.1: Topic/Partition/Segment (COMPLETED ✅)
    ↓ (understand structure)
Task 1.2: Produce with Keys
    ↓ (observe partitioning)
Task 1.3: Consume & Log
    ↓ (verify ordering)
Task 1.4: Rebalance & Scale
    ↓ (understand group dynamics)
Task 1.5: Offset Semantics
    ↓ (master delivery guarantees)
Module A Complete! Proceed to Module B (Producer Patterns)
```

---

## 📊 Progress Dashboard

| Task | Status | Completed | Learnings | Code Files |
|------|--------|-----------|-----------|-----------|
| 1.1  | ✅     | 100%      | 5/5       | 3 created |
| 1.2  | ⏳     | 0%        | 0/5       | 0 created |
| 1.3  | ⏳     | 0%        | 0/5       | 0 created |
| 1.4  | ⏳     | 0%        | 0/5       | 0 created |
| 1.5  | ⏳     | 0%        | 0/5       | 0 created |

**Module A Progress: 20% Complete** (1/5 tasks done)

---

## 💡 Interview Q&A Progress

### Module A Questions (After Task 1.1)

✅ **Q: Explain Kafka's architecture**
- A: Cluster of brokers, each broker has topics
- Topics divided into partitions (parallelism)
- Partitions are append-only logs with offsets
- Leaders coordinate replication
- [Can explain after Task 1.1]

✅ **Q: How does partitioning work?**
- A: Message's key is hashed: `partition = hash(key) % num_partitions`
- Same key always goes to same partition
- Ensures ordering per key
- [Can explain after Task 1.1]

✅ **Q: What is an offset?**
- A: Position in partition's log (0, 1, 2, ...)
- Consumer tracks offset to know what's been consumed
- [Can explain after Task 1.1]

⏳ **Q: What is a consumer group?** (Task 1.4)
- A: [Will learn in Task 1.4]

⏳ **Q: How does rebalancing work?** (Task 1.4)
- A: [Will learn in Task 1.4]

⏳ **Q: What are delivery semantics?** (Task 1.5)
- A: [Will learn in Task 1.5]

---

## 🎓 Key Concepts Mastery

### After Task 1.1 ✅
- [x] Topic & Partition structure
- [x] Append-only log model
- [x] Offset semantics (basic)
- [x] Segment files
- [x] Key-based routing formula
- [x] Ordering guarantee per partition

### After Task 1.2 ⏳
- [ ] Producer API
- [ ] Key selection strategy
- [ ] Partition distribution
- [ ] Batch vs single publish

### After Task 1.3 ⏳
- [ ] Consumer API
- [ ] Offset tracking
- [ ] Message ordering verification
- [ ] Log analysis

### After Task 1.4 ⏳
- [ ] Consumer group basics
- [ ] Rebalance mechanism
- [ ] Partition assignment strategy
- [ ] Lag monitoring

### After Task 1.5 ⏳
- [ ] Offset commit strategies
- [ ] Delivery semantic tradeoffs
- [ ] Duplicate handling
- [ ] Message loss prevention

---

## 📖 Reading Materials

### Essential Reading (Task 1.1)
1. `ModuleA_Task_1_1_Detailed.md` - Complete guide
2. `Task_1_1_README.md` - Quick reference
3. Kafka official docs: Topics and Logs section

### Recommended Reading Before Task 1.2
- Confluent Kafka C# client documentation
- Producer configuration options
- Partitioner interface

---

## 🔧 Environment Setup

### Prerequisites (All Done ✅)
- [x] Docker with Kafka running
- [x] .NET 8 project setup
- [x] Confluent.Kafka NuGet package
- [x] Admin client configured
- [x] Test endpoints working

### Tools for Learning
- [x] Swagger UI: `http://localhost:5224/swagger`
- [x] REST client: `Task_1_1.http`
- [x] Docker CLI: `docker ps`, `docker logs`
- [x] Kafka CLI: `kafka-topics`, `kafka-console-producer`, `kafka-console-consumer`

---

## 🎯 Quick Navigation

### Task 1.1 Complete Documentation
- **Quick Start**: Read `Task_1_1_README.md` (5 min)
- **Deep Dive**: Read `ModuleA_Task_1_1_Detailed.md` (20 min)
- **Code Review**: Check `KafkaTopicConfig.cs` (10 min)
- **Implementation**: Review `KafkaTopicProvisioningService.cs` (15 min)
- **API Testing**: Run `Task_1_1.http` endpoints (10 min)

### Next Task (Task 1.2)
- [ ] Create producer service with key support
- [ ] Write test to produce 30 messages (10 per key)
- [ ] Verify partition distribution via API
- [ ] Document findings

---

## 📝 Notes & Reflections

### Task 1.1 Completion Notes
- Date Started: [Your date]
- Date Completed: [Your date]
- Time Spent: [Hours]
- Difficulty: ⭐⭐ (Easy-Moderate)
- Aha Moments:
  1. Partitions enable parallelism
  2. Key determines partition via hash
  3. Ordering only within partition
  4. Segment files are physical storage

### Challenges Encountered
- Logger type mismatch (fixed by using generic ILogger)
- Topic metadata not immediately available (solved with brief delay)

### Questions Remaining
- How to implement custom partitioner?
- What's optimal partition count?
- How to handle partition rebalancing during deployment?

---

## ✨ Summary

**Task 1.1: Topic Provisioning - COMPLETE** ✅

What you accomplished:
- Implemented auto-topic creation on app startup
- Created 5 demo topics with different configurations
- Built REST API for topic management
- Verified partition structure and metadata
- Documented learning thoroughly

What you learned:
- Kafka architecture (topic/partition/segment)
- Partitioning strategy and formula
- Offset semantics
- Ordering guarantees

What's next:
- Task 1.2: Produce messages with keys
- Task 1.3: Consume and verify ordering
- Task 1.4: Experience rebalancing
- Task 1.5: Master delivery semantics

**Status: Ready to proceed to Task 1.2** 🚀

---

