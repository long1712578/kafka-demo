# ===================================================
# KAFKA EXERCISES - HANDS-ON LEARNING
# ===================================================
# Hướng dẫn thực hành từng bước để master Kafka
# ===================================================

Write-Host "🎓 KAFKA HANDS-ON EXERCISES" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan
Write-Host ""

function Show-Menu {
    Write-Host "Select an exercise:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. Create Topic với Custom Configuration" -ForegroundColor White
    Write-Host "  2. Test Partitioning Strategies" -ForegroundColor White
    Write-Host "  3. Consumer Groups Demo" -ForegroundColor White
    Write-Host "  4. Replication & Failover Test" -ForegroundColor White
    Write-Host "  5. Performance Testing" -ForegroundColor White
    Write-Host "  6. Monitor Consumer Lag" -ForegroundColor White
    Write-Host "  7. Advanced: Transactions Demo" -ForegroundColor White
    Write-Host "  8. Cleanup (Delete Test Topics)" -ForegroundColor White
    Write-Host "  0. Exit" -ForegroundColor White
    Write-Host ""
}

function Exercise-1 {
    Write-Host ""
    Write-Host "=== Exercise 1: Create Topic với Custom Configuration ===" -ForegroundColor Cyan
    Write-Host ""
    
    $topicName = Read-Host "Enter topic name (default: exercise-1-topic)"
    if ([string]::IsNullOrWhiteSpace($topicName)) {
        $topicName = "exercise-1-topic"
    }
    
    $partitions = Read-Host "Number of partitions (default: 3)"
    if ([string]::IsNullOrWhiteSpace($partitions)) {
        $partitions = 3
    }
    
    $replication = Read-Host "Replication factor (default: 3)"
    if ([string]::IsNullOrWhiteSpace($replication)) {
        $replication = 3
    }
    
    Write-Host ""
    Write-Host "Creating topic: $topicName" -ForegroundColor Yellow
    Write-Host "  Partitions: $partitions" -ForegroundColor Gray
    Write-Host "  Replication Factor: $replication" -ForegroundColor Gray
    
    docker exec kafka-tools kafka-topics --create `
        --topic $topicName `
        --partitions $partitions `
        --replication-factor $replication `
        --config retention.ms=86400000 `
        --config compression.type=snappy `
        --config min.insync.replicas=2 `
        --bootstrap-server kafka1:9092
    
    Write-Host ""
    Write-Host "✅ Topic created! Details:" -ForegroundColor Green
    docker exec kafka-tools kafka-topics --describe `
        --topic $topicName `
        --bootstrap-server kafka1:9092
    
    Write-Host ""
    Write-Host "💡 Learning Points:" -ForegroundColor Cyan
    Write-Host "   - Partitions: Cho phép parallel processing" -ForegroundColor White
    Write-Host "   - Replication: Đảm bảo data safety" -ForegroundColor White
    Write-Host "   - Min ISR: Tối thiểu replicas phải sync" -ForegroundColor White
    Write-Host ""
    
    Pause
}

function Exercise-2 {
    Write-Host ""
    Write-Host "=== Exercise 2: Test Partitioning Strategies ===" -ForegroundColor Cyan
    Write-Host ""
    
    $topic = "partitioning-test"
    
    Write-Host "Creating topic: $topic (6 partitions)..." -ForegroundColor Yellow
    docker exec kafka-tools kafka-topics --create `
        --topic $topic `
        --partitions 6 `
        --replication-factor 3 `
        --bootstrap-server kafka1:9092 2>&1 | Out-Null
    
    Write-Host "✅ Topic created" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Producing messages với different keys..." -ForegroundColor Yellow
    
    # Produce messages with keys
    for ($i = 1; $i -le 20; $i++) {
        $key = "key-$($i % 3)"  # 3 keys: key-0, key-1, key-2
        $message = "Message $i with key $key"
        echo "$key`:$message" | docker exec -i kafka-tools kafka-console-producer `
            --topic $topic `
            --property "parse.key=true" `
            --property "key.separator=:" `
            --bootstrap-server kafka1:9092 2>&1 | Out-Null
        
        Write-Host "  Sent: $message" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "✅ Messages sent!" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "📊 Check message distribution in Kafka UI:" -ForegroundColor Cyan
    Write-Host "   http://localhost:8080/ui/clusters/kafka-cluster-learning/topics/$topic" -ForegroundColor White
    Write-Host ""
    
    Write-Host "💡 Learning Points:" -ForegroundColor Cyan
    Write-Host "   - Same key → same partition (ordering guarantee)" -ForegroundColor White
    Write-Host "   - Different keys → load balanced across partitions" -ForegroundColor White
    Write-Host "   - No key → round-robin distribution" -ForegroundColor White
    Write-Host ""
    
    Pause
}

function Exercise-3 {
    Write-Host ""
    Write-Host "=== Exercise 3: Consumer Groups Demo ===" -ForegroundColor Cyan
    Write-Host ""
    
    $topic = "consumer-group-test"
    
    Write-Host "Creating topic with 6 partitions..." -ForegroundColor Yellow
    docker exec kafka-tools kafka-topics --create `
        --topic $topic `
        --partitions 6 `
        --replication-factor 3 `
        --bootstrap-server kafka1:9092 2>&1 | Out-Null
    
    Write-Host "✅ Topic created" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Producing 100 test messages..." -ForegroundColor Yellow
    for ($i = 1; $i -le 100; $i++) {
        echo "Message $i - $(Get-Date)" | docker exec -i kafka-tools kafka-console-producer `
            --topic $topic `
            --bootstrap-server kafka1:9092 2>&1 | Out-Null
    }
    Write-Host "✅ Messages produced" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "🎯 Now, manually open 3 terminals and run:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Terminal 1:" -ForegroundColor Yellow
    Write-Host "docker exec -it kafka-tools kafka-console-consumer --topic $topic --group demo-group --bootstrap-server kafka1:9092" -ForegroundColor White
    Write-Host ""
    Write-Host "Terminal 2:" -ForegroundColor Yellow
    Write-Host "docker exec -it kafka-tools kafka-console-consumer --topic $topic --group demo-group --bootstrap-server kafka1:9092" -ForegroundColor White
    Write-Host ""
    Write-Host "Terminal 3:" -ForegroundColor Yellow
    Write-Host "docker exec -it kafka-tools kafka-console-consumer --topic $topic --group demo-group --bootstrap-server kafka1:9092" -ForegroundColor White
    Write-Host ""
    
    Write-Host "💡 Observe:" -ForegroundColor Cyan
    Write-Host "   - Each consumer gets different partitions" -ForegroundColor White
    Write-Host "   - When you stop a consumer, partitions rebalance" -ForegroundColor White
    Write-Host "   - Each message consumed by only 1 consumer in group" -ForegroundColor White
    Write-Host ""
    
    Write-Host "📊 Monitor consumer groups:" -ForegroundColor Cyan
    docker exec kafka-tools kafka-consumer-groups --describe `
        --group demo-group `
        --bootstrap-server kafka1:9092
    
    Write-Host ""
    Pause
}

function Exercise-4 {
    Write-Host ""
    Write-Host "=== Exercise 4: Replication & Failover Test ===" -ForegroundColor Cyan
    Write-Host ""
    
    $topic = "failover-test"
    
    Write-Host "Step 1: Create topic with RF=3..." -ForegroundColor Yellow
    docker exec kafka-tools kafka-topics --create `
        --topic $topic `
        --partitions 1 `
        --replication-factor 3 `
        --bootstrap-server kafka1:9092
    
    Write-Host ""
    Write-Host "✅ Topic created. Current state:" -ForegroundColor Green
    docker exec kafka-tools kafka-topics --describe `
        --topic $topic `
        --bootstrap-server kafka1:9092
    
    Write-Host ""
    $leader = Read-Host "Which broker is the leader? (1/2/3)"
    
    Write-Host ""
    Write-Host "Step 2: Producing messages..." -ForegroundColor Yellow
    for ($i = 1; $i -le 10; $i++) {
        echo "Message $i before failover" | docker exec -i kafka-tools kafka-console-producer `
            --topic $topic `
            --bootstrap-server kafka1:9092 2>&1 | Out-Null
    }
    Write-Host "✅ 10 messages produced" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "Step 3: Simulating broker failure..." -ForegroundColor Yellow
    Write-Host "Stopping kafka$leader..." -ForegroundColor Red
    docker stop kafka$leader
    
    Write-Host ""
    Write-Host "⏳ Waiting for leader election (5 seconds)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    Write-Host ""
    Write-Host "✅ New state after failover:" -ForegroundColor Green
    $otherBroker = if ($leader -eq "1") { "2" } else { "1" }
    docker exec kafka-tools kafka-topics --describe `
        --topic $topic `
        --bootstrap-server kafka${otherBroker}:9092
    
    Write-Host ""
    Write-Host "Step 4: Producing more messages (cluster still works!)..." -ForegroundColor Yellow
    for ($i = 11; $i -le 20; $i++) {
        echo "Message $i after failover" | docker exec -i kafka-tools kafka-console-producer `
            --topic $topic `
            --bootstrap-server kafka${otherBroker}:9092 2>&1 | Out-Null
    }
    Write-Host "✅ 10 more messages produced (despite broker failure!)" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "Step 5: Verify all messages are still available..." -ForegroundColor Yellow
    $count = docker exec kafka-tools kafka-run-class kafka.tools.GetOffsetShell `
        --broker-list kafka${otherBroker}:9092 `
        --topic $topic | Select-String -Pattern ":\d+" | ForEach-Object { $_.Matches.Value.TrimStart(':') }
    
    Write-Host "✅ Total messages in topic: $count" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "Step 6: Restarting failed broker..." -ForegroundColor Yellow
    docker start kafka$leader
    Start-Sleep -Seconds 10
    Write-Host "✅ Broker restarted and syncing..." -ForegroundColor Green
    
    Write-Host ""
    Write-Host "💡 Learning Points:" -ForegroundColor Cyan
    Write-Host "   - Replication ensures no data loss" -ForegroundColor White
    Write-Host "   - Automatic leader election" -ForegroundColor White
    Write-Host "   - Cluster continues working with available brokers" -ForegroundColor White
    Write-Host "   - Failed broker rejoins and catches up" -ForegroundColor White
    Write-Host ""
    
    Pause
}

function Exercise-5 {
    Write-Host ""
    Write-Host "=== Exercise 5: Performance Testing ===" -ForegroundColor Cyan
    Write-Host ""
    
    $topic = "performance-test"
    
    Write-Host "Creating topic..." -ForegroundColor Yellow
    docker exec kafka-tools kafka-topics --create `
        --topic $topic `
        --partitions 6 `
        --replication-factor 3 `
        --bootstrap-server kafka1:9092 2>&1 | Out-Null
    
    Write-Host "✅ Topic created" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "🚀 Running producer performance test..." -ForegroundColor Yellow
    Write-Host "   Producing 10,000 messages..." -ForegroundColor Gray
    docker exec kafka-tools kafka-producer-perf-test `
        --topic $topic `
        --num-records 10000 `
        --record-size 1024 `
        --throughput -1 `
        --producer-props bootstrap.servers=kafka1:9092 `
                           acks=all `
                           compression.type=snappy
    
    Write-Host ""
    Write-Host "🚀 Running consumer performance test..." -ForegroundColor Yellow
    docker exec kafka-tools kafka-consumer-perf-test `
        --topic $topic `
        --messages 10000 `
        --bootstrap-server kafka1:9092 `
        --group perf-test-group
    
    Write-Host ""
    Write-Host "💡 Learning Points:" -ForegroundColor Cyan
    Write-Host "   - Throughput: messages/second" -ForegroundColor White
    Write-Host "   - Latency: Average, P99, P999" -ForegroundColor White
    Write-Host "   - Batching improves throughput" -ForegroundColor White
    Write-Host "   - Compression reduces network usage" -ForegroundColor White
    Write-Host ""
    
    Pause
}

function Exercise-6 {
    Write-Host ""
    Write-Host "=== Exercise 6: Monitor Consumer Lag ===" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "📊 Listing all consumer groups:" -ForegroundColor Yellow
    docker exec kafka-tools kafka-consumer-groups --list `
        --bootstrap-server kafka1:9092
    
    Write-Host ""
    $group = Read-Host "Enter consumer group name to monitor"
    
    if ([string]::IsNullOrWhiteSpace($group)) {
        Write-Host "❌ No group specified" -ForegroundColor Red
        return
    }
    
    Write-Host ""
    Write-Host "📊 Consumer group details:" -ForegroundColor Yellow
    docker exec kafka-tools kafka-consumer-groups --describe `
        --group $group `
        --bootstrap-server kafka1:9092
    
    Write-Host ""
    Write-Host "💡 Understanding the output:" -ForegroundColor Cyan
    Write-Host "   CURRENT-OFFSET: Where consumer has read to" -ForegroundColor White
    Write-Host "   LOG-END-OFFSET: Latest offset in partition" -ForegroundColor White
    Write-Host "   LAG: Messages behind (LOG-END - CURRENT)" -ForegroundColor White
    Write-Host ""
    Write-Host "   LAG = 0: Consumer is caught up ✅" -ForegroundColor White
    Write-Host "   LAG > 0: Consumer is behind ⚠️" -ForegroundColor White
    Write-Host ""
    
    Write-Host "📊 Also check in Grafana: http://localhost:3000" -ForegroundColor Cyan
    Write-Host ""
    
    Pause
}

function Exercise-7 {
    Write-Host ""
    Write-Host "=== Exercise 7: Transactions Demo ===" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "🎯 This requires .NET code - TransactionalKafkaProducer" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Example usage:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host @"
var producer = new TransactionalKafkaProducer("localhost:19092", logger);

// All messages committed atomically
await producer.PublishTransactionalBatchAsync("orders", new[]
{
    new KafkaMessage { Content = "Order 1" },
    new KafkaMessage { Content = "Order 2" },
    new KafkaMessage { Content = "Order 3" }
});
"@ -ForegroundColor White
    
    Write-Host ""
    Write-Host "💡 Key Concepts:" -ForegroundColor Cyan
    Write-Host "   - TransactionalId: Required for transactions" -ForegroundColor White
    Write-Host "   - EnableIdempotence: Must be true" -ForegroundColor White
    Write-Host "   - Acks=All: Required for exactly-once" -ForegroundColor White
    Write-Host "   - All-or-nothing: Either all messages commit or none" -ForegroundColor White
    Write-Host ""
    Write-Host "📚 See: KafkaDemo.Infrastructure/Producers/TransactionalKafkaProducer.cs" -ForegroundColor Cyan
    Write-Host ""
    
    Pause
}

function Exercise-8 {
    Write-Host ""
    Write-Host "=== Exercise 8: Cleanup ===" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "📋 Current topics:" -ForegroundColor Yellow
    docker exec kafka-tools kafka-topics --list --bootstrap-server kafka1:9092
    
    Write-Host ""
    $confirm = Read-Host "Delete all test topics? (y/n)"
    
    if ($confirm -eq 'y') {
        $topics = @(
            "exercise-1-topic",
            "partitioning-test",
            "consumer-group-test",
            "failover-test",
            "performance-test",
            "learning-test-topic"
        )
        
        foreach ($topic in $topics) {
            Write-Host "Deleting $topic..." -ForegroundColor Yellow
            docker exec kafka-tools kafka-topics --delete `
                --topic $topic `
                --bootstrap-server kafka1:9092 2>&1 | Out-Null
        }
        
        Write-Host "✅ Cleanup completed" -ForegroundColor Green
    } else {
        Write-Host "Cancelled" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Pause
}

# Main loop
while ($true) {
    Clear-Host
    Show-Menu
    
    $choice = Read-Host "Enter choice"
    
    switch ($choice) {
        "1" { Exercise-1 }
        "2" { Exercise-2 }
        "3" { Exercise-3 }
        "4" { Exercise-4 }
        "5" { Exercise-5 }
        "6" { Exercise-6 }
        "7" { Exercise-7 }
        "8" { Exercise-8 }
        "0" { 
            Write-Host "Goodbye! 👋" -ForegroundColor Cyan
            exit 0
        }
        default {
            Write-Host "Invalid choice!" -ForegroundColor Red
            Start-Sleep -Seconds 2
        }
    }
}
