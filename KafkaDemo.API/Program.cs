using KafkaDemo.Core.Interfaces;
using KafkaDemo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IKafkaProducer>(new KafkaProducer("localhost:9092"));
// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddSignalR();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register background service for comsume
builder.Services.AddHostedService<KafkaSignalRConsumerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHub<ChatHub>("/chathub");
app.Run();
