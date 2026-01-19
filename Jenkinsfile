pipeline {
    agent any
    
    environment {
        DOCKER_IMAGE = 'kafkademo-api'
        DOCKER_TAG = "${BUILD_NUMBER}"
    }
    
    triggers {
        githubPush()  // Trigger on GitHub push events
    }
    
    stages {
        stage('Checkout') {
            steps {
                echo '📥 Checking out code...'
                git branch: 'master', url: 'https://github.com/long1712578/kafka-demo.git'
            }
        }
        
        stage('Build Docker Image') {
            steps {
                echo '🐳 Building Docker image...'
                sh """
                    cd ${WORKSPACE}
                    docker build -t ${DOCKER_IMAGE}:${DOCKER_TAG} -f KafkaDemo.API/Dockerfile .
                    docker tag ${DOCKER_IMAGE}:${DOCKER_TAG} ${DOCKER_IMAGE}:latest
                """
            }
        }
        
        stage('Deploy') {
            steps {
                echo '🚀 Deploying API container...'
                sh """
                    # Stop and remove old API container
                    docker stop kafkademo-api || true
                    docker rm kafkademo-api || true
                    
                    # Run new API container
                    docker run -d \
                        --name kafkademo-api \
                        --network kafka-demo_default \
                        -p 5000:5000 \
                        -e ASPNETCORE_ENVIRONMENT=Production \
                        -e Kafka__BootstrapServers=kafka:29092 \
                        --restart unless-stopped \
                        ${DOCKER_IMAGE}:${DOCKER_TAG}
                """
            }
        }
        
        stage('Verify') {
            steps {
                echo '✅ Verifying deployment...'
                sh """
                    sleep 5
                    docker ps | grep kafkademo-api
                    echo "API deployed successfully with build #${BUILD_NUMBER}"
                """
            }
        }
    }
    
    post {
        success {
            echo '🎉 Build and deploy successful!'
        }
        failure {
            echo '❌ Build failed!'
        }
        cleanup {
            echo '🧹 Cleaning up old images...'
            sh 'docker image prune -f || true'
        }
    }
}
