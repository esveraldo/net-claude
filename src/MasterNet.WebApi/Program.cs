using MasterNet.Application;
using MasterNet.Application.Interfaces;
using MasterNet.Domain.Courses;
using MasterNet.Infrastructure;
using MasterNet.Infrastructure.Photos;
using MasterNet.Infrastructure.Reports;
using MasterNet.Persistence;
using MasterNet.WebApi.Extensions;
using MasterNet.WebApi.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Este repositorio NO usa .NET User Secrets. La configuración local privada
// (TokenKey, ConnectionStrings:SqlServerDatabase, etc.) vive en appsettings.Local.json
// (gitignored, solo Development). Orden explícito de mayor a menor precedencia:
// variables de entorno > appsettings.Local.json (solo Development) > appsettings.{Environment}.json > appsettings.json.
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

builder.Configuration
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddHealthChecks();

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(); // Registrar AddInfrastructure
builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddPoliciesServices();

builder.Services
    .Configure<CloudinarySettings>(builder.Configuration.GetSection(nameof(CloudinarySettings)));

builder.Services.AddScoped<IPhotoService, PhotoService>();

//builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped(typeof(IReportService<>), typeof(ReportService<>));

builder.Services.AddHttpContextAccessor();
//builder.Services.AddTransient(s => s.GetService<HttpContext>()!.User);

builder.Services.AddProblemDetails();


IEdmModel GetEdmModel()
{
    var edm = new ODataConventionModelBuilder();
    edm.EntitySet<Course>("courses");
    return edm.GetEdmModel();
}

builder.Services.AddControllers()
                   .AddOData(opt => opt
                    .Select()
                    .Filter()
                    .OrderBy()
                    .Expand()
                    .Count()
                    .SetMaxTop(100)
                    .AddRouteComponents("odata", GetEdmModel())
                );


builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Ingresa el token JWT"
        };
        return Task.CompletedTask;
    });

    // Marca en el documento OpenAPI qué operaciones requieren el esquema "Bearer",
    // para que Scalar muestre el candado solo en los endpoints protegidos por [Authorize].
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;
        var requiresAuth = endpointMetadata.OfType<IAuthorizeData>().Any()
            && !endpointMetadata.OfType<IAllowAnonymous>().Any();

        if (requiresAuth)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });
        }

        return Task.CompletedTask;
    });
});

// Orígenes permitidos vienen de configuración (Cors:AllowedOrigins).
// En Development, si no se configuran, se permite cualquier origen para simplificar el desarrollo local.
// En Staging/Production se exige la lista explícita: fallar rápido en vez de abrir CORS a cualquier origen.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if ((allowedOrigins is null || allowedOrigins.Length == 0) && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "Falta la configuración 'Cors:AllowedOrigins' para este entorno. " +
        "Define al menos un origen permitido en appsettings o en la configuración del entorno.");
}

builder.Services.AddCors(o => o.AddPolicy("corsapp", policy =>
{
    if (allowedOrigins is { Length: > 0 })
    {
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
    }
    else
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    }
}));

var app = builder.Build();

// Aplicar migraciones pendientes (solo Development) antes de aceptar tráfico.
await app.ApplyMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("MasterNet API")
            .AddPreferredSecuritySchemes("Bearer");
    });
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

// CORS debe ir antes de autenticación/autorización: si no, las respuestas 401/403
// y las peticiones preflight quedan sin las cabeceras Access-Control-*.
app.UseCors("corsapp");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.Run();

// Expuesto para Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory<Program>)
// en tests/MasterNet.WebApi.IntegrationTests.
public partial class Program { }
