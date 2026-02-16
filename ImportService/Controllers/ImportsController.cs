using ImportService.Services.ImportJobs;
using Microsoft.AspNetCore.Mvc;

namespace ImportService.Controllers;

[ApiController]
[Route("imports")]
public class ImportsController : ControllerBase
{
    private readonly IImportJobService _jobs;

    public ImportsController(IImportJobService jobs) => _jobs = jobs;

    [HttpPost("csv")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadCsv(
        [FromForm] IFormFile file,
        [FromForm] string userId,
        [FromForm] string? accountId,
        CancellationToken ct)
    {
        var jobId = await _jobs.CreateCsvImportJobAsync(userId, accountId, file, ct);
        return Accepted(new { jobId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var job = await _jobs.GetAsync(id, ct);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string userId, CancellationToken ct)
        => Ok(await _jobs.ListAsync(userId, ct));
}