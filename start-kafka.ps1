# ===================================================
# KAFKA CLUSTER SETUP & TEST SCRIPT
# ===================================================
# Mục đích: Start Kafka cluster và verify tất cả services
# Author: Kafka Learning Project
# ===================================================

Write-Host "🚀 KAFKA LEARNING ENVIRONMENT SETUP" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check Docker
Write-Host "📋 Step 1: Checking Docker..." -ForegroundColor Yellow
if (!(Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Docker not found! Please install Docker Desktop." -ForegroundColor Red
    exit 1
}

$dockerRunning = docker info 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker is not running! Please start Docker Desktop." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Docker is running" -ForegroundColor Green
Write-Host ""

# Step 2: Navigate to kafka directory
Write-Host "📋 Step 2: Navigating to kafka directory..." -ForegroundColor Yellow
$kafkaDir = Join-Path $PSScriptRoot "kafka"
if (!(Test-Path $kafkaDir)) {
    Write-Host "❌ Kafka directory not found: $kafkaDir" -ForegroundColor Red
    exit 1
}
Set-Location $kafkaDir
Write-Host "✅ Current directory: $kafkaDir" -ForegroundColor Green
Write-Host ""

# Step 3: Stop existing containers (cleanup)
Write-Host "📋 Step 3: Cleaning up existing containers..." -ForegroundColor Yellow
docker-compose down -v 2>&1 | Out-Null
Write-Host "✅ Cleanup completed" -ForegroundColor Green
Write-Host ""

# Step 4: Start Kafka cluster
Write-Host "📋 Step 4: Starting Kafka cluster..." -ForegroundColor Yellow
Write-Host "   This may take 2-3 minutes..." -ForegroundColor Gray
docker-compose up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to start Kafka cluster!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Kafka cluster started" -ForegroundColor Green
Write-Host ""

# Step 5: Wait for services to be ready
Write-Host "📋 Step 5: Waiting for services to be ready..." -ForegroundColor Yellow
Write-Host "   Waiting 30 seconds for initialization..." -ForegroundColor Gray
Start-Sleep -Seconds 30

# Check Zookeeper
Write-Host "   Checking Zookeeper..." -ForegroundColor Gray
$zkStatus = docker exec zookeeper1 echo "ruok" 2>&1
if ($zkStatus -match "imok") {
    Write-Host "   ✅ Zookeeper is ready" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Zookeeper may not be fully ready" -ForegroundColor Yellow
}

# Check Kafka brokers
Write-Host "   Checking Kafka brokers..." -ForegroundColor Gray
$kafka1Status = docker exec kafka1 kafka-broker-api-versions --bootstrap-server localhost:9092 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Kafka brokers are ready" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Kafka brokers may not be fully ready" -ForegroundColor Yellow
}

Write-Host ""

# Step 6: Display running services
Write-Host "📋 Step 6: Running services:" -ForegroundColor Yellow
docker-compose ps
Write-Host ""

# Step 7: Create test topic
Write-Host "📋 Step 7: Creating test topic..." -ForegroundColor Yellow
$createTopic = docker exec kafka-tools kafka-topics --create `
    --topic learning-test-topic `
    --partitions 3 `
    --replication-factor 3 `
    --bootstrap-server kafka1:9092 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Test topic 'learning-test-topic' created" -ForegroundColor Green
} else {
    Write-Host "⚠️  Topic may already exist or brokers not ready yet" -ForegroundColor Yellow
}
Write-Host ""

# Step 8: Verify topic
Write-Host "📋 Step 8: Verifying topic..." -ForegroundColor Yellow
docker exec kafka-tools kafka-topics --describe `
    --topic learning-test-topic `
    --bootstrap-server kafka1:9092
Write-Host ""

# Step 9: Test producer
Write-Host "📋 Step 9: Testing producer..." -ForegroundColor Yellow
$testMessage = "Hello Kafka! Test message at $(Get-Date)"
echo $testMessage | docker exec -i kafka-tools kafka-console-producer `
    --topic learning-test-topic `
    --bootstrap-server kafka1:9092

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Test message produced successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to produce test message" -ForegroundColor Red
}
Write-Host ""

# Step 10: Test consumer (read last message)
Write-Host "📋 Step 10: Testing consumer (reading last message)..." -ForegroundColor Yellow
Write-Host "   Press Ctrl+C after seeing the message..." -ForegroundColor Gray
Start-Sleep -Seconds 2

$job = Start-Job -ScriptBlock {
    docker exec kafka-tools kafka-console-consumer `
        --topic learning-test-topic `
        --from-beginning `
        --max-messages 1 `
        --bootstrap-server kafka1:9092 2>&1
}

Wait-Job $job -Timeout 10 | Out-Null
$output = Receive-Job $job
Remove-Job $job -Force

if ($output -match "Hello Kafka") {
    Write-Host "✅ Consumer received message successfully" -ForegroundColor Green
} else {
    Write-Host "⚠️  Consumer test inconclusive" -ForegroundColor Yellow
}
Write-Host ""

# Step 11: Display access information
Write-Host "🎉 SETUP COMPLETED!" -ForegroundColor Green
Write-Host "==================" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Access Points:" -ForegroundColor Cyan
Write-Host "   Kafka Brokers:" -ForegroundColor White
Write-Host "      Broker 1: localhost:19092" -ForegroundColor Gray
Write-Host "      Broker 2: localhost:29092" -ForegroundColor Gray
Write-Host "      Broker 3: localhost:39092" -ForegroundColor Gray
Write-Host ""
Write-Host "   🎮 Kafka UI:        http://localhost:8080" -ForegroundColor White
Write-Host "   📊 AKHQ:            http://localhost:8082" -ForegroundColor White
Write-Host "   📈 Grafana:         http://localhost:3000 (admin/admin)" -ForegroundColor White
Write-Host "   📉 Prometheus:      http://localhost:9090" -ForegroundColor White
Write-Host "   🔧 Schema Registry: http://localhost:8081" -ForegroundColor White
Write-Host "   🔌 Kafka Connect:   http://localhost:8083" -ForegroundColor White
Write-Host ""

Write-Host "🎓 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Open Kafka UI: http://localhost:8080" -ForegroundColor White
Write-Host "   2. Explore the 'learning-test-topic'" -ForegroundColor White
Write-Host "   3. Read KAFKA_LEARNING_GUIDE.md" -ForegroundColor White
Write-Host "   4. Build .NET projects: dotnet build" -ForegroundColor White
Write-Host "   5. Run examples: dotnet run --project KafkaDemo.API" -ForegroundColor White
Write-Host ""

Write-Host "📚 Useful Commands:" -ForegroundColor Cyan
Write-Host "   List topics:" -ForegroundColor White
Write-Host "      docker exec kafka-tools kafka-topics --list --bootstrap-server kafka1:9092" -ForegroundColor Gray
Write-Host ""
Write-Host "   Describe cluster:" -ForegroundColor White
Write-Host "      docker exec kafka-tools kafka-broker-api-versions --bootstrap-server kafka1:9092" -ForegroundColor Gray
Write-Host ""
Write-Host "   View logs:" -ForegroundColor White
Write-Host "      docker logs kafka1 -f" -ForegroundColor Gray
Write-Host ""
Write-Host "   Stop cluster:" -ForegroundColor White
Write-Host "      docker-compose down" -ForegroundColor Gray
Write-Host ""

Write-Host "Happy Learning! 🚀" -ForegroundColor Green
