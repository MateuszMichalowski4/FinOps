using CategorizationService;
using CategorizationService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);


builder.Services.AddDbContext<CategorizationDbContext>(opt =>
    opt.UseNpgsql(
       builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CategorizationDbContext>();
    db.Database.Migrate();
}

host.Run();