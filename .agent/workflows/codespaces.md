---
description: Khởi chạy Kafka trên GitHub Codespaces thay Docker local
---

# 🚀 Sử dụng GitHub Codespaces cho Kafka

## Bước 1: Push code lên GitHub (nếu chưa có)

// turbo
```bash
git add .devcontainer/
git commit -m "feat: add GitHub Codespaces configuration"
git push origin main
```

## Bước 2: Tạo Codespace

1. Vào repository trên GitHub
2. Click nút **<> Code** màu xanh
3. Chọn tab **Codespaces**
4. Click **Create codespace on main**

## Bước 3: Đợi Setup (~3-5 phút)

Codespace sẽ tự động:
- Khởi động Docker containers (Kafka + Kafka UI)
- Restore NuGet packages
- Build solution
- Forward ports

## Bước 4: Truy cập Kafka UI

1. Click tab **PORTS** ở bottom panel
2. Tìm port **8080** (Kafka UI)
3. Click biểu tượng 🌐 để mở trong browser

## Bước 5: Chạy ứng dụng .NET

// turbo
```bash
dotnet run --project KafkaDemo.API
```

## Kafka CLI Commands

// turbo
```bash
# Liệt kê topics
docker exec kafka kafka-topics.sh --bootstrap-server localhost:29092 --list
```

// turbo
```bash
# Tạo topic mới
docker exec kafka kafka-topics.sh --bootstrap-server localhost:29092 --create --topic test-topic --partitions 3 --replication-factor 1
```

## Lưu ý

- Free tier GitHub Codespaces: 60 giờ/tháng
- Dùng **Ctrl+C** để dừng các processes
- Codespace sẽ tự stop sau 30 phút không hoạt động
