using ImportService.Data;
using ImportService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImportService.Handlers;

public class CategoryAssignedHandler
{
    private readonly ImportDbContext _db;
    private readonly ILogger<CategoryAssignedHandler> _logger;

    public CategoryAssignedHandler(ImportDbContext db, ILogger<CategoryAssignedHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(FinOps.Contracts.CategoryAssigned evt, CancellationToken ct)
    {
        var already = await _db.ProcessedEvents.AnyAsync(x => x.EventId == evt.EventId, ct);
        if (already) return;

        var tx = await _db.Transactions.FirstOrDefaultAsync(x => x.Id == evt.TransactionId, ct);
        if (tx is null)
        {
            throw new InvalidOperationException($"Transaction not found: {evt.TransactionId}");
        }

        tx.CategoryId = evt.CategoryId;
        tx.CategoryConfidence = evt.Confidence;

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = evt.EventId, ProcessedAt = DateTimeOffset.UtcNow });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Category applied. tx={TransactionId} cat={CategoryId}", evt.TransactionId, evt.CategoryId);
    }
}