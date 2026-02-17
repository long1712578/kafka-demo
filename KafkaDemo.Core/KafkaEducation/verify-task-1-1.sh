#!/bin/bash

###############################################################################
# Task 1.1: Kafka Topic Provisioning Verification Script
# Usage: bash ./KafkaDemo.Core/KafkaEducation/verify-task-1-1.sh
###############################################################################

echo "============================================"
echo "🔍 Task 1.1 Verification"
echo "============================================"
echo ""

API_URL="http://localhost:5224"
KAFKA_BOOTSTRAP="localhost:9092"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "📋 Environment Check:"
echo "  API URL: $API_URL"
echo "  Kafka Bootstrap: $KAFKA_BOOTSTRAP"
echo ""

# Step 1: Check API connectivity
echo "1️⃣  Checking API connectivity..."
if curl -s "$API_URL/health" > /dev/null; then
    echo -e "   ${GREEN}✓ API is running${NC}"
else
    echo -e "   ${RED}✗ API is not reachable${NC}"
    exit 1
fi
echo ""

# Step 2: List topics
echo "2️⃣  Listing existing topics..."
TOPICS=$(curl -s "$API_URL/api/kafka/topics" | jq -r '.topics[]' 2>/dev/null)
if [ -z "$TOPICS" ]; then
    echo -e "   ${YELLOW}⚠ No topics found${NC}"
    echo "   Attempting to create Module A topics..."
    
    INIT_RESULT=$(curl -s -X POST "$API_URL/api/kafka/init-module-a-topics")
    echo "   $INIT_RESULT" | jq '.' 2>/dev/null || echo "   $INIT_RESULT"
else
    echo -e "   ${GREEN}✓ Found topics:${NC}"
    echo "$TOPICS" | while read topic; do
        echo "     - $topic"
    done
fi
echo ""

# Step 3: Get metadata for each topic
echo "3️⃣  Topic Metadata:"
TOPICS=$(curl -s "$API_URL/api/kafka/topics" | jq -r '.topics[]' 2>/dev/null)
for topic in $TOPICS; do
    METADATA=$(curl -s "$API_URL/api/kafka/topics/$topic/metadata" 2>/dev/null)
    PARTITIONS=$(echo $METADATA | jq '.partitionCount' 2>/dev/null)
    
    if [ "$PARTITIONS" != "null" ] && [ ! -z "$PARTITIONS" ]; then
        echo -e "   ${GREEN}✓ $topic:${NC} $PARTITIONS partitions"
    else
        echo -e "   ${RED}✗ $topic:${NC} Could not get metadata"
    fi
done
echo ""

# Step 4: Verify expected topics
echo "4️⃣  Expected Topics Check:"
EXPECTED_TOPICS=("user-events" "orders" "payments" "notifications" "order-processing.DLQ")
TOPICS=$(curl -s "$API_URL/api/kafka/topics" | jq -r '.topics[]' 2>/dev/null)

for expected in "${EXPECTED_TOPICS[@]}"; do
    if echo "$TOPICS" | grep -q "^$expected$"; then
        echo -e "   ${GREEN}✓ $expected${NC}"
    else
        echo -e "   ${RED}✗ $expected${NC} - NOT FOUND"
    fi
done
echo ""

# Step 5: Detailed partition info
echo "5️⃣  Detailed Partition Information:"
TOPICS=$(curl -s "$API_URL/api/kafka/topics" | jq -r '.topics[]' 2>/dev/null)
for topic in $TOPICS; do
    echo -e "   ${YELLOW}Topic: $topic${NC}"
    METADATA=$(curl -s "$API_URL/api/kafka/topics/$topic/metadata" 2>/dev/null)
    echo "$METADATA" | jq '.partitions[] | "     Partition \(.partitionId): Leader=\(.leader), Replicas=\(.replicas), ISR=\(.inSyncReplicas)"' -r 2>/dev/null || echo "     Unable to get partition details"
done
echo ""

echo "============================================"
echo "✅ Task 1.1 Verification Complete"
echo "============================================"
echo ""
echo "📚 Next Steps:"
echo "  1. Review Module A learning materials"
echo "  2. Proceed to Task 1.2 (Producer with Key)"
echo "  3. Proceed to Task 1.3 (Consumer with Partition Logging)"
echo ""
