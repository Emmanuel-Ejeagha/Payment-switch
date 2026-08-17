using Asp.Versioning.ApiExplorer;
using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Auth;
using BuildingBlocks.Shared.Configuration;
using BuildingBlocks.Shared.Data;
using BuildingBlocks.Shared.HealthChecks;
using BuildingBlocks.Shared.Middleware;
using BuildingBlocks.Shared.RateLimiting;
using BuildingBlocks.Shared.Versioning;
using BuildingBlocks.Shared.Caching;
using BuildingBlocks.Shared.Http;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using Serilog;
using Settlement.API.Middlewares;
using Settlement.API.Security;
using Settlement.Application;
using Settlement.Application.Features.Command.TriggerSettlement;
using Settlement.Infrastructure;
using Settlement.Infrastructure.Persistence;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.ValidateSecuritySecrets("SettlementDb");

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));
var otel = builder.AddPaymentSwitchObservability("Settlement");
otel.WithMetrics(metrics => metrics.AddPrometheusExporter());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.AddServer(new OpenApiServer { Url = "/settlement" });
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Settlement API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddPaymentSwitchJwtBearer(builder.Configuration);

builder.Services.AddAuthorization();

builder.Services.AddPaymentSwitchCors(builder.Configuration);

builder.Services.AddSettlementApplication();
builder.Services.AddSettlementInfrastructure(builder.Configuration);

builder.Services.AddCorrelationId();
builder.Services.AddPaymentSwitchRateLimiting();
builder.Services.AddPaymentSwitchVersioning();
builder.Services.AddPaymentSwitchOutputCache();

builder.Services.AddPaymentSwitchHealthChecks()
    .AddDbContextCheck<AppDbContext>("db", tags: ["ready"])
    .AddRabbitMqHealthCheck(builder.Configuration);

builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("SettlementDb")));
});
builder.Services.AddHangfireServer();

var app = builder.Build();

app.MigrateDatabase<AppDbContext>();

app.UsePaymentSwitchSecurityHeaders();
app.UsePaymentSwitchForwardedHeaders(builder.Configuration);
app.UseCorrelationId();
app.UseRequestSizeLimit();
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();

app.UseSwagger(c => c.RouteTemplate = "{documentName}/swagger.json");
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint($"../{description.GroupName}/swagger.json",
            description.GroupName.ToUpperInvariant());
    }
});

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UsePaymentSwitchOutputCache();

app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    // TASK-013: the dashboard is admin-only. UseAuthentication above has already
    // populated the user from the JWT bearer attached by the frontend BFF proxy.
    Authorization = [new AdminDashboardAuthorizationFilter()]
});

using (var serviceScope = app.Services.CreateScope())
{
    var triggerHandler = serviceScope.ServiceProvider.GetRequiredService<TriggerSettlementHandler>();
    RecurringJob.AddOrUpdate("nightly-settlement",
        () => triggerHandler.Handle(new TriggerSettlementCommand(DateTime.UtcNow.Date.AddDays(-1)), CancellationToken.None),
        "0 1 * * *");
}

app.MapControllers();
app.MapPrometheusScrapingEndpoint();
app.MapPaymentSwitchHealthEndpoints();

app.Run();