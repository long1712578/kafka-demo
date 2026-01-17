# ⚡ KAFKA QUICK REFERENCE - CHEAT SHEET

## 🚀 START/STOP CLUSTER

```powershell
# Start cluster
cd d:\Projects\KafkaDemo\kafka
docker-compose up -d

# Stop cluster
docker-compose down

# Stop & remove data
docker-compose down -v

# Check status
docker-compose ps

# View logs
docker logs kafka1 -f
```

## 📊 TOPIC OPERATIONS

### Create Topic
```bash
docker exec kafka-tools kafka-topics --create \
  --topic my-topic \
  --partitions 3 \
  --replication-factor 3 \
  --bootstrap-server kafka1:9092
```

### List Topics
```bash
docker exec kafka-tools kafka-topics --list \
  --bootstrap-server kafka1:9092
```

### Describe Topic
```bash
docker exec kafka-tools kafka-topics --describe \
  --topic my-topic \
  --bootstrap-server kafka1:9092
```

### Delete Topic
```bash
docker exec kafka-tools kafka-topics --delete \
  --topic my-topic \
  --bootstrap-server kafka1:9092
```

### Alter Topic (Increase Partitions)
```bash
docker exec kafka-tools kafka-topics --alter \
  --topic my-topic \
  --partitions 6 \
  --bootstrap-server kafka1:9092
```

## 📨 PRODUCER OPERATIONS

### Console Producer (Simple)
```bash
docker exec -it kafka-tools kafka-console-producer \
  --topic my-topic \
  --bootstrap-server kafka1:9092
```

### Producer with Keys
```bash
docker exec -it kafka-tools kafka-console-producer \
  --topic my-topic \
  --property "parse.key=true" \
  --property "key.separator=:" \
  --bootstrap-server kafka1:9092

# Then type: key1:value1
```

### Performance Test
```bash
docker exec kafka-tools kafka-producer-perf-test \
  --topic my-topic \
  --num-records 10000 \
  --record-size 1024 \
  --throughput -1 \
  --producer-props bootstrap.servers=kafka1:9092 acks=all
```

## 📥 CONSUMER OPERATIONS

### Console Consumer (From Beginning)
```bash
docker exec -it kafka-tools kafka-console-consumer \
  --topic my-topic \
  --from-beginning \
  --bootstrap-server kafka1:9092
```

### Consumer with Key
```bash
docker exec -it kafka-tools kafka-console-consumer \
  --topic my-topic \
  --property print.key=true \
  --property key.separator=":" \
  --from-beginning \
  --bootstrap-server kafka1:9092
```

### Consumer Group
```bash
docker exec -it kafka-tools kafka-console-consumer \
  --topic my-topic \
  --group my-group \
  --bootstrap-server kafka1:9092
```

### Performance Test
```bash
docker exec kafka-tools kafka-consumer-perf-test \
  --topic my-topic \
  --messages 10000 \
  --bootstrap-server kafka1:9092
```

## 👥 CONSUMER GROUP OPERATIONS

### List Consumer Groups
```bash
docker exec kafka-tools kafka-consumer-groups --list \
  --bootstrap-server kafka1:9092
```

### Describe Consumer Group (Check Lag)
```bash
docker exec kafka-tools kafka-consumer-groups --describe \
  --group my-group \
  --bootstrap-server kafka1:9092
```

### Reset Offsets (To Beginning)
```bash
docker exec kafka-tools kafka-consumer-groups --reset-offsets \
  --group my-group \
  --topic my-topic \
  --to-earliest \
  --execute \
  --bootstrap-server kafka1:9092
```

### Reset Offsets (To Specific Offset)
```bash
docker exec kafka-tools kafka-consumer-groups --reset-offsets \
  --group my-group \
  --topic my-topic:0 \
  --to-offset 100 \
  --execute \
  --bootstrap-server kafka1:9092
```

### Delete Consumer Group
```bash
docker exec kafka-tools kafka-consumer-groups --delete \
  --group my-group \
  --bootstrap-server kafka1:9092
```

## 🔧 CLUSTER OPERATIONS

### List Brokers
```bash
docker exec kafka-tools kafka-broker-api-versions \
  --bootstrap-server kafka1:9092
```

### Cluster Metadata
```bash
docker exec kafka-tools kafka-metadata \
  --bootstrap-server kafka1:9092
```

### Check Cluster Health
```bash
docker exec zookeeper1 zkCli.sh ls /brokers/ids
```

## 📈 MONITORING

### Kafka UI
```
http://localhost:8080
```

### AKHQ
```
http://localhost:8082
```

### Grafana
```
http://localhost:3000
Username: admin
Password: admin
```

### Prometheus
```
http://localhost:9090
```

## 🔍 DEBUGGING

### Check Topic Offsets
```bash
docker exec kafka-tools kafka-run-class kafka.tools.GetOffsetShell \
  --broker-list kafka1:9092 \
  --topic my-topic
```

### Dump Log Segments
```bash
docker exec kafka1 kafka-dump-log \
  --files /var/lib/kafka/data/my-topic-0/00000000000000000000.log \
  --print-data-log
```

### Verify Consumer Group State
```bash
docker exec kafka-tools kafka-consumer-groups --describe \
  --group my-group \
  --members \
  --bootstrap-server kafka1:9092
```

## 🛡️ IMPORTANT CONFIGURATIONS

### Producer Best Practices
```csharp
var config = new ProducerConfig
{
    BootstrapServers = "localhost:19092",
    Acks = Acks.All,              // Strongest durability
    EnableIdempotence = true,     // Exactly-once
    MaxInFlight = 5,
    LingerMs = 10,                // Batching
    CompressionType = Snappy,     // Compression
};
```

### Consumer Best Practices
```csharp
var config = new ConsumerConfig
{
    BootstrapServers = "localhost:19092",
    GroupId = "my-group",
    EnableAutoCommit = false,           // Manual commit
    AutoOffsetReset = Earliest,
    IsolationLevel = ReadCommitted,     // Read only committed
    SessionTimeoutMs = 45000,
    MaxPollIntervalMs = 300000,
};
```

## ⚠️ COMMON ISSUES

### Issue: Cannot connect to Kafka
```powershell
# Check if services are running
docker-compose ps

# Check logs
docker logs kafka1 --tail 50

# Restart services
docker-compose restart
```

### Issue: Consumer not receiving messages
```bash
# Check topic exists
docker exec kafka-tools kafka-topics --list --bootstrap-server kafka1:9092

# Check consumer group
docker exec kafka-tools kafka-consumer-groups --describe \
  --group my-group \
  --bootstrap-server kafka1:9092

# Reset offsets to beginning
docker exec kafka-tools kafka-consumer-groups --reset-offsets \
  --group my-group \
  --topic my-topic \
  --to-earliest \
  --execute \
  --bootstrap-server kafka1:9092
```

### Issue: High consumer lag
```bash
# Monitor lag
docker exec kafka-tools kafka-consumer-groups --describe \
  --group my-group \
  --bootstrap-server kafka1:9092

# Solutions:
# 1. Add more consumers (up to number of partitions)
# 2. Increase processing speed
# 3. Increase MaxPollIntervalMs
# 4. Use parallel processing
```

## 🎯 PERFORMANCE TUNING

### High Throughput
```csharp
// Producer
LingerMs = 100,              // Wait 100ms to batch
BatchSize = 1048576,         // 1MB batches
CompressionType = Lz4,       // Fast compression

// Consumer
FetchMinBytes = 1048576,     // 1MB min fetch
MaxPartitionFetchBytes = 10485760,  // 10MB per partition
```

### Low Latency
```csharp
// Producer
LingerMs = 0,                // Send immediately
BatchSize = 1,
CompressionType = None,

// Consumer
FetchMinBytes = 1,
FetchMaxWaitMs = 0,
```

## 📚 USEFUL LINKS

- Kafka UI: http://localhost:8080
- AKHQ: http://localhost:8082
- Grafana: http://localhost:3000
- Prometheus: http://localhost:9090
- Schema Registry: http://localhost:8081
- Kafka Connect: http://localhost:8083

## 🎓 LEARNING RESOURCES

- Full Guide: KAFKA_LEARNING_GUIDE.md
- README: README.md
- Official Docs: https://kafka.apache.org/documentation/

## ⌨️ KEYBOARD SHORTCUTS (Kafka UI)

- `Ctrl + K`: Search topics
- `Ctrl + /`: Show shortcuts
- `Esc`: Close dialogs

---

**Quick Setup:**
```powershell
# 1. Start cluster
.\start-kafka.ps1

# 2. Run exercises
.\exercises.ps1

# 3. Build .NET projects
dotnet build

# 4. Run examples
dotnet run --project KafkaDemo.API
```

**Happy Kafka-ing! 🚀**
