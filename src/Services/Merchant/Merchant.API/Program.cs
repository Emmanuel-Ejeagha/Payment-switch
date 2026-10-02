using Asp.Versioning.ApiExplorer;
using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Configuration;
using BuildingBlocks.Shared.Data;
using BuildingBlocks.Shared.HealthChecks;
using BuildingBlocks.Shared.Middleware;
using BuildingBlocks.Shared.RateLimiting;
using BuildingBlocks.Shared.Versioning;
using BuildingBlocks.Shared.Caching;
using BuildingBlocks.Shared.Auth;
using BuildingBlocks.Shared.Http;
using Merchant.API.Middlewares;
using Merchant.API.Services;
using Merchant.Application;
using Merchant.Infrastructure;
using Merchant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.ValidateSecuritySecrets("MerchantDb");
builder.Configuration.ValidateWebhookSecretEncryptionKey();

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));
var otel = builder.AddPaymentSwitchObservability("Merchant");
otel.WithMetrics(metrics => metrics.AddPrometheusExporter());

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    // gRPC (webhook-secret delivery) is ServiceOnly-gated and reachable only on
    // the internal docker/k8s network (port 5001 is not published). Per
    // TASK-004, that is the documented in-network exception to end-to-end TLS.
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});
builder.Services.AddGrpc();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// TASK-046: Swagger/OpenAPI docs are dev/test-only. In Production the API
// surface is nginx-only and discovery endpoints must not be exposed (see
// docs/prod-exposure.md).
if (!builder.Environment.IsProduction())
{
    builder.Services.AddSwaggerGen(c =>
    {
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        c.AddServer(new OpenApiServer { Url = "/merchant" });
        c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Merchant API", Version = "v1" });
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
}

builder.Services.AddPaymentSwitchJwtBearer(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.ServiceOnly, policy =>
        policy.RequireClaim(ServiceTokenOptions.ClientTypeClaim, ServiceTokenOptions.ClientTypeService));
});

builder.Services.AddPaymentSwitchCors(builder.Configuration);

builder.Services.AddMerchantApplication();
builder.Services.AddMerchantInfrastructure(builder.Configuration);

builder.Services.AddCorrelationId();
builder.Services.AddPaymentSwitchRateLimiting();
builder.Services.AddPaymentSwitchVersioning();
builder.Services.AddPaymentSwitchOutputCache();

builder.Services.AddPaymentSwitchHealthChecks()
    .AddDbContextCheck<AppDbContext>("db", tags: ["ready"])
    .AddRabbitMqHealthCheck(builder.Configuration);

var app = builder.Build();

app.MigrateDatabase<AppDbContext>();

using (var scope = app.Services.CreateScope())
{
    var backfill = scope.ServiceProvider.GetRequiredService<Merchant.Infrastructure.Security.WebhookSecretEncryptionBackfill>();
    await backfill.ExecuteAsync();
}

app.UsePaymentSwitchSecurityHeaders();
app.UsePaymentSwitchForwardedHeaders(builder.Configuration);
app.UseCorrelationId();
app.UseRequestSizeLimit();
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();

if (!app.Environment.IsProduction())
{
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
}

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UsePaymentSwitchOutputCache();
app.MapControllers();
app.MapPrometheusScrapingEndpoint();
app.MapGrpcService<MerchantGrpcService>();
app.MapPaymentSwitchHealthEndpoints();

app.Run();