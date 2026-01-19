# 🚀 HƯỚNG DẪN CHI TIẾT CI/CD VỚI JENKINS + DOCKER + CODESPACES

## 📋 Mục Lục

1. [Tổng Quan Kiến Trúc](#1-tổng-quan-kiến-trúc)
2. [Chi Tiết Từng File](#2-chi-tiết-từng-file)
3. [Luồng Hoạt Động CI/CD](#3-luồng-hoạt-động-cicd)
4. [Cách Setup Từ Đầu](#4-cách-setup-từ-đầu)
5. [Troubleshooting](#5-troubleshooting)

---

## 1. TỔNG QUAN KIẾN TRÚC

### 1.1 Sơ Đồ Hệ Thống

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         GITHUB CODESPACES                                    │
│                    (Máy ảo Ubuntu chạy Docker)                               │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                     DOCKER HOST (Codespace)                          │    │
│  │                                                                      │    │
│  │   ┌──────────────┐   ┌──────────────┐   ┌──────────────┐            │    │
│  │   │    Kafka     │   │  Kafka UI    │   │   Jenkins    │            │    │
│  │   │  Port 9092   │   │  Port 8080   │   │  Port 8081   │            │    │
│  │   │  Port 29092  │   │              │   │              │            │    │
│  │   └──────────────┘   └──────────────┘   └──────┬───────┘            │    │
│  │          │                                      │                    │    │
│  │          │                                      │ docker.sock        │    │
│  │          │                                      │ (gọi Docker Host)  │    │
│  │          │                                      ▼                    │    │
│  │          │                              ┌──────────────┐            │    │
│  │          └─────────────────────────────►│ KafkaDemo    │            │    │
│  │                  (gửi message)          │    API       │            │    │
│  │                                         │ Port 5000    │            │    │
│  │                                         └──────────────┘            │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

                               ▲
                               │ Webhook
                               │
                    ┌──────────┴──────────┐
                    │      GitHub         │
                    │   Repository        │
                    │ (kafka-demo)        │
                    └─────────────────────┘
```

### 1.2 Giải Thích Các Thành Phần

| Thành Phần | Vai Trò | Port |
|------------|---------|------|
| **Kafka** | Message Broker - nhận/gửi messages | 9092, 29092 |
| **Kafka UI** | Giao diện web xem messages | 8080 |
| **Jenkins** | CI/CD Server - tự động build/deploy | 8081 |
| **KafkaDemo API** | Ứng dụng .NET của bạn | 5000 |

### 1.3 Khái Niệm Quan Trọng: Docker-out-of-Docker

```
┌─────────────────────────────────────────────┐
│           CODESPACE (Host)                  │
│                                             │
│   /var/run/docker.sock  ◄───────────────┐   │
│         │                               │   │
│         ▼                               │   │
│   ┌──────────┐                          │   │
│   │ Docker   │◄──── commands ────────┐  │   │
│   │ Daemon   │                       │  │   │
│   └──────────┘                       │  │   │
│         │                            │  │   │
│         ├── Container: kafka         │  │   │
│         ├── Container: kafka-ui      │  │   │
│         ├── Container: jenkins ──────┘  │   │
│         │        │                      │   │
│         │        └── volume mount ──────┘   │
│         │            docker.sock            │
│         │                                   │
│         └── Container: kafkademo-api        │
│                                             │
└─────────────────────────────────────────────┘
```

**Giải thích:**
- Jenkins chạy TRONG một container Docker
- Jenkins cần gọi lệnh `docker build`, `docker run`
- Thay vì cài Docker trong Jenkins (Docker-in-Docker), ta mount `docker.sock` vào Jenkins
- Khi Jenkins gõ `docker ps`, nó thực ra đang "điều khiển" Docker của Host (Codespace)
- Đây gọi là **Docker-out-of-Docker** (DooD)

---

## 2. CHI TIẾT TỪNG FILE

### 2.1 KafkaDemo.API/Dockerfile

```dockerfile
# ===== STAGE 1: BUILD =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#   ▲
#   │ Image này NẶNG (~800MB) vì chứa SDK để compile code
#   │ Chỉ dùng trong quá trình build, không đưa vào production

WORKDIR /src
#   ▲
#   │ Tạo thư mục làm việc trong container

COPY KafkaDemo.API/*.csproj ./KafkaDemo.API/
COPY KafkaDemo.Infrastructure/*.csproj ./KafkaDemo.Infrastructure/
COPY KafkaDemo.Shared/*.csproj ./KafkaDemo.Shared/
COPY KafkaDemo.sln ./
#   ▲
#   │ Copy các file .csproj trước để tận dụng cache
#   │ Nếu dependency không đổi, Docker sẽ dùng cache layer

RUN dotnet restore
#   ▲
#   │ Tải các NuGet packages

COPY . .
#   ▲
#   │ Bây giờ mới copy toàn bộ source code

RUN dotnet publish KafkaDemo.API/KafkaDemo.API.csproj \
    -c Release -o /app/publish
#   ▲
#   │ Compile và publish ra thư mục /app/publish


# ===== STAGE 2: RUNTIME =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
#   ▲
#   │ Image này NHẸ (~200MB) vì chỉ chứa runtime
#   │ KHÔNG có SDK, KHÔNG có source code gốc

WORKDIR /app

COPY --from=build /app/publish .
#   ▲
#   │ Chỉ copy OUTPUT từ stage build
#   │ Source code, SDK đều bị bỏ lại

EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "KafkaDemo.API.dll"]
#   ▲
#   │ Lệnh chạy khi container start
```

**Tại sao dùng Multi-stage build?**
| | Single Stage | Multi-Stage |
|---|---|---|
| Image Size | ~1GB | ~200MB |
| Chứa SDK | ✅ Có | ❌ Không |
| Bảo mật | Kém (lộ SDK) | Tốt |
| Build time | Nhanh hơn lần đầu | Lâu hơn lần đầu |

---

### 2.2 .devcontainer/jenkins.Dockerfile

```dockerfile
FROM jenkins/jenkins:lts
#   ▲
#   │ Bắt đầu từ Jenkins chính thức

USER root
#   ▲
#   │ Switch sang root để có quyền cài đặt

# Install Docker CLI
RUN apt-get update && \
    apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release && \
    curl -fsSL https://download.docker.com/linux/debian/gpg | \
        gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) \
        signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] \
        https://download.docker.com/linux/debian \
        $(lsb_release -cs) stable" | \
        tee /etc/apt/sources.list.d/docker.list > /dev/null && \
    apt-get update && \
    apt-get install -y docker-ce-cli docker-compose-plugin && \
    rm -rf /var/lib/apt/lists/*
#   ▲
#   │ Cài Docker CLI (không phải Docker Daemon!)
#   │ CLI = công cụ gõ lệnh `docker build`, `docker run`
#   │ Daemon = engine thực sự chạy containers (đã có trên Host)

USER jenkins
#   ▲
#   │ Switch về user jenkins để bảo mật
```

**Tại sao cần custom Jenkins image?**

| Jenkins Default | Jenkins Custom |
|-----------------|----------------|
| ❌ Không có `docker` command | ✅ Có `docker` command |
| ❌ Không build được image | ✅ Build được image |
| ❌ Không deploy được | ✅ Deploy được |

---

### 2.3 docker-compose.yml

```yaml
services:
  # ========== KAFKA BROKER ==========
  kafka:
    image: apache/kafka:3.7.0
    container_name: kafka
    ports:
      - "9092:9092"     # External (máy local truy cập)
      - "29092:29092"   # Internal (containers khác truy cập)
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      # Cấu hình KRaft mode (không cần Zookeeper)
      
      KAFKA_LISTENERS: PLAINTEXT://0.0.0.0:29092,CONTROLLER://0.0.0.0:9093,EXTERNAL://0.0.0.0:9092
      #   ▲
      #   │ Kafka lắng nghe trên nhiều ports:
      #   │ - 29092: Các containers trong cùng network gọi
      #   │ - 9092: Máy local (ngoài Docker) gọi
      #   │ - 9093: Kafka cluster nội bộ (controller)
      
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:29092,EXTERNAL://localhost:9092
      #   ▲
      #   │ Khi client hỏi "địa chỉ Kafka là gì?":
      #   │ - Nếu là container: trả về kafka:29092
      #   │ - Nếu là localhost: trả về localhost:9092
      
    healthcheck:
      test: ["/opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:29092 --list"]
      interval: 10s
      timeout: 10s
      retries: 5
      start_period: 30s
      #   ▲
      #   │ Kiểm tra Kafka đã sẵn sàng chưa
      #   │ Các service dependent sẽ đợi


  # ========== KAFKA UI ==========
  kafka-ui:
    image: provectuslabs/kafka-ui:v0.7.2
    container_name: kafka-ui
    ports:
      - "8080:8080"
    environment:
      KAFKA_CLUSTERS_0_NAME: KafkaDemo
      KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS: kafka:29092
      #   ▲
      #   │ UI kết nối tới Kafka qua internal network
      #   │ Dùng tên container "kafka" thay vì localhost
    depends_on:
      kafka:
        condition: service_healthy
        #   ▲
        #   │ Đợi Kafka healthy trước khi start


  # ========== .NET API ==========
  api:
    build:
      context: .
      dockerfile: KafkaDemo.API/Dockerfile
    container_name: kafkademo-api
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Kafka__BootstrapServers=kafka:29092
      #   ▲
      #   │ Cấu hình Kafka connection string
      #   │ Double underscore (__) = nested config
      #   │ Tương đương: { "Kafka": { "BootstrapServers": "kafka:29092" } }
    depends_on:
      kafka:
        condition: service_healthy
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "--fail", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3


  # ========== JENKINS CI/CD ==========
  jenkins:
    build:
      context: .
      dockerfile: .devcontainer/jenkins.Dockerfile
      #   ▲
      #   │ Dùng custom image đã cài Docker CLI
      
    container_name: jenkins
    ports:
      - "8081:8080"     # Jenkins UI
      - "50000:50000"   # Jenkins agent
    volumes:
      - jenkins_data:/var/jenkins_home
      #   ▲
      #   │ Persist dữ liệu Jenkins (jobs, plugins, settings)
      
      - /var/run/docker.sock:/var/run/docker.sock
      #   ▲
      #   │ QUAN TRỌNG NHẤT!
      #   │ Mount Docker socket từ Host vào Jenkins
      #   │ Cho phép Jenkins điều khiển Docker của Host
      
    environment:
      - JAVA_OPTS=-Djenkins.install.runSetupWizard=false
      #   ▲
      #   │ Bỏ qua setup wizard (không hỏi password lần đầu)
      
    user: root
    #   ▲
    #   │ Chạy với root để có quyền gọi Docker socket
    
    restart: unless-stopped


volumes:
  jenkins_data:
    # Volume để lưu dữ liệu Jenkins giữa các lần restart
```

---

### 2.4 Jenkinsfile (Pipeline Script)

```groovy
pipeline {
    agent any
    // ▲ Chạy trên bất kỳ agent nào (ở đây chỉ có 1 agent là Jenkins master)
    
    environment {
        DOCKER_IMAGE = 'kafkademo-api'
        DOCKER_TAG = "${BUILD_NUMBER}"
        // ▲ Mỗi build sẽ có tag riêng: kafkademo-api:1, kafkademo-api:2, ...
    }
    
    triggers {
        githubPush()
        // ▲ Tự động chạy pipeline khi nhận webhook từ GitHub
    }
    
    stages {
        // ===== STAGE 1: CHECKOUT =====
        stage('Checkout') {
            steps {
                echo '📥 Checking out code...'
                git branch: 'master', url: 'https://github.com/long1712578/kafka-demo.git'
                // ▲ Clone/pull code từ GitHub về workspace của Jenkins
                // Workspace: /var/jenkins_home/workspace/KafkaDemo-Pipeline/
            }
        }
        
        // ===== STAGE 2: BUILD DOCKER IMAGE =====
        stage('Build Docker Image') {
            steps {
                echo '🐳 Building Docker image...'
                sh """
                    cd ${WORKSPACE}
                    docker build -t ${DOCKER_IMAGE}:${DOCKER_TAG} -f KafkaDemo.API/Dockerfile .
                    docker tag ${DOCKER_IMAGE}:${DOCKER_TAG} ${DOCKER_IMAGE}:latest
                """
                // ▲ Jenkins gọi Docker CLI
                // Docker CLI gọi Docker Daemon của Host (qua socket)
                // Docker Daemon build image dựa trên Dockerfile
            }
        }
        
        // ===== STAGE 3: DEPLOY =====
        stage('Deploy') {
            steps {
                echo '🚀 Deploying API container...'
                sh """
                    # Stop container cũ (nếu có)
                    docker stop kafkademo-api || true
                    docker rm kafkademo-api || true
                    
                    # Run container mới
                    docker run -d \
                        --name kafkademo-api \
                        --network kafka-demo_default \
                        -p 5000:5000 \
                        -e ASPNETCORE_ENVIRONMENT=Production \
                        -e Kafka__BootstrapServers=kafka:29092 \
                        --restart unless-stopped \
                        ${DOCKER_IMAGE}:${DOCKER_TAG}
                """
                // ▲ QUAN TRỌNG:
                // --network kafka-demo_default: Join vào cùng network với Kafka
                // Nếu không có dòng này, container API không kết nối được Kafka!
            }
        }
        
        // ===== STAGE 4: VERIFY =====
        stage('Verify') {
            steps {
                echo '✅ Verifying deployment...'
                sh """
                    sleep 5
                    docker ps | grep kafkademo-api
                    echo "API deployed successfully with build #${BUILD_NUMBER}"
                """
            }
        }
    }
    
    post {
        success {
            echo '🎉 Build and deploy successful!'
        }
        failure {
            echo '❌ Build failed!'
        }
        cleanup {
            echo '🧹 Cleaning up old images...'
            sh 'docker image prune -f || true'
            // ▲ Xóa các image không sử dụng để tiết kiệm disk
        }
    }
}
```

---

## 3. LUỒNG HOẠT ĐỘNG CI/CD

### 3.1 Sơ Đồ Luồng Hoạt Động

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           CI/CD FLOW                                          │
└──────────────────────────────────────────────────────────────────────────────┘

 YOU               GITHUB              JENKINS            DOCKER            APP
  │                  │                    │                  │                │
  │ 1. git push      │                    │                  │                │
  ├─────────────────►│                    │                  │                │
  │                  │                    │                  │                │
  │                  │ 2. Webhook POST    │                  │                │
  │                  ├───────────────────►│                  │                │
  │                  │                    │                  │                │
  │                  │                    │ 3. git clone     │                │
  │                  │◄───────────────────┤                  │                │
  │                  │                    │                  │                │
  │                  │                    │ 4. docker build  │                │
  │                  │                    ├─────────────────►│                │
  │                  │                    │                  │                │
  │                  │                    │ 5. docker stop   │                │
  │                  │                    ├─────────────────►│────────────────┤
  │                  │                    │                  │   (old dead)   │
  │                  │                    │                  │                │
  │                  │                    │ 6. docker run    │                │
  │                  │                    ├─────────────────►│                │
  │                  │                    │                  │   ┌────────┐   │
  │                  │                    │                  │   │  NEW   │   │
  │                  │                    │                  │   │  APP   │◄──┤
  │                  │                    │                  │   │ :5000  │   │
  │                  │                    │                  │   └────────┘   │
  │                  │                    │                  │                │
  │ 7. Access app    │                    │                  │                │
  ├──────────────────┼────────────────────┼──────────────────┼───────────────►│
  │                  │                    │                  │                │
  ▼                  ▼                    ▼                  ▼                ▼
```

### 3.2 Giải Thích Chi Tiết Từng Bước

#### **Bước 1: Bạn Push Code**
```bash
# Trong VS Code (Codespace hoặc local)
git add .
git commit -m "feat: thêm tính năng XYZ"
git push origin master
```

#### **Bước 2: GitHub Gửi Webhook**
```
POST https://codespaces-xxxx-8081.app.github.dev/github-webhook/

Headers:
  X-GitHub-Event: push
  Content-Type: application/json

Body:
{
  "ref": "refs/heads/master",
  "after": "abc123...",
  "commits": [...]
}
```

#### **Bước 3: Jenkins Nhận Webhook và Clone Code**
```
Jenkins Log:
[Pipeline] Start of Pipeline
[Pipeline] stage (Checkout)
[Pipeline] git
Cloning repository https://github.com/long1712578/kafka-demo.git
> git checkout -f abc123...
```

#### **Bước 4: Jenkins Gọi Docker Build**
```bash
# Jenkins thực thi:
docker build -t kafkademo-api:15 -f KafkaDemo.API/Dockerfile .

# Docker Daemon (trên Host) thực hiện:
# Step 1/12: FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Step 2/12: WORKDIR /src
# ...
# Successfully built abc123
# Successfully tagged kafkademo-api:15
```

#### **Bước 5 & 6: Jenkins Deploy Container Mới**
```bash
# Stop container cũ
docker stop kafkademo-api
# kafkademo-api

docker rm kafkademo-api
# kafkademo-api

# Run container mới
docker run -d \
    --name kafkademo-api \
    --network kafka-demo_default \
    -p 5000:5000 \
    -e Kafka__BootstrapServers=kafka:29092 \
    kafkademo-api:15
# a1b2c3d4e5f6...
```

#### **Bước 7: Truy Cập Ứng Dụng**
- Trong Codespace: Mở port 5000 → Click 🌐
- URL: `https://codespaces-xxxx-5000.app.github.dev`

---

## 4. CÁCH SETUP TỪ ĐẦU

### 4.1 Prerequisites

- GitHub Account
- Repository với code .NET
- GitHub Codespace (hoặc máy có Docker)

### 4.2 Step-by-Step

#### **Step 1: Tạo các file cần thiết**

```
KafkaDemo/
├── .devcontainer/
│   └── jenkins.Dockerfile      # Custom Jenkins với Docker CLI
├── KafkaDemo.API/
│   └── Dockerfile              # Multi-stage build cho .NET
├── docker-compose.yml          # Định nghĩa services
├── Jenkinsfile                 # Pipeline script
└── ...
```

#### **Step 2: Start Services trong Codespace**

```bash
# Mở terminal trong Codespace
docker compose up -d

# Kiểm tra các containers
docker ps

# Expected output:
# CONTAINER ID   IMAGE              PORTS                    NAMES
# abc123         kafka              9092/tcp, 29092/tcp     kafka
# def456         kafka-ui           8080/tcp                kafka-ui
# ghi789         jenkins            8081->8080/tcp          jenkins
```

#### **Step 3: Truy cập Jenkins UI**

1. Tab **PORTS** trong VS Code Codespace
2. Tìm port **8081**
3. Click 🌐 để mở browser

#### **Step 4: Tạo Pipeline Job trong Jenkins**

1. Click **"New Item"**
2. Tên: `KafkaDemo-Pipeline`
3. Loại: **Pipeline**
4. Click OK

5. Scroll xuống **Pipeline** section
6. Definition: **Pipeline script**
7. Paste toàn bộ nội dung Jenkinsfile
8. Click **Save**

#### **Step 5: Setup GitHub Webhook**

1. GitHub → Repository → Settings → Webhooks → Add webhook
2. **Payload URL**: `https://<codespace-name>-8081.app.github.dev/github-webhook/`
   
   Lấy URL:
   ```bash
   echo "https://$(hostname)-8081.app.github.dev/github-webhook/"
   ```

3. **Content type**: `application/json`
4. **SSL verification**: Disable
5. **Events**: Just the push event
6. Click **Add webhook**

#### **Step 6: Enable Trigger trong Jenkins**

1. Jenkins → KafkaDemo-Pipeline → Configure
2. **Build Triggers** section
3. ✅ Tick **"GitHub hook trigger for GITScm polling"**
4. Click **Save**

#### **Step 7: Test**

```bash
# Push empty commit để test
git commit --allow-empty -m "test: trigger webhook"
git push
```

Jenkins sẽ tự động start build!

---

## 5. TROUBLESHOOTING

### 5.1 Lỗi: `docker: not found`

**Nguyên nhân:** Jenkins container không có Docker CLI.

**Fix:** Dùng custom Jenkins image với Docker CLI đã cài.

### 5.2 Lỗi: `checkout scm` only available in Multibranch Pipeline

**Nguyên nhân:** Bạn dùng Pipeline script (paste trực tiếp) nhưng code có `checkout scm`.

**Fix:** Thay `checkout scm` bằng:
```groovy
git branch: 'master', url: 'https://github.com/username/repo.git'
```

### 5.3 Lỗi: Container name already in use

**Nguyên nhân:** Container cũ chưa được xóa.

**Fix:** Thêm lệnh stop/rm trước khi run:
```bash
docker stop kafkademo-api || true
docker rm kafkademo-api || true
docker run -d ...
```

### 5.4 Lỗi: Webhook không trigger Jenkins

**Nguyên nhân:**
1. URL webhook sai (hostname Codespace thay đổi)
2. SSL verification enabled
3. Chưa enable trigger trong Jenkins

**Fix:**
1. Kiểm tra hostname hiện tại: `hostname`
2. Update webhook URL trong GitHub
3. Disable SSL verification
4. Enable "GitHub hook trigger for GITScm polling" trong Jenkins

### 5.5 Lỗi: API không kết nối được Kafka

**Nguyên nhân:** Container API không cùng network với Kafka.

**Fix:** Thêm `--network kafka-demo_default` khi `docker run`.

---

## 📚 TÓM TẮT

| File | Vai Trò |
|------|---------|
| `Dockerfile` | Đóng gói .NET app thành Docker image |
| `jenkins.Dockerfile` | Custom Jenkins với Docker CLI |
| `docker-compose.yml` | Định nghĩa và kết nối các services |
| `Jenkinsfile` | Script tự động hóa CI/CD |

| Bước | Mô Tả |
|------|-------|
| 1 | Developer push code |
| 2 | GitHub gửi webhook |
| 3 | Jenkins clone code |
| 4 | Jenkins build Docker image |
| 5 | Jenkins deploy container mới |
| 6 | App chạy với phiên bản mới |

**Kết quả cuối cùng:** Mỗi khi bạn push code, ứng dụng tự động được build và deploy mà không cần làm thủ công! 🎉
