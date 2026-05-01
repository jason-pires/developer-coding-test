using System.Diagnostics;
using API.Config;
using API.DI;
using API.Middleware;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

var openTelemetrySection = builder.Configuration.GetSection("OpenTelemetry");
var serviceName = openTelemetrySection["ServiceName"] ?? "hacker-news-api";
var serviceVersion = openTelemetrySection["ServiceVersion"] ?? "1.0.0";
var otlpEndpoint = openTelemetrySection["OtlpEndpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddSource("HackerNews.Api");

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
            });
        }
    });

builder.Services.AddOpenApi();
builder.Services.AddConfig(builder.Configuration);
builder.Services.ConfigureMapster();
builder.Services.AddApiServicesAndResilience(builder.Configuration, builder.Environment);
builder.Services.AddMyDependencyInjection();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.Use(async (context, next) =>
{
    using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString()))
    using (LogContext.PushProperty("SpanId", Activity.Current?.SpanId.ToString()))
    {
        await next();
    }
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseResponseCompression();
app.UseCors("AllowPolicy");
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hacker News Orchestrator API v1");
    options.RoutePrefix = string.Empty;
});

app.MapControllers();
app.UseStaticFiles();

app.Run();
