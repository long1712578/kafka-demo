# 🚀 KAFKA DEMO - ADVANCED LEARNING PROJECT

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/YOUR_USERNAME/KafkaDemo?quickstart=1)

---

## ☁️ CHẠY TRÊN GITHUB CODESPACES (KHÔNG CẦN DOCKER LOCAL)

> **💡 Mới!** Bạn có thể chạy project này hoàn toàn trên cloud với GitHub Codespaces - không cần cài Docker trên máy!

### Quick Start với Codespaces

1. **Click badge** ở trên hoặc vào repo → Code → Codespaces → Create codespace
2. **Đợi ~3 phút** để môi trường setup xong
3. **Truy cập Kafka UI**: Tab Ports → Click port 8080
4. **Bắt đầu code!** Kafka + Kafka UI đã chạy sẵn

📖 **Hướng dẫn chi tiết**: [CODESPACES_GUIDE.md](./CODESPACES_GUIDE.md)

---

## 📋 GIỚI THIỆU

Project này được thiết kế để học Kafka từ cơ bản đến expert level với .NET. Bao gồm:

- **3-node Kafka Cluster** với Zookeeper ensemble
- **Schema Registry** cho Avro serialization
- **Kafka Connect** cho integration patterns
- **Monitoring Stack** (Prometheus + Grafana + Kafka UI)
- **Advanced .NET Examples** (Producers, Consumers, Admin)

## 🏗️ KIẾN TRÚC HỆ THỐNG

```
┌─────────────────────────────────────────────────────────────────┐
│                         KAFKA CLUSTER                           │
│  ┌──────────┐      ┌──────────┐      ┌──────────┐             │
│  │ Kafka 1  │      │ Kafka 2  │      │ Kafka 3  │             │
│  │ :19092   │◄────►│ :29092   │◄────►│ :39092   │             │
│  └──────────┘      └──────────┘      └──────────┘             │
│       │                 │                 │                      │
│       └─────────────────┴─────────────────┘                      │
│                         │                                        │
│                         ▼                                        │
│         ┌───────────────────────────────┐                       │
│         │   ZOOKEEPER ENSEMBLE          │                       │
│         │   ZK1:2181 ZK2:2182 ZK3:2183  │                       │
│         └───────────────────────────────┘                       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      SUPPORTING SERVICES                        │
│                                                                 │
│  Schema Registry:8081  │  Kafka Connect:8083                   │
│  Kafka UI:8080         │  AKHQ:8082                            │
│  Prometheus:9090       │  Grafana:3000                         │
└─────────────────────────────────────────────────────────────────┘
```

## 🎯 FEATURES

### Kafka Infrastructure
- ✅ 3-node Kafka cluster với high availability
- ✅ 3-node Zookeeper ensemble
- ✅ Replication Factor = 3, Min ISR = 2
- ✅ JMX monitoring enabled
- ✅ Compression (Snappy)

### Producers
- ✅ **AdvancedKafkaProducer**: Custom partitioner, batch processing
- ✅ **TransactionalKafkaProducer**: Exactly-once semantics
- ✅ Key-based partitioning & explicit partition routing

### Consumers
- ✅ **AdvancedKafkaConsumer**: Manual commit, rebalancing callbacks
- ✅ **ParallelKafkaConsumer**: Multi-threaded processing
- ✅ Consumer groups & offset management

### Admin Operations
- ✅ Topic CRUD, partition management
- ✅ Consumer group monitoring
- ✅ Cluster health checks

### Monitoring
- ✅ Kafka UI, AKHQ, Prometheus, Grafana

---

## 🚀 QUICK START

### 1. Start Kafka Cluster

```powershell
cd d:\Projects\KafkaDemo\kafka
docker-compose up -d
```

**Verify services:**
```powershell
docker-compose ps
```

### 2. Access Management UIs

| Service | URL | Credentials |
|---------|-----|-------------|
| Kafka UI | http://localhost:8080 | - |
| AKHQ | http://localhost:8082 | - |
| Grafana | http://localhost:3000 | admin/admin |
| Prometheus | http://localhost:9090 | - |

### 3. Build & Run .NET Projects

```powershell
cd d:\Projects\KafkaDemo
dotnet restore
dotnet build

# Run API
dotnet run --project KafkaDemo.API

# Run Consumer
dotnet run --project KafkaDemo.Consumer
```

---

## 📚 PROJECT STRUCTURE

```
KafkaDemo/
├── kafka/                          # Docker Compose setup
│   ├── docker-compose.yml          # Full Kafka cluster + tools
│   └── prometheus.yml              # Prometheus config
│
├── KafkaDemo.Infrastructure/       # Kafka implementations
│   ├── Producers/
│   │   ├── AdvancedKafkaProducer.cs
│   │   └── TransactionalKafkaProducer.cs
│   ├── Consumers/
│   │   ├── AdvancedKafkaConsumer.cs
│   │   └── ParallelKafkaConsumer.cs
│   └── Admin/
│       └── KafkaAdminService.cs
│
├── KAFKA_LEARNING_GUIDE.md         # Comprehensive learning guide
└── README.md                        # This file
```

---

## 🎓 LEARNING PATH

**Week 1: Basics** - Setup, topics, simple producer/consumer  
**Week 2: Intermediate** - Consumer groups, offset management  
**Week 3: Advanced** - Transactions, custom partitioners  
**Week 4: Expert** - Schema Registry, performance tuning  

📖 **Full Guide**: [KAFKA_LEARNING_GUIDE.md](./KAFKA_LEARNING_GUIDE.md)

---

## 🛠️ HANDS-ON EXERCISES

### Exercise 1: Create Topic

```bash
docker exec kafka-tools kafka-topics --create \
  --topic learning-topic \
  --partitions 3 \
  --replication-factor 3 \
  --bootstrap-server kafka1:9092
```

### Exercise 2: Test Producer

```csharp
var producer = new AdvancedKafkaProducer("localhost:19092", logger);
await producer.PublishAsync("learning-topic", new KafkaMessage
{
    Id = Guid.NewGuid(),
    Content = "Hello Kafka!",
    CreatedAt = DateTime.UtcNow,
    Type = "test"
});
```

### Exercise 3: Test Failover

```powershell
# Stop a broker
docker stop kafka1

# Messages still available! (replication)

# Restart
docker start kafka1
```

---

## 📊 MONITORING

- **Kafka UI**: http://localhost:8080 - Browse topics, messages, consumer groups
- **AKHQ**: http://localhost:8082 - Advanced management
- **Grafana**: http://localhost:3000 - Metrics dashboards
- **Prometheus**: http://localhost:9090 - Raw metrics

---

## 🔧 TROUBLESHOOTING

### Cannot connect to Kafka?
```powershell
docker-compose ps          # Check services
docker logs kafka1         # Check broker logs
```

### Consumer not receiving messages?
```csharp
AutoOffsetReset = AutoOffsetReset.Earliest  // Start from beginning
```

---

## 📖 RESOURCES

- 📘 [Learning Guide](./KAFKA_LEARNING_GUIDE.md) - Complete Kafka concepts
- 📚 [Official Kafka Docs](https://kafka.apache.org/documentation/)
- 🎓 [Confluent Platform](https://docs.confluent.io/)

---

## 🎯 WHAT YOU'LL LEARN

✅ Kafka cluster architecture  
✅ Producer/Consumer patterns  
✅ Partitioning & replication  
✅ Consumer groups & rebalancing  
✅ Transactions (exactly-once)  
✅ Performance tuning  
✅ Monitoring & operations  
✅ Production best practices  

---

## 👨‍💻 AUTHOR

Project created for learning Kafka with .NET  
**Happy Learning! 🚀**

---

## 🔗 QUICK LINKS

- [📖 Learning Guide](./KAFKA_LEARNING_GUIDE.md)
- [🎮 Kafka UI](http://localhost:8080)
- [📊 Grafana](http://localhost:3000)

**Start your Kafka journey today! 🎓**