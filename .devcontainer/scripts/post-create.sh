#!/bin/bash
set -e

echo "🚀 Setting up KafkaDemo development environment..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Navigate to workspace
cd /workspace

# Restore .NET packages
print_status "Restoring .NET packages..."
dotnet restore KafkaDemo.sln
print_success "NuGet packages restored!"

# Build solution to verify everything works
print_status "Building solution..."
dotnet build KafkaDemo.sln --no-restore
print_success "Solution built successfully!"

# Install dotnet tools
print_status "Installing .NET tools..."
dotnet tool install --global dotnet-ef 2>/dev/null || true
dotnet tool install --global dotnet-outdated-tool 2>/dev/null || true
print_success ".NET tools installed!"

# Create necessary directories
print_status "Creating log directories..."
mkdir -p /workspace/logs

# Set up git configuration
print_status "Configuring Git..."
git config --global core.autocrlf input
git config --global core.eol lf

# Display environment info
echo ""
echo "=============================================="
echo -e "${GREEN}🎉 Development Environment Ready!${NC}"
echo "=============================================="
echo ""
echo "📦 .NET SDK: $(dotnet --version)"
echo "🐳 Docker: $(docker --version 2>/dev/null || echo 'Available via docker-in-docker')"
echo ""
echo "🔗 Available Services:"
echo "   - Kafka Broker: kafka:29092 (internal) / localhost:9092 (external)"
echo "   - Kafka UI: http://localhost:8080"
echo ""
echo "📚 Quick Commands:"
echo "   - Build: dotnet build"
echo "   - Run API: dotnet run --project KafkaDemo.API"
echo "   - Run Consumer: dotnet run --project KafkaDemo.Consumer"
echo ""
echo "=============================================="
