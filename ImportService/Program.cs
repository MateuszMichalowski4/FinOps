using System;
using Confluent.Kafka;
using FinOps.Contracts;
using System.Text.Json;
using ImportService.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ImportDbContext>(opt =>
    opt.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=finops;Username=finops;Password=finops"));

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

var kafkaBootstrap =
    Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:29092";

var producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaBootstrap,
    Acks = Acks.All
};

using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

app.MapPost("/debug/publish", async () =>
    {
        var evt = new TransactionImported(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            TransactionId: Guid.NewGuid(),
            UserId: "user-1",
            Amount: 42.50m,
            Currency: "PLN",
            Merchant: "Zabka",
            BookedAt: DateTimeOffset.UtcNow
        );

        var json = JsonSerializer.Serialize(evt);

        await producer.ProduceAsync(
            "finops.transaction.imported",
            new Message<string, string>
            {
                Key = evt.UserId,
                Value = json
            });

        return Results.Ok(evt);
    })
    .WithName("PublishTransactionImported")
    .WithOpenApi();

app.Run();