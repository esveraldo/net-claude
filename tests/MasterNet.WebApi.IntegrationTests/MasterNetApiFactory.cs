using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;

namespace MasterNet.WebApi.IntegrationTests;

// Levanta la API completa (Program.cs real, tal cual arranca en producción) contra un
// SQL Server efímero (Testcontainers). El entorno se fija en Development para que
// ApplyMigrationsAsync (WebApi/Extensions/DataSeed.cs) migre y siembre la base
// automáticamente al primer request, igual que en desarrollo local.
public class MasterNetApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServerDatabase"] = _dbContainer.GetConnectionString(),
                // Clave de firma JWT únicamente para que IdentityServiceExtensions no
                // lance al arrancar; los tests de Student no emiten ni validan tokens.
                ["TokenKey"] = "integration-tests-super-secret-signing-key-0123456789"
            });
        });
    }
}
