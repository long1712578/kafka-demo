#!/bin/bash
set -e

echo "🔄 Starting KafkaDemo services..."

# Function to wait for Kafka
wait_for_kafka() {
    echo "⏳ Waiting for Kafka to be ready..."
    local max_attempts=30
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if docker exec kafka kafka-topics.sh --bootstrap-server localhost:29092 --list &>/dev/null; then
            echo "✅ Kafka is ready!"
            return 0
        fi
        echo "   Attempt $attempt/$max_attempts - Kafka not ready yet..."
        sleep 2
        ((attempt++))
    done
    
    echo "❌ Kafka failed to start after $max_attempts attempts"
    return 1
}

# Wait for Kafka to be healthy
wait_for_kafka

# Create default topics if they don't exist
echo "📝 Ensuring default topics exist..."
docker exec kafka kafka-topics.sh --bootstrap-server localhost:29092 --create --if-not-exists --topic demo-topic --partitions 3 --replication-factor 1 2>/dev/null || true
docker exec kafka kafka-topics.sh --bootstrap-server localhost:29092 --create --if-not-exists --topic chat-messages --partitions 3 --replication-factor 1 2>/dev/null || true

# Display helpful info
echo ""
echo "=============================================="
echo "🎯 KafkaDemo Environment Started!"
echo "=============================================="
echo ""
echo "🌐 Access Points:"
echo "   📊 Kafka UI: Click on 'Ports' tab → Port 8080"
echo "   🔌 Kafka Broker: kafka:29092 (from code)"
echo ""
echo "✨ Ready to develop!"
echo "=============================================="
