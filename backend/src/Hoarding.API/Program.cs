using Hoarding.Application;
using Hoarding.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Observability — wire BEFORE everything else so all subsequent
// middleware, layers, and DI activations are instrumented.
//
// All observability is OPT-IN: the SDKs only attach an exporter when the
// relevant endpoint env vars are set. Running without them gives you a
// fully functional API with zero outbound observability traffic — useful
// for local dev, smoke tests, and air-gapped environments.
// ============================================================
const string ServiceName = "hoardly-api";
var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";

var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"]
    ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
var otelEnabled  = !string.IsNullOrWhiteSpace(otlpEndpoint);

var sentryDsn    = builder.Configuration["Sentry:Dsn"]
    ?? Environment.GetEnvironmentVariable("SENTRY_DSN");
var sentryEnabled = !string.IsNullOrWhiteSpace(sentryDsn);

// ----- OpenTelemetry (traces + metrics) -----
// OTLP endpoint, headers, protocol read from standard env vars:
//   OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_EXPORTER_OTLP_HEADERS, OTEL_EXPORTER_OTLP_PROTOCOL
if (otelEnabled)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r
            .AddService(ServiceName, serviceVersion: serviceVersion)
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", builder.Environment.EnvironmentName),
            }))
        .WithTracing(t => t
            .AddAspNetCoreInstrumentation(o =>
            {
                // Don't trace the noisy health endpoint or Swagger.
                o.Filter = ctx =>
                {
                    var path = ctx.Request.Path.Value ?? string.Empty;
                    return !path.StartsWith("/health") && !path.StartsWith("/swagger");
                };
                o.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = true)
            // Npgsql emits to its own ActivitySource — subscribe by name to avoid
            // a namespace collision with Npgsql.EntityFrameworkCore.PostgreSQL's
            // AddNpgsql<TContext>(IServiceCollection, ...) extension.
            .AddSource("Npgsql")
            .AddOtlpExporter())
        .WithMetrics(m => m
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter());
}

// ----- Sentry (errors + breadcrumbs) -----
// Fully opt-in: skip the entire Sentry pipeline if no DSN is configured.
// The Serilog sink below also short-circuits unless sentryEnabled is true.
if (sentryEnabled)
{
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = sentryDsn;
        o.Environment = builder.Environment.EnvironmentName;
        o.Release = $"{ServiceName}@{serviceVersion}";
        o.TracesSampleRate = builder.Environment.IsDevelopment() ? 1.0 : 0.1;
        o.SendDefaultPii = false;
        o.MinimumBreadcrumbLevel = LogLevel.Information;
        o.MinimumEventLevel = LogLevel.Warning;
    });
}

// ----- Logging (Serilog → console + optional Sentry sink) -----
// Logs include trace IDs automatically when a current Activity exists, which lets
// you click from a log line in Grafana Loki to its trace in Tempo, and vice versa.
// The Sentry Serilog sink only attaches when sentryEnabled, otherwise it would
// try to initialise a second SDK instance and fail at runtime.
builder.Host.UseSerilog((ctx, sp, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .Enrich.WithProperty("service.name", ServiceName)
      .Enrich.WithProperty("service.version", serviceVersion)
      .WriteTo.Console(outputTemplate:
          "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

    if (sentryEnabled)
    {
        lc.WriteTo.Sentry(o =>
        {
            o.InitializeSdk = false;     // SDK initialised via UseSentry above
            o.MinimumBreadcrumbLevel = Serilog.Events.LogEventLevel.Information;
            o.MinimumEventLevel = Serilog.Events.LogEventLevel.Warning;
        });
    }
});

// ----- Layers -----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Diagnostic banner — confirms which observability outputs are active on this run.
Console.WriteLine($"[observability] OpenTelemetry={(otelEnabled ? $"enabled → {otlpEndpoint}" : "disabled (no OTEL_EXPORTER_OTLP_ENDPOINT)")}");
Console.WriteLine($"[observability] Sentry={(sentryEnabled ? "enabled" : "disabled (no SENTRY_DSN)")}");

// ----- Controllers + JSON -----
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();

// ----- Swagger -----
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hoardly API",
        Version = "v1",
        Description = "Multi-channel advertising marketplace — hoardings, creators, and more."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer auth. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ----- JWT Auth -----
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

// ----- CORS (Angular dev server) -----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", p => p
        .WithOrigins("http://localhost:4200", "https://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// ----- Pipeline -----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

// HTTPS redirect is OFF in Development because `dotnet run` and the Docker
// dev compose only bind plain HTTP (5000). Leaving it on causes every request
// to 307 to https://localhost:5001 which doesn't exist, killing the frontend
// data fetches with a CORS/redirect error.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AngularApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
