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
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using Payment.API.Middlewares;
using Payment.Application;
using Payment.Infrastructure;
using Payment.Infrastructure.Persistence;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.ValidateSecuritySecrets("PaymentDb");

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));
var otel = builder.AddPaymentSwitchObservability("Payment");
otel.WithMetrics(metrics => metrics.AddPrometheusExporter());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSwaggerGen(c =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.AddServer(new OpenApiServer { Url = "/payment" });
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payment API", Version = "v1" });
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

builder.Services.AddPaymentApplication();
builder.Services.AddPaymentInfrastructure(builder.Configuration);

builder.Services.AddCorrelationId();
builder.Services.AddMemoryCache();
builder.Services.AddPaymentSwitchRateLimiting();
builder.Services.AddPaymentSwitchVersioning();
builder.Services.AddPaymentSwitchOutputCache();

builder.Services.AddPaymentSwitchHealthChecks()
    .AddDbContextCheck<AppDbContext>("db", tags: ["ready"])
    .AddRabbitMqHealthCheck(builder.Configuration);

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
app.UseMiddleware<SecretKeyAuthMiddleware>();
app.UsePaymentSwitchOutputCache();
app.MapControllers();
app.MapPrometheusScrapingEndpoint();
app.MapPaymentSwitchHealthEndpoints();

app.Run();