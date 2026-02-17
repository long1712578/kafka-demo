# 🚀 Task 1.1: Step-by-Step Execution Guide

## Before You Start
- [ ] Docker with Kafka is running
- [ ] .NET 8 is installed
- [ ] Project compiles successfully (run `dotnet build`)
- [ ] You have 10-15 minutes

---

## Step 1: Verify Kafka is Running ✅ (2 minutes)

### Option A: Using Docker
```bash
# Check if Kafka container is running
docker ps | grep kafka

# Expected output:
# CONTAINER ID  IMAGE  COMMAND  ...  PORTS  NAMES
# abc123        kafka  ...      ...  9092   kafka
```

### Option B: Using Docker Compose
```bash
# Start Kafka if not running
docker-compose up -d

# Verify
docker-compose ps
```

### Option C: Check Connectivity
```bash
# Try to list topics (ignore any error, just checking connection)
docker exec kafka kafka-topics --list --bootstrap-server kafka:9092
```

**✅ If you see Kafka running or no connection error, move to Step 2**

---

## Step 2: Start the API Application ✅ (2 minutes)

### In Terminal/PowerShell:
```bash
cd KafkaDemo.API
dotnet run

# OR
dotnet run --project KafkaDemo.API
```

### Expected Output:
```
info: KafkaDemo.Infrastructure.KafkaTopicProvisioningService[0]
      🚀 [Task 1.1] Starting Kafka Topic Provisioning...
      
info: KafkaDemo.Infrastructure.Admin.KafkaAdminService[0]
      ✅ Kafka Admin Client initialized

info: KafkaDemo.Infrastructure.KafkaTopicProvisioningService[0]
      📋 Existing topics: 

info: KafkaDemo.Infrastructure.KafkaTopicProvisioningService[0]
      📝 Creating topic: user-events

info: KafkaDemo.Infrastructure.KafkaTopicProvisioningService[0]
      ✅ Topic 'user-events' created successfully

[... repeats for other topics ...]

info: KafkaDemo.Infrastructure.KafkaTopicProvisioningService[0]
      ✅ [Task 1.1] Topic Provisioning completed successfully!

info: Microsoft.Hosting.Lifetime[14]
      Application started. Press Ctrl+C to quit
```

**✅ If you see this output, topics were created successfully!**

---

## Step 3: Verify Topics via REST API ✅ (3 minutes)

### Option A: Using Swagger UI (Easiest)
```
1. Open browser: http://localhost:5224/swagger
2. Find "KafkaController" section
3. Click on endpoint: "GET /api/kafka/topics"
4. Click "Try it out" button
5. Click "Execute"
6. Look for response with 5 topics:
   - user-events
   - orders
   - payments
   - notifications
   - order-processing.DLQ
```

### Option B: Using REST Client Extension (VS Code)
```
1. Open file: KafkaDemo.API/Task_1_1.http
2. Find request: "1️⃣  List all existing topics"
3. Click "Send Request" button
4. Check response in the right panel
```

### Option C: Using cURL
```bash
curl http://localhost:5224/api/kafka/topics

# Expected response (formatted):
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

**✅ If you see 5 topics, move to Step 4**

---

## Step 4: View Topic Metadata ✅ (3 minutes)

### Option A: Swagger UI
```
1. Open: http://localhost:5224/swagger
2. Find endpoint: "GET /api/kafka/topics/{topicName}/metadata"
3. Click "Try it out"
4. In "topicName" field, enter: user-events
5. Click "Execute"
6. Look for response with partitions:
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
       ... (2 more partitions)
     ]
   }
```

### Option B: cURL
```bash
curl http://localhost:5224/api/kafka/topics/user-events/metadata
```

### What to Observe:
- `user-events` has 3 partitions (IDs: 0, 1, 2)
- Each partition has a leader
- Each partition has replicas (for reliability)
- Each partition is in-sync

**✅ If you see 3 partitions for user-events, move to Step 5**

---

## Step 5: Check Other Topics ✅ (2 minutes)

### Repeat Step 4 for other topics:

```bash
# Check orders (should have 3 partitions)
curl http://localhost:5224/api/kafka/topics/orders/metadata

# Check payments (should have 5 partitions)
curl http://localhost:5224/api/kafka/topics/payments/metadata

# Check notifications (should have 1 partition)
curl http://localhost:5224/api/kafka/topics/notifications/metadata

# Check DLQ (should have 3 partitions)
curl http://localhost:5224/api/kafka/topics/order-processing.DLQ/metadata
```

### Expected Results:
| Topic | Partitions |
|-------|-----------|
| user-events | 3 |
| orders | 3 |
| payments | 5 |
| notifications | 1 |
| order-processing.DLQ | 3 |

**✅ If partition counts match, Task 1.1 is COMPLETE!**

---

## Step 6: (Optional) Run Verification Script

```bash
# Make script executable
chmod +x KafkaDemo.Core/KafkaEducation/verify-task-1-1.sh

# Run verification
bash KafkaDemo.Core/KafkaEducation/verify-task-1-1.sh

# Expected output:
# ✓ API is running
# ✓ Found topics: 5
# ✓ user-events: 3 partitions
# ✓ orders: 3 partitions
# ... etc
# ✅ Task 1.1 Verification Complete
```

---

## 📊 Expected Final State

### Topics Created ✅
```
✓ user-events        (3 partitions)
✓ orders             (3 partitions)
✓ payments           (5 partitions)
✓ notifications      (1 partition)
✓ order-processing.DLQ (3 partitions)
```

### API Endpoints Working ✅
```
✓ GET /api/kafka/topics (list)
✓ GET /api/kafka/topics/{name}/metadata (details)
✓ POST /api/kafka/topics (create custom)
✓ POST /api/kafka/init-module-a-topics (initialize)
```

### Documentation Available ✅
```
✓ TASK_1_1_COMPLETION_SUMMARY.md (overview)
✓ Task_1_1_README.md (quick reference)
✓ ModuleA_Task_1_1_Detailed.md (deep dive)
✓ ModuleA_Learning_Tracker.md (progress)
✓ KafkaDemo.API/Task_1_1.http (tests)
```

---

## 🎓 Understanding What You Just Did

### What Happened:
1. **API Started** → Registered KafkaTopicProvisioningService
2. **Service Started** → Connected to Kafka Admin client
3. **Topics Checked** → Listed existing topics (empty)
4. **Topics Created** → Created 5 demo topics with configs
5. **Metadata Logged** → Logged partition details
6. **API Ready** → REST endpoints available for topic ops

### Why This Matters:
- **Automatic Provisioning**: Infrastructure as Code pattern
- **Idempotent**: Safe to restart app (won't recreate)
- **Observable**: Can query topics via REST API
- **Scalable**: Easy to add more topics to ModuleATopics

### Key Learning:
```
Topic with 3 partitions = 3 independent logs

user-events (3 partitions):
├─ Partition 0: [messages with keys hashing to 0]
├─ Partition 1: [messages with keys hashing to 1]
└─ Partition 2: [messages with keys hashing to 2]

Same key always hashes to same partition
=> Guarantees ordering for that key
=> Enables parallelism across keys
```

---

## ✅ Task 1.1 Complete!

You have successfully:
1. [x] Created Kafka topics via REST API
2. [x] Understood topic/partition structure
3. [x] Verified auto-provisioning works
4. [x] Queried partition metadata
5. [x] Prepared environment for next tasks

---

## 🎯 Next: Task 1.2 - Producing Messages

Ready to move on? Here's what's next:

**Task 1.2: Produce Messages with Keys**
- Produce 30 messages (10 per key) to `user-events`
- Verify partition distribution
- Observe key-based routing in action

**Files to create:**
- Producer demo service
- Message generator (simulating user events)
- Partition verification script

**Learning outcomes:**
- How keys determine partition assignment
- Load distribution across partitions
- Producer API usage

**Estimated time**: 1-2 hours

---

## 💡 Tips

### If Something Goes Wrong:

**Q: Topics not created**
- A: Check API logs for errors. Verify Kafka is running.

**Q: Cannot access API**
- A: Verify port 5224 is correct in appsettings.

**Q: Metadata shows 0 partitions**
- A: Wait 2 seconds, topics sometimes take time to fully propagate.

**Q: Want to clean up and restart**
- A: Delete Docker volume: `docker volume rm kafkavol` then restart

---

## 📚 Additional Resources

### Documentation Files
1. `TASK_1_1_COMPLETION_SUMMARY.md` - Full summary
2. `Task_1_1_README.md` - Quick reference
3. `ModuleA_Task_1_1_Detailed.md` - Complete guide
4. `ModuleA_Learning_Tracker.md` - Progress tracking

### Code Files
1. `KafkaDemo.Core/Models/KafkaTopicConfig.cs` - Topic configs
2. `KafkaDemo.Infrastructure/KafkaTopicProvisioningService.cs` - Auto-create logic
3. `KafkaDemo.API/Controllers/KafkaController.cs` - REST endpoints

### Test Files
1. `KafkaDemo.API/Task_1_1.http` - API tests
2. `KafkaDemo.Core/KafkaEducation/verify-task-1-1.sh` - Verification script

---

## 🎉 Congratulations!

**You've completed Module A Task 1.1!**

You now understand:
- How Kafka topics are divided into partitions
- How keys determine partition assignment via hashing
- How partitions enable both parallelism and ordering
- How to automatically provision topics in your application

**Ready for Task 1.2!** 🚀

