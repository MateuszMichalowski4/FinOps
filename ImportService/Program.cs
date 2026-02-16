using ImportService.Data;
using ImportService.Handlers;
using ImportService.Kafka;
using ImportService.Services;
using ImportService.Services.ImportJobs;
using ImportService.Services.Storage;
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

// Import Jobs
builder.Services.AddScoped<IImportJobService, ImportJobService>();
builder.Services.AddSingleton<IImportFileStorage, LocalImportFileStorage>();
builder.Services.AddHostedService<ImportJobWorker>();

// Kafka consumers (background workers)
builder.Services.AddHostedService<CategoryAssignedConsumer>();

builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddScoped<CategoryAssignedHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ImportDbContext>();
    db.Database.Migrate();
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();