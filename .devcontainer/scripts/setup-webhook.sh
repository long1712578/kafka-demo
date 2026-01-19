#!/bin/bash

# Script to setup GitHub webhook for Jenkins
echo "🔔 Setting up GitHub Webhook..."

# Get Codespace name
CODESPACE_NAME=$(hostname)
JENKINS_PORT=8081

# Construct webhook URL
WEBHOOK_URL="https://${CODESPACE_NAME}-${JENKINS_PORT}.app.github.dev/github-webhook/"

echo ""
echo "✅ Jenkins Webhook URL:"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "$WEBHOOK_URL"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📋 Next steps:"
echo "1. Go to: https://github.com/long1712578/kafka-demo/settings/hooks"
echo "2. Click 'Add webhook'"
echo "3. Payload URL: $WEBHOOK_URL"
echo "4. Content type: application/json"
echo "5. Events: Just the push event"
echo "6. Click 'Add webhook'"
echo ""
echo "🎯 After setup, every git push will trigger Jenkins build automatically!"
