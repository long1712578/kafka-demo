# KafkaDemo.Consumer

KafkaDemo.Consumer is a simple .NET 8 console application demonstrating how to consume messages from an Apache Kafka topic using the pub/sub (publish/subscribe) pattern.

## Features

- Built with C# 12 and .NET 8
- Demonstrates Kafka consumer implementation
- Easy to extend for real-world pub/sub scenarios

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Apache Kafka](https://kafka.apache.org/) instance (local or remote)
- (Optional) [Confluent.Kafka](https://www.nuget.org/packages/Confluent.Kafka/) NuGet package for Kafka integration

## Getting Started

1. **Clone the repository:**
```bash
git clone https://github.com/long1712578/kafka-demo.git
cd KafkaDemo.Consumer
```
2. **Install dependencies:**
If you use Confluent.Kafka, run:
```bash
dotnet add package Confluent.Kafka
```

3. **Configure Kafka settings:**
Update your consumer configuration in `Program.cs` or a configuration file with your Kafka broker and topic details.

4. **Run the application:**
```bash
dotnet run --project KafkaDemo.Consumer
```
## Example Usage

The current implementation prints a simple message to the console. Extend `Program.cs` to connect to Kafka and consume messages:
```bash
using Confluent.Kafka;
var config = new ConsumerConfig { BootstrapServers = "localhost:9092", GroupId = "demo-consumer-group", AutoOffsetReset = AutoOffsetReset.Earliest };
using var consumer = new ConsumerBuilder<Ignore, string>(config).Build(); consumer.Subscribe("your-topic");
while (true) { var consumeResult = consumer.Consume(); Console.WriteLine($"Received message: {consumeResult.Message.Value}"); }
```

# Kafka + Kafka UI with Docker Compose (KRaft Mode)

This project provides a **Kafka cluster running in KRaft mode (no Zookeeper)**, along with **Kafka UI** for easy topic browsing, producing, and consuming messages.

---

## 🛠️ Prerequisites

- Docker
- Docker Compose
- (Optional) `uuidgen` if you want to generate your own Cluster ID

---

## 🚀 How to Start

1. **Start the services:**

   ```bash
   docker-compose up -d

# Kafka + Kafka-UI + Docker Compose (Bitnami Image, KRaft mode)

## 📝 Mục tiêu

- Triển khai **Kafka** chạy bằng Docker Compose
- Không cần Zookeeper (dùng KRaft mode)
- Sử dụng **Kafka-UI** để quản lý topics
- Cho phép:
  - Client (producer/consumer) bên ngoài kết nối qua `localhost:9092`
  - Các container khác (như Kafka UI) kết nối qua network nội bộ `kafka:29092`

---

## 🏗️ Kiến trúc

```plaintext
  ┌─────────────┐
  │  kafka-ui   │  --> kết nối nội bộ --> kafka:29092
  └─────────────┘
         │
         │
  ┌─────────────┐
  │   Kafka     │
  └─────────────┘
         │
         │
  <==> client app bên ngoài connect qua localhost:9092


## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please open issues or submit pull requests for improvements.

---

For more information about Kafka, visit the [official documentation](https://kafka.apache.org/documentation/).