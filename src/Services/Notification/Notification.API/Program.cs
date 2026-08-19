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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Notification.API.Hubs;
using Notification.API.Middlewares;
using Notification.API.RateLimiting;
using Notification.API.Services;
using Notification.Application;
using Notification.Application.Interfaces;
using Notification.Infrastructure;
using Notification.Infrastructure.Persistence;
using OpenTelemetry.Metrics;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.ValidateSecuritySecrets("NotificationDb");

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));
var otel = builder.AddPaymentSwitchObservability("Notification");
otel.WithMetrics(metrics => metrics.AddPrometheusExporter());

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
        c.AddServer(new OpenApiServer { Url = "/notification" });
        c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Notification API", Version = "v1" });
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

builder.Services.AddPaymentSwitchJwtBearer(builder.Configuration, options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddPaymentSwitchCors(builder.Configuration);
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
builder.Services.AddSingleton<RateLimitHubFilter>();
builder.Services.AddSignalR(options =>
{
    // TASK-047: harden against abusive connections — cap inbound frame size,
    // bound parallel client invocations, and never leak exception details.
    options.MaximumReceiveMessageSize = 32 * 1024;
    options.MaximumParallelInvocationsPerClient = 4;
    options.EnableDetailedErrors = false;
})
.AddHubOptions<PaymentNotificationHub>(options =>
{
    options.AddFilter<RateLimitHubFilter>();
});

builder.Services.AddHttpClient<IMerchantGroupResolver, MerchantGroupResolver>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Merchant:BaseUrl"] ?? "http://merchant-api:8080");
});
builder.Services.AddNotificationApplication();
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.Services.AddScoped<IRealTimeNotifier, SignalRRealTimeNotifier>();

builder.Services.AddCorrelationId();
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
app.MapHub<PaymentNotificationHub>("/hubs/payment-notifications");
app.MapPrometheusScrapingEndpoint();
app.MapPaymentSwitchHealthEndpoints();
app.UseStaticFiles();

app.Run();