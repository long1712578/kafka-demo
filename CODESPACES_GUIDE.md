# 🚀 Hướng Dẫn Sử Dụng GitHub Codespaces cho KafkaDemo

## 📖 Mục Lục
- [Giới thiệu](#giới-thiệu)
- [Cách khởi động Codespace](#cách-khởi-động-codespace)
- [Cấu trúc môi trường](#cấu-trúc-môi-trường)
- [Làm việc với Kafka](#làm-việc-với-kafka)
- [Tips & Tricks](#tips--tricks)
- [So sánh với Docker Local](#so-sánh-với-docker-local)
- [Troubleshooting](#troubleshooting)

---

## 🎯 Giới thiệu

**GitHub Codespaces** là môi trường development hoàn chỉnh chạy trên cloud của GitHub. Với setup này, bạn có thể:

| Tính năng | Mô tả |
|-----------|-------|
| ✅ Không cần Docker Desktop | Codespace có Docker tích hợp sẵn |
| ✅ Code từ mọi nơi | Chỉ cần browser hoặc VS Code |
| ✅ Môi trường nhất quán | Mọi người dùng chung 1 config |
| ✅ Tự động setup | Kafka + Kafka UI chạy sẵn |
| ✅ Free tier | 60 giờ/tháng miễn phí |

---

## 🚀 Cách Khởi Động Codespace

### Bước 1: Mở GitHub Repository

Truy cập repository của bạn trên GitHub:
```
https://github.com/YOUR_USERNAME/KafkaDemo
```

### Bước 2: Tạo Codespace

**Cách 1: Từ nút Code**
1. Click nút **`<> Code`** màu xanh
2. Chọn tab **Codespaces**
3. Click **Create codespace on main**

**Cách 2: Phím tắt**
- Nhấn phím **`.`** để mở web editor
- Hoặc nhấn **`,`** để tạo Codespace trực tiếp

### Bước 3: Đợi Setup

Codespace sẽ tự động:
1. 🐳 Khởi động Docker containers (Kafka + Kafka UI)
2. 📦 Restore NuGet packages
3. 🔧 Build solution
4. 🔌 Forward các ports cần thiết

> ⏱️ Lần đầu setup mất khoảng 3-5 phút

---

## 📁 Cấu Trúc Môi Trường

```
KafkaDemo/
├── .devcontainer/
│   ├── devcontainer.json           # Cấu hình chính
│   ├── docker-compose.devcontainer.yml  # Docker Compose overlay
│   └── scripts/
│       ├── post-create.sh          # Chạy sau khi tạo container
│       └── post-start.sh           # Chạy mỗi lần start
├── docker-compose.yml              # Docker Compose gốc
└── ... (code của bạn)
```

### Services chạy trong Codespace:

| Service | Container | Port | Mô tả |
|---------|-----------|------|-------|
| Kafka Broker | `kafka` | 9092, 29092 | Apache Kafka (KRaft mode) |
| Kafka UI | `kafka-ui` | 8080 | Web UI quản lý Kafka |
| Dev Container | `devcontainer` | - | Môi trường .NET |

---

## 🔧 Làm Việc với Kafka

### Truy cập Kafka UI

1. Click tab **PORTS** ở bottom panel
2. Tìm port **8080** (Kafka UI)
3. Click biểu tượng 🌐 để mở trong browser

### Kết nối từ code

```csharp
// appsettings.json hoặc appsettings.Development.json
{
  "Kafka": {
    "BootstrapServers": "kafka:29092",  // Trong Codespace
    // "BootstrapServers": "localhost:9092",  // Local Docker
  }
}
```

### Kafka CLI Commands

```bash
# Liệt kê topics
docker exec kafka kafka-topics.sh --bootstrap-server localhost:29092 --list

# Tạo topic mới
docker exec kafka kafka-topics.sh --bootstrap-server localhost:29092 \
    --create --topic my-topic --partitions 3 --replication-factor 1

# Xem chi tiết topic
docker exec kafka kafka-topics.sh --bootstrap-server localhost:29092 \
    --describe --topic my-topic

# Produce message
docker exec -it kafka kafka-console-producer.sh \
    --bootstrap-server localhost:29092 --topic my-topic

# Consume messages
docker exec -it kafka kafka-console-consumer.sh \
    --bootstrap-server localhost:29092 --topic my-topic --from-beginning
```

### Chạy ứng dụng .NET

```bash
# Build solution
dotnet build

# Chạy API
dotnet run --project KafkaDemo.API

# Chạy Consumer
dotnet run --project KafkaDemo.Consumer

# Chạy cả hai (mở 2 terminal)
# Terminal 1: dotnet run --project KafkaDemo.API
# Terminal 2: dotnet run --project KafkaDemo.Consumer
```

---

## 💡 Tips & Tricks

### 1. Mở Codespace bằng VS Code Desktop

```bash
# Cài GitHub Codespaces extension trong VS Code
# Sau đó: Ctrl+Shift+P → "Codespaces: Connect to Codespace"
```

### 2. Prebuilds để khởi động nhanh hơn

Thêm file `.github/codespaces/prebuild-configuration.json`:
```json
{
  "onPushBranches": ["main", "develop"]
}
```

### 3. Dotfiles cá nhân

Tạo repo `your-username/dotfiles` để tự động apply config cá nhân.

### 4. Giữ Codespace sống

```bash
# Trong terminal
while true; do sleep 60; done
```

### 5. Kiểm tra resource usage

```bash
# Xem CPU/Memory
docker stats

# Xem disk
df -h
```

---

## ⚖️ So Sánh với Docker Local

| Tiêu chí | Docker Local | GitHub Codespaces |
|----------|-------------|-------------------|
| **Cài đặt** | Cần cài Docker Desktop (~500MB) | Không cần cài gì |
| **Tài nguyên máy** | Dùng RAM/CPU của máy bạn | Dùng cloud của GitHub |
| **Performance** | Nhanh (chạy local) | Tùy network (thường nhanh) |
| **Chi phí** | Miễn phí + điện | Free 60h/tháng, sau đó $0.18/h |
| **Chia sẻ** | Khó | Dễ - share link Codespace |
| **Offline** | Có thể | Không |
| **Setup nhất quán** | Tùy máy | 100% giống nhau |

### Khi nào nên dùng Codespaces?

✅ **Nên dùng khi:**
- Máy yếu, ít RAM
- Muốn môi trường nhất quán cho team
- Demo/pair programming
- Máy không cài được Docker (Windows Home cũ)

❌ **Không nên dùng khi:**
- Làm offline nhiều
- Cần performance tối đa
- Đã vượt quota miễn phí

---

## 🔥 Troubleshooting

### ❌ Kafka không khởi động

```bash
# Xem logs
docker logs kafka

# Restart container
docker restart kafka

# Xem health status
docker inspect kafka --format='{{.State.Health.Status}}'
```

### ❌ Port không accessible

1. Vào tab **PORTS**
2. Đảm bảo port có visibility là **Public** hoặc **Private**
3. Click chuột phải → Change Port Protocol → HTTP

### ❌ Build lỗi

```bash
# Clean và rebuild
dotnet clean
dotnet restore
dotnet build
```

### ❌ Codespace chậm

1. Upgrade lên machine type lớn hơn:
   - Click ⚙️ ở bottom-left
   - Change Machine Type
   - Chọn 4-core thay vì 2-core

### ❌ Out of storage

```bash
# Xem dung lượng
df -h

# Dọn dẹp Docker
docker system prune -af

# Dọn NuGet cache
rm -rf ~/.nuget/packages/*
```

---

## 📚 Tài Liệu Tham Khảo

- [GitHub Codespaces Documentation](https://docs.github.com/en/codespaces)
- [Dev Container Specification](https://containers.dev/)
- [Kafka Documentation](https://kafka.apache.org/documentation/)
- [Kafka UI Documentation](https://docs.kafka-ui.provectus.io/)

---

## 🎉 Kết Luận

Với setup này, bạn có thể:

1. **Không cần Docker Desktop** trên máy local
2. **Mở Codespace từ browser** và có Kafka chạy sẵn
3. **Chia sẻ môi trường** với team members
4. **Học Kafka một cách chuyên nghiệp** với môi trường production-like

Happy Kafka coding! 🚀
