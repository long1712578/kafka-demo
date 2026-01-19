# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY KafkaDemo.sln ./
COPY KafkaDemo.API/*.csproj ./KafkaDemo.API/
COPY KafkaDemo.Core/*.csproj ./KafkaDemo.Core/
COPY KafkaDemo.Infrastructure/*.csproj ./KafkaDemo.Infrastructure/
COPY KafkaDemo.Shared/*.csproj ./KafkaDemo.Shared/

# Restore dependencies
RUN dotnet restore KafkaDemo.API/KafkaDemo.API.csproj

# Copy all source code
COPY . .

# Build and publish
WORKDIR /src/KafkaDemo.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Kafka__BootstrapServers=kafka:29092

# Expose port
EXPOSE 5000

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:5000/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "KafkaDemo.API.dll"]
