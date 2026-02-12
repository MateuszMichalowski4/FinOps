using CategorizationService;
using CategorizationService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<CategorizationDbContext>(opt =>
    opt.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=finops;Username=finops;Password=finops"));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();