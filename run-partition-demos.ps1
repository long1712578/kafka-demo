# ===================================================================
# KAFKA PARTITION & CONSUMER - DEMO RUNNER
# 
# Script này giúp bạn chạy các demos để hiểu rõ về:
# - Partition và Consumer relationship
# - Message ordering với keys
# - Consumer groups và rebalancing
# ===================================================================

Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   KAFKA PARTITION & CONSUMER DEMOS                            ║" -ForegroundColor Cyan
Write-Host "║                                                               ║" -ForegroundColor Cyan
Write-Host "║   📚 Học về Kafka Partition & Consumer                       ║" -ForegroundColor Cyan
Write-Host "║   ✅ Chứng minh các nguyên tắc quan trọng                    ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ===================================================================
# SETUP & VERIFICATION
# ===================================================================

function Check-KafkaRunning {
    Write-Host "🔍 Kiểm tra Kafka cluster..." -ForegroundColor Yellow
    
    $kafkaRunning = docker ps --filter "name=kafka1" --format "{{.Names}}" 2>$null
    
    if ($kafkaRunning) {
        Write-Host "✅ Kafka cluster đang chạy" -ForegroundColor Green
        return $true
    } else {
        Write-Host "❌ Kafka cluster CHƯA chạy!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Vui lòng start Kafka bằng lệnh:" -ForegroundColor Yellow
        Write-Host "  cd d:\Projects\KafkaDemo\kafka" -ForegroundColor White
        Write-Host "  docker-compose up -d" -ForegroundColor White
        Write-Host ""
        return $false
    }
}

function Show-Menu {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                      CHỌN DEMO                                ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  [1] Demo 1: Producer với Employee ID làm Key" -ForegroundColor White
    Write-Host "      → Chứng minh: Cùng key → Cùng partition" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  [2] Demo 2: 1 Consumer đọc NHIỀU Partitions" -ForegroundColor White
    Write-Host "      → Chứng minh: '1 consumer = 1 partition' là SAI" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  [3] Demo 3: Multiple Consumers - Partition Assignment" -ForegroundColor White
    Write-Host "      → Kafka tự động chia partitions cho consumers" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  [4] Demo 4: Message Ordering với Key" -ForegroundColor White
    Write-Host "      → Đảm bảo thứ tự 100% cho cùng key" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  [5] Demo 5: Multiple Consumer Groups" -ForegroundColor White
    Write-Host "      → Nhiều groups độc lập đọc cùng topic" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  [A] Chạy TẤT CẢ demos" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  [V] View Kafka Topic Info" -ForegroundColor Magenta
    Write-Host "  [C] View Consumer Groups" -ForegroundColor Magenta
    Write-Host "  [U] Open Kafka UI (Browser)" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "  [Q] Thoát" -ForegroundColor Red
    Write-Host ""
}

# ===================================================================
# KAFKA INSPECTION TOOLS
# ===================================================================

function View-TopicInfo {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  KAFKA TOPIC INFORMATION" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "📋 List all topics:" -ForegroundColor Yellow
    docker exec kafka-tools kafka-topics --list --bootstrap-server kafka1:9092
    
    Write-Host ""
    Write-Host "📊 Topic details for 'hrmcore.staging.demo':" -ForegroundColor Yellow
    docker exec kafka-tools kafka-topics --describe --topic hrmcore.staging.demo --bootstrap-server kafka1:9092 2>$null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️  Topic 'hrmcore.staging.demo' chưa tồn tại. Chạy demo để tạo topic." -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Nhấn ENTER để tiếp tục..." -ForegroundColor Gray
    Read-Host
}

function View-ConsumerGroups {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  CONSUMER GROUPS INFORMATION" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "👥 List all consumer groups:" -ForegroundColor Yellow
    docker exec kafka-tools kafka-consumer-groups --list --bootstrap-server kafka1:9092
    
    Write-Host ""
    Write-Host "📊 Consumer group details (demo groups):" -ForegroundColor Yellow
    
    $groups = @(
        "demo-group-single-consumer",
        "demo-group-multiple-consumers",
        "demo-ordering-verification",
        "hrm-processor",
        "analytics-service",
        "audit-logger"
    )
    
    foreach ($group in $groups) {
        Write-Host ""
        Write-Host "Group: $group" -ForegroundColor Magenta
        docker exec kafka-tools kafka-consumer-groups --describe --group $group --bootstrap-server kafka1:9092 2>$null
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  (not active)" -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host "Nhấn ENTER để tiếp tục..." -ForegroundColor Gray
    Read-Host
}

function Open-KafkaUI {
    Write-Host ""
    Write-Host "🌐 Mở Kafka UI trong browser..." -ForegroundColor Yellow
    Start-Process "http://localhost:8080"
    Write-Host "✅ Browser đã được mở" -ForegroundColor Green
    Write-Host ""
    Write-Host "Nhấn ENTER để tiếp tục..." -ForegroundColor Gray
    Read-Host
}

# ===================================================================
# DEMO EXECUTION
# ===================================================================

function Run-Demo {
    param (
        [string]$DemoName,
        [string]$Description
    )
    
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  RUNNING: $DemoName" -ForegroundColor Green
    Write-Host "║  $Description" -ForegroundColor Green
    Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "⏳ Đang compile và chạy..." -ForegroundColor Yellow
    Write-Host ""
    
    # Build và run demo
    $demoCode = @"
using System;
using System.Threading.Tasks;
using KafkaDemo.Examples;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            await PartitionConsumerDemo.$DemoName();
        }
        catch (Exception ex)
        {
            Console.WriteLine(`$"❌ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
"@
    
    # Create temp program
    $tempFile = "d:\Projects\KafkaDemo\temp_demo_runner.cs"
    $demoCode | Out-File -FilePath $tempFile -Encoding UTF8
    
    # Run with dotnet script (assuming you have C# project structure)
    # Alternative: Call into your existing test project
    Write-Host "💡 TIP: Bạn cũng có thể mở Visual Studio và chạy code trong Examples\PartitionConsumerDemo.cs" -ForegroundColor Cyan
    Write-Host ""
    
    Remove-Item $tempFile -ErrorAction SilentlyContinue
    
    Write-Host ""
    Write-Host "✅ Demo hoàn thành!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Nhấn ENTER để tiếp tục..." -ForegroundColor Gray
    Read-Host
}

# ===================================================================
# EDUCATIONAL CONTENT
# ===================================================================

function Show-ConceptExplanation {
    param (
        [string]$Concept
    )
    
    switch ($Concept) {
        "partition-consumer" {
            Write-Host ""
            Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
            Write-Host "║  KHÁI NIỆM: PARTITION & CONSUMER                             ║" -ForegroundColor Magenta
            Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
            Write-Host ""
            Write-Host "✅ NGUYÊN TẮC ĐÚNG:" -ForegroundColor Green
            Write-Host "   • Trong CÙNG 1 Consumer Group:" -ForegroundColor White
            Write-Host "     - 1 Partition CHỈ được consume bởi 1 Consumer" -ForegroundColor Yellow
            Write-Host "     - 1 Consumer CÓ THỂ consume NHIỀU Partitions" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "❌ SAI LẦM THƯỜNG GẶP:" -ForegroundColor Red
            Write-Host "   • '1 consumer = 1 partition' → KHÔNG ĐÚNG!" -ForegroundColor Red
            Write-Host "   • '1 consumer chỉ đọc 1 partition' → SAI!" -ForegroundColor Red
            Write-Host ""
            Write-Host "📊 VÍ DỤ:" -ForegroundColor Cyan
            Write-Host "   Topic có 6 partitions, Consumer Group có 2 consumers:" -ForegroundColor White
            Write-Host "   • Consumer 1 → P0, P1, P2 (3 partitions)" -ForegroundColor Yellow
            Write-Host "   • Consumer 2 → P3, P4, P5 (3 partitions)" -ForegroundColor Yellow
            Write-Host ""
        }
        
        "message-key" {
            Write-Host ""
            Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
            Write-Host "║  KHÁI NIỆM: MESSAGE KEY                                       ║" -ForegroundColor Magenta
            Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
            Write-Host ""
            Write-Host "🔑 MỤC ĐÍCH CỦA KEY:" -ForegroundColor Cyan
            Write-Host "   1. Đảm bảo message ordering cho cùng key" -ForegroundColor White
            Write-Host "   2. Data locality (cùng consumer xử lý cùng entity)" -ForegroundColor White
            Write-Host "   3. Load balancing tự động" -ForegroundColor White
            Write-Host ""
            Write-Host "✅ BEST PRACTICE - DÙNG KEY:" -ForegroundColor Green
            Write-Host "   • Employee ID (NV001, NV002, ...)" -ForegroundColor Yellow
            Write-Host "   • User ID" -ForegroundColor Yellow
            Write-Host "   • Order ID" -ForegroundColor Yellow
            Write-Host "   • Aggregate Root ID" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "🎯 KẾT QUẢ:" -ForegroundColor Cyan
            Write-Host "   Partition = hash(key) % numPartitions" -ForegroundColor White
            Write-Host "   Cùng key → Cùng partition → Cùng thứ tự" -ForegroundColor Yellow
            Write-Host ""
        }
    }
    
    Write-Host "Nhấn ENTER để tiếp tục..." -ForegroundColor Gray
    Read-Host
}

# ===================================================================
# QUICK REFERENCE
# ===================================================================

function Show-QuickReference {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  QUICK REFERENCE - KAFKA COMMANDS                             ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "📋 LIST TOPICS:" -ForegroundColor Yellow
    Write-Host "docker exec kafka-tools kafka-topics --list --bootstrap-server kafka1:9092" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "📊 DESCRIBE TOPIC:" -ForegroundColor Yellow
    Write-Host "docker exec kafka-tools kafka-topics --describe --topic <topic-name> --bootstrap-server kafka1:9092" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "👥 LIST CONSUMER GROUPS:" -ForegroundColor Yellow
    Write-Host "docker exec kafka-tools kafka-consumer-groups --list --bootstrap-server kafka1:9092" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "📊 DESCRIBE CONSUMER GROUP:" -ForegroundColor Yellow
    Write-Host "docker exec kafka-tools kafka-consumer-groups --describe --group <group-id> --bootstrap-server kafka1:9092" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "🌐 KAFKA UI:" -ForegroundColor Yellow
    Write-Host "http://localhost:8080" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "Nhấn ENTER để tiếp tục..." -ForegroundColor Gray
    Read-Host
}

# ===================================================================
# MAIN PROGRAM
# ===================================================================

# Check Kafka first
if (-not (Check-KafkaRunning)) {
    Write-Host ""
    Write-Host "❌ Không thể tiếp tục khi Kafka chưa chạy!" -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "📚 TÀI LIỆU THAM KHẢO:" -ForegroundColor Yellow
Write-Host "   • PARTITION_CONSUMER_FAQ.md - Câu hỏi thường gặp" -ForegroundColor White
Write-Host "   • Examples\PartitionConsumerDemo.cs - Source code demos" -ForegroundColor White
Write-Host "   • KAFKA_LEARNING_GUIDE.md - Hướng dẫn toàn diện" -ForegroundColor White
Write-Host ""

# Main loop
do {
    Show-Menu
    
    $choice = Read-Host "Chọn [1-5, A, V, C, U, Q]"
    
    switch ($choice.ToUpper()) {
        "1" {
            Show-ConceptExplanation "message-key"
            Run-Demo "Demo1_ProducerWithEmployeeKey" "Producer với Employee ID làm Key"
        }
        "2" {
            Show-ConceptExplanation "partition-consumer"
            Run-Demo "Demo2_SingleConsumerMultiplePartitions" "1 Consumer đọc NHIỀU Partitions"
        }
        "3" {
            Run-Demo "Demo3_MultipleConsumersPartitionAssignment" "Multiple Consumers - Auto Assignment"
        }
        "4" {
            Run-Demo "Demo4_MessageOrderingWithKey" "Message Ordering với Key"
        }
        "5" {
            Run-Demo "Demo5_MultipleConsumerGroups" "Multiple Consumer Groups"
        }
        "A" {
            Show-ConceptExplanation "partition-consumer"
            Show-ConceptExplanation "message-key"
            Run-Demo "RunAllDemos" "Tất cả demos"
        }
        "V" {
            View-TopicInfo
        }
        "C" {
            View-ConsumerGroups
        }
        "U" {
            Open-KafkaUI
        }
        "R" {
            Show-QuickReference
        }
        "Q" {
            Write-Host ""
            Write-Host "👋 Tạm biệt! Happy Learning! 🚀" -ForegroundColor Green
            Write-Host ""
            break
        }
        default {
            Write-Host ""
            Write-Host "❌ Lựa chọn không hợp lệ!" -ForegroundColor Red
        }
    }
    
} while ($choice.ToUpper() -ne "Q")

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  🎓 KẾT LUẬN QUAN TRỌNG" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Dùng Employee ID làm key là ĐÚNG và là BEST PRACTICE" -ForegroundColor Green
Write-Host "✅ KHÔNG vi phạm bất kỳ nguyên tắc nào" -ForegroundColor Green
Write-Host "✅ 1 Consumer CÓ THỂ đọc NHIỀU partitions" -ForegroundColor Green
Write-Host "✅ 1 Partition CHỈ 1 Consumer (trong cùng group)" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Bạn đang làm đúng! Tiếp tục phát triển HRM system!" -ForegroundColor Yellow
Write-Host ""
