# 🚀 KafkaDemo CI/CD & Monitoring Guide

## 📋 Table of Contents
1. [GitHub Webhook Setup](#github-webhook)
2. [Monitoring (Prometheus + Grafana)](#monitoring)
3. [Multi-Environment Deployment](#multi-environment)
4. [Security](#security)

---

## 🔔 A. GitHub Webhook

### Setup Steps:

1. **Get your webhook URL:**
   ```bash
   chmod +x .devcontainer/scripts/setup-webhook.sh
   ./.devcontainer/scripts/setup-webhook.sh
   ```

2. **Configure GitHub webhook:**
   - Go to: https://github.com/long1712578/kafka-demo/settings/hooks
   - Click "Add webhook"
   - Payload URL: (from step 1)
   - Content type: `application/json`
   - Events: `Just the push event`
   - Active: ✅
   - Click "Add webhook"

3. **Test:**
   ```bash
   git commit --allow-empty -m "test: trigger webhook"
   git push
   ```
   
   Jenkins will automatically start building!

---

## 📊 B. Monitoring (Prometheus + Grafana)

### Start Monitoring Stack:

```bash
docker compose up -d prometheus grafana node-exporter
```

### Access Dashboards:

| Service | Port | URL | Credentials |
|---------|------|-----|-------------|
| **Prometheus** | 9090 | http://localhost:9090 | None |
| **Grafana** | 3000 | http://localhost:3000 | admin / admin123 |
| **Node Exporter** | 9100 | http://localhost:9100/metrics | None |

### Grafana Setup:

1. Login to Grafana (admin / admin123)
2. Data source **Prometheus** is auto-configured
3. Import dashboards:
   - Node Exporter: Dashboard ID `1860`
   - Docker: Dashboard ID `893`
   - Custom Kafka metrics: Create your own!

### Key Metrics to Monitor:

- **CPU Usage**: `rate(process_cpu_seconds_total[5m])`
- **Memory**: `process_resident_memory_bytes`
- **Container Status**: `container_up`
- **HTTP Requests**: `http_requests_total`

---

## 🌍 C. Multi-Environment Deployment

### Available Environments:

| Environment | Port | Command |
|-------------|------|---------|
| **Development** | 5000 | `./scripts/deploy.sh dev` |
| **Staging** | 5001 | `./scripts/deploy.sh staging` |
| **Production** | 5002 | `./scripts/deploy.sh prod` |

### Deploy to Specific Environment:

```bash
# Development
chmod +x scripts/deploy.sh
./scripts/deploy.sh dev

# Staging
./scripts/deploy.sh staging

# Production
./scripts/deploy.sh prod
```

### Environment Differences:

**Development:**
- Debug logging
- Hot reload enabled
- Single replica

**Staging:**
- Info logging
- 2 replicas
- Resource limits

**Production:**
- Warning logging only
- 3 replicas
- Healthchecks
- Resource limits & reservations

---

## 🔐 D. Security

### SSL Certificates for Kafka:

1. **Generate certificates:**
   ```bash
   chmod +x security/generate-certs.sh
   ./security/generate-certs.sh
   ```

2. **Configure Kafka with SSL:**
   Update `docker-compose.yml`:
   ```yaml
   kafka:
     environment:
       KAFKA_SSL_KEYSTORE_LOCATION: /etc/kafka/secrets/kafka.server.keystore.jks
       KAFKA_SSL_KEYSTORE_PASSWORD: kafkademo123
       KAFKA_SSL_TRUSTSTORE_LOCATION: /etc/kafka/secrets/kafka.server.truststore.jks
       KAFKA_SSL_TRUSTSTORE_PASSWORD: kafkademo123
     volumes:
       - ./security/certs:/etc/kafka/secrets
   ```

### Secrets Management:

1. **Create .env file:**
   ```bash
   cp .env.example .env
   ```

2. **Update passwords:**
   Edit `.env` and change all passwords

3. **Never commit .env:**
   Already in `.gitignore`

### Jenkins Authentication:

1. **Enable security in Jenkins:**
   - Manage Jenkins → Security
   - Enable "Jenkins' own user database"
   - Allow users to sign up: ❌

2. **Create admin user:**
   - Username: admin
   - Password: (strong password)

### Best Practices:

✅ Use strong passwords  
✅ Enable SSL/TLS for production  
✅ Rotate secrets regularly  
✅ Use environment variables, not hardcoded values  
✅ Implement RBAC (Role-Based Access Control)  
✅ Enable audit logging  

---

## 🔍 Troubleshooting

### Webhook doesn't trigger:

- Check webhook delivery in GitHub settings
- Ensure Jenkins port is publicly accessible
- Check Jenkins logs: `docker logs jenkins`

### Prometheus not scraping:

- Verify targets in Prometheus UI: http://localhost:9090/targets
- Check network connectivity
- Verify prometheus.yml configuration

### Container won't start:

```bash
# Check logs
docker logs <container-name>

# Check resources
docker stats

# Restart
docker compose restart <service-name>
```

---

## 📚 Additional Resources

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Jenkins Pipeline](https://www.jenkins.io/doc/book/pipeline/)
- [Kafka Security](https://kafka.apache.org/documentation/#security)

---

## 🎯 Next Steps

1. ✅ Setup GitHub webhook for auto-build
2. ✅ Configure Grafana dashboards
3. ✅ Test multi-environment deployments
4. ✅ Generate and apply SSL certificates
5. 📝 Create custom metrics for your API
6. 📊 Setup alerting in Grafana
7. 🔐 Implement authentication in API
8. 📦 Add integration tests to pipeline

Happy coding! 🚀
