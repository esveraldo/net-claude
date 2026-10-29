using MasterNet.Domain.Abstractions;
using MasterNet.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Infrastructure;


namespace MasterNet.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddDbContext<MasterNetDbContext>(opt =>
        {
            opt.LogTo(Console.WriteLine, new[]
            {
                DbLoggerCategory.Database.Command.Name
            }, LogLevel.Information)
               .EnableSensitiveDataLogging();

            //opt.UseSqlite(configuration.GetConnectionString("SqliteDatabase"))
            opt.UseSqlServer(configuration.GetConnectionString("SqlServerDatabase"))
               .UseAsyncSeeding(async (context, serviceProvider, cancellationToken) =>
               {
                   var dbContext = (MasterNetDbContext)context;
                   var logger = context.GetService<ILogger<MasterNetDbContext>>();

                   try
                   {
                       await SeedDatabase.SeedRolesAndUsersAsync(context, logger, cancellationToken);
                       await SeedDatabase.SeedPricesAsync(dbContext, logger, cancellationToken);
                       await SeedDatabase.SeedInstructorsAsync(dbContext, logger, cancellationToken);
                       await SeedDatabase.SeedCoursesAsync(dbContext, logger, cancellationToken);
                       await SeedDatabase.SeedStudentsAsync(dbContext, logger, cancellationToken);
                       await SeedDatabase.SeedRatingsAsync(dbContext, logger, cancellationToken);
                       await SeedDatabase.SeedDevicesAsync(dbContext, logger, cancellationToken);
                   }
                   catch (Exception ex)
                   {
                       logger?.LogError(ex, "Error while seeding data");
                   }
               });
        });

        return services;
    }
}