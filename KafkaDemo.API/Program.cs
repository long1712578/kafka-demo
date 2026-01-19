using KafkaDemo.Core.Interfaces;
using KafkaDemo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Get Kafka bootstrap servers from configuration
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddSingleton<IKafkaProducer>(new KafkaProducer(kafkaBootstrapServers));

// Add health checks
builder.Services.AddHealthChecks();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true));
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register background service for comsume
builder.Services.AddHostedService<KafkaSignalRConsumerService>();

var app = builder.Build();

// Enable Swagger in all environments for API documentation
app.UseSwagger();
app.UseSwaggerUI();

// Health check endpoint
app.MapHealthChecks("/health");

app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

app.MapHub<ChatHub>("/chathub"); 

app.Run();

