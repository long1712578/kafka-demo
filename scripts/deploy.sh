#!/bin/bash

# Multi-Environment Deployment Script

ENV=${1:-dev}  # Default to dev if no argument

echo "🚀 Deploying to $ENV environment..."

case $ENV in
  dev|development)
    echo "📦 Starting Development environment..."
    docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d
    echo "✅ Development environment running on port 5000"
    ;;
    
  staging)
    echo "📦 Starting Staging environment..."
    docker compose -f docker-compose.yml -f docker-compose.staging.yml up -d
    echo "✅ Staging environment running on port 5001"
    ;;
    
  prod|production)
    echo "📦 Starting Production environment..."
    docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
    echo "✅ Production environment running on port 5002"
    ;;
    
  *)
    echo "❌ Unknown environment: $ENV"
    echo "Usage: ./deploy.sh [dev|staging|prod]"
    exit 1
    ;;
esac

echo ""
echo "🔍 Container status:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
