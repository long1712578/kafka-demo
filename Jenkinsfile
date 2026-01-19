pipeline {
    agent any
    
    environment {
        DOCKER_IMAGE = 'kafkademo-api'
        DOCKER_TAG = "${BUILD_NUMBER}"
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    }
    
    stages {
        stage('Checkout') {
            steps {
                echo '📥 Checking out code...'
                checkout scm
            }
        }
        
        stage('Restore') {
            steps {
                echo '📦 Restoring NuGet packages...'
                sh 'dotnet restore'
            }
        }
        
        stage('Build') {
            steps {
                echo '🔨 Building solution...'
                sh 'dotnet build --configuration Release --no-restore'
            }
        }
        
        stage('Test') {
            steps {
                echo '🧪 Running tests...'
                sh 'dotnet test --configuration Release --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"'
            }
            post {
                always {
                    // Publish test results
                    script {
                        if (fileExists('**/test-results.trx')) {
                            echo '📊 Test results available'
                        }
                    }
                }
            }
        }
        
        stage('Docker Build') {
            steps {
                echo '🐳 Building Docker image...'
                sh """
                    docker build -t ${DOCKER_IMAGE}:${DOCKER_TAG} -f KafkaDemo.API/Dockerfile .
                    docker tag ${DOCKER_IMAGE}:${DOCKER_TAG} ${DOCKER_IMAGE}:latest
                """
            }
        }
        
        stage('Deploy') {
            steps {
                echo '🚀 Deploying to Docker Compose...'
                sh """
                    docker compose stop api || true
                    docker compose rm -f api || true
                    docker compose up -d api
                """
            }
        }
        
        stage('Health Check') {
            steps {
                echo '❤️ Checking API health...'
                script {
                    def maxRetries = 10
                    def retryCount = 0
                    def healthy = false
                    
                    while (retryCount < maxRetries && !healthy) {
                        try {
                            sh 'curl --fail http://localhost:5000/health'
                            healthy = true
                            echo '✅ API is healthy!'
                        } catch (Exception e) {
                            retryCount++
                            echo "⏳ Waiting for API... (${retryCount}/${maxRetries})"
                            sleep(5)
                        }
                    }
                    
                    if (!healthy) {
                        error '❌ API health check failed!'
                    }
                }
            }
        }
    }
    
    post {
        success {
            echo '🎉 Pipeline completed successfully!'
        }
        failure {
            echo '❌ Pipeline failed!'
        }
        cleanup {
            echo '🧹 Cleaning up...'
            sh 'docker image prune -f || true'
        }
    }
}
