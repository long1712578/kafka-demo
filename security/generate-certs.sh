#!/bin/bash

# Script to generate SSL certificates for Kafka

CERTS_DIR="./security/certs"
VALIDITY_DAYS=365

echo "🔐 Generating SSL certificates for Kafka..."

# Create certs directory
mkdir -p $CERTS_DIR

# Generate CA key and certificate
echo "1️⃣ Creating Certificate Authority (CA)..."
openssl req -new -x509 -keyout $CERTS_DIR/ca-key -out $CERTS_DIR/ca-cert -days $VALIDITY_DAYS \
  -subj "/C=VN/ST=HCM/L=HCM/O=KafkaDemo/CN=ca" \
  -passout pass:kafkademo123

# Generate Kafka broker keystore
echo "2️⃣ Creating Kafka broker keystore..."
keytool -genkey -keystore $CERTS_DIR/kafka.server.keystore.jks \
  -alias kafka-broker -validity $VALIDITY_DAYS -keyalg RSA \
  -dname "CN=kafka,OU=KafkaDemo,O=KafkaDemo,L=HCM,S=HCM,C=VN" \
  -storepass kafkademo123 -keypass kafkademo123

# Create a certificate request
echo "3️⃣ Creating certificate signing request..."
keytool -keystore $CERTS_DIR/kafka.server.keystore.jks \
  -alias kafka-broker -certreq -file $CERTS_DIR/cert-request \
  -storepass kafkademo123 -keypass kafkademo123

# Sign the certificate with CA
echo "4️⃣ Signing certificate with CA..."
openssl x509 -req -CA $CERTS_DIR/ca-cert -CAkey $CERTS_DIR/ca-key \
  -in $CERTS_DIR/cert-request -out $CERTS_DIR/cert-signed \
  -days $VALIDITY_DAYS -CAcreateserial -passin pass:kafkademo123

# Import CA cert into keystore
echo "5️⃣ Importing CA certificate..."
keytool -keystore $CERTS_DIR/kafka.server.keystore.jks \
  -alias CARoot -import -file $CERTS_DIR/ca-cert \
  -storepass kafkademo123 -noprompt

# Import signed cert into keystore
echo "6️⃣ Importing signed certificate..."
keytool -keystore $CERTS_DIR/kafka.server.keystore.jks \
  -alias kafka-broker -import -file $CERTS_DIR/cert-signed \
  -storepass kafkademo123 -noprompt

# Create truststore
echo "7️⃣ Creating truststore..."
keytool -keystore $CERTS_DIR/kafka.server.truststore.jks \
  -alias CARoot -import -file $CERTS_DIR/ca-cert \
  -storepass kafkademo123 -noprompt

echo ""
echo "✅ SSL certificates generated successfully!"
echo "📁 Certificates location: $CERTS_DIR"
echo ""
echo "🔑 Passwords (save these securely):"
echo "  - Keystore password: kafkademo123"
echo "  - Truststore password: kafkademo123"
echo "  - CA password: kafkademo123"
