using ImportService.Dtos;
using ImportService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImportService.Controllers;

[ApiController]
[Route("transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionsService _svc;

    public TransactionsController(ITransactionsService svc) => _svc = svc;

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] List<TransactionImportRequest> request, CancellationToken ct)
    {
        await _svc.ImportAsync(request, ct);
        return Accepted();
    }

    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] string userId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
        => Ok(await _svc.QueryAsync(userId, from, to, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var tx = await _svc.GetAsync(id, ct);
        return tx is null ? NotFound() : Ok(tx);
    }
}