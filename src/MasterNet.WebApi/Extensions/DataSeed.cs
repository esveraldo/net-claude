using MasterNet.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MasterNet.WebApi.Extensions;

public static class DataSeed
{
    // Applies pending EF Core migrations automatically. Restricted to Development:
    // running migrations on app startup in Staging/Production is risky and should
    // instead go through `dotnet ef database update` as a controlled deployment step.
    public static async Task ApplyMigrationsAsync(
        this IApplicationBuilder app
    )
    {
        if (!app.ApplicationServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            return;
        }

        using var scope = app.ApplicationServices.CreateScope();
        var service = scope.ServiceProvider;
        var loggerFactory = service.GetRequiredService<ILoggerFactory>();

        try
        {
            var context = service.GetRequiredService<MasterNetDbContext>();
            await context.Database.MigrateAsync();
        }
        catch (Exception e)
        {
            var logger = loggerFactory.CreateLogger<MasterNetDbContext>();
            logger.LogError(e, "Error applying pending migrations.");
        }
    }
}
