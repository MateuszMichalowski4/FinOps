using System;
using Confluent.Kafka;
using FinOps.Contracts;
using System.Text.Json;
using ImportService.Data;
using ImportService.Workers;
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

builder.Services.AddHostedService<CategoryAssignedConsumer>();


var app = builder.Build();

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

app.MapPost("/debug/publish", async (ImportDbContext db) =>
{
    var txId = Guid.NewGuid();

    var tx = new TransactionEntity
    {
        Id = txId,
        UserId = "user-1",
        Amount = 42.50m,
        Currency = "PLN",
        Merchant = "Zabka",
        BookedAt = DateTimeOffset.UtcNow,
        CreatedAt = DateTimeOffset.UtcNow
    };

    db.Transactions.Add(tx);
    await db.SaveChangesAsync();

    var evt = new TransactionImported(
        EventId: Guid.NewGuid(),
        OccurredAt: DateTimeOffset.UtcNow,
        TransactionId: txId,
        UserId: tx.UserId,
        Amount: tx.Amount,
        Currency: tx.Currency,
        Merchant: tx.Merchant,
        BookedAt: tx.BookedAt
    );

    var json = JsonSerializer.Serialize(evt);

    await producer.ProduceAsync(
        "finops.transaction.imported",
        new Message<string, string> { Key = evt.UserId, Value = json });

    return Results.Ok(evt);
});


app.Run();