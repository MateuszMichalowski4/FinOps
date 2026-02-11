using ImportService.Data;
using ImportService.Handlers;
using ImportService.Kafka;
using ImportService.Services;
using ImportService.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<ImportDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Application services
builder.Services.AddScoped<ITransactionsService, TransactionsService>();

// Kafka consumers (background workers)
builder.Services.AddHostedService<CategoryAssignedConsumer>();

builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddScoped<CategoryAssignedHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();