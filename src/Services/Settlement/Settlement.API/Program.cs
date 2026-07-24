using Asp.Versioning.ApiExplorer;
using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Data;
using BuildingBlocks.Shared.HealthChecks;
using BuildingBlocks.Shared.Middleware;
using BuildingBlocks.Shared.RateLimiting;
using BuildingBlocks.Shared.Versioning;
using BuildingBlocks.Shared.Caching;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using Serilog;
using Settlement.API.Middlewares;
using Settlement.Application;
using Settlement.Application.Features.Command.TriggerSettlement;
using Settlement.Infrastructure;
using Settlement.Infrastructure.Persistence;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSettlementApplication();
builder.Services.AddSettlementInfrastructure(builder.Configuration);

builder.Services.AddCorrelationId();
builder.Services.AddPaymentSwitchRateLimiting();
builder.Services.AddPaymentSwitchVersioning();
builder.Services.AddPaymentSwitchOutputCache();

builder.Services.AddPaymentSwitchHealthChecks()
    .AddDbContextCheck<AppDbContext>("db", tags: ["ready"])
    .AddCheck("rabbitmq", () =>
    {
        try
        {
            using var tcp = new System.Net.Sockets.TcpClient();
            tcp.Connect(builder.Configuration["RabbitMQ:HostName"] ?? "localhost", 5672);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ unreachable", ex);
        }
    }, tags: ["ready"]);

builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("SettlementDb")));
});
builder.Services.AddHangfireServer();

var app = builder.Build();

app.MigrateDatabase<AppDbContext>();

app.UsePaymentSwitchSecurityHeaders();
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

app.UseHangfireDashboard();

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