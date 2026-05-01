using Application.Interfaces.AssemblyMaker;
using Asp.Versioning;
using Infrastructure.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Polly;
using System.IO.Compression;
using System.Net;

namespace API.Config
{
    public static class ApiConfigAndResilience
    {
        public static void AddApiServicesAndResilience(this IServiceCollection services, ConfigurationManager configuration,
        IWebHostEnvironment environment)
        {
            services
                .AddSingleton<ILoggerFactory, LoggerFactory>()
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddResiliencePipeline("resilience", (builder, context) =>
            {
                builder
                .AddTimeout(TimeSpan.FromSeconds(30))
                .AddRetry(new()
                {
                    MaxRetryAttempts = 3,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<InvalidOperationException>()
                        .Handle<HttpRequestException>()
                        .Handle<OperationCanceledException>()
                        .HandleResult(r => ((HttpResponseMessage)r).StatusCode == HttpStatusCode.TooManyRequests || !((HttpResponseMessage)r).IsSuccessStatusCode),
                    Delay = TimeSpan.FromSeconds(3),
                    BackoffType = DelayBackoffType.Exponential,
                    OnRetry = retryArguments =>
                    {
                        var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger<GenericRestClient>();

                        logger.LogDebug("Trying {tentative} after {duration} seconds by {Exception}",
                            retryArguments.AttemptNumber, retryArguments.RetryDelay, retryArguments.Outcome.Exception.Message);

                        return ValueTask.CompletedTask;
                    }
                });
            });

            // Configure
            services
                .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest)
                .Configure<RouteOptions>(options => options.LowercaseUrls = true)
                .Configure<ApiBehaviorOptions>(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                });

            services.AddControllers(options =>
            {
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            });

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services
                .AddEndpointsApiExplorer()
                .AddCors(options =>
                {
                    options.AddPolicy("AllowPolicy",
                        builder =>
                        {
                            builder.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                        });
                })
                .AddResponseCompression(options =>
                {
                    options.EnableForHttps = true;
                    options.Providers.Add<GzipCompressionProvider>();
                })
                .AddMemoryCache()
                .AddHttpClient()
                .AddHealthChecks();

            services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            // Services
            services.Scan(scan => scan
                .FromAssemblyOf<IServiceAssemblyMarker>()
                .AddClasses(classes => classes.AssignableTo(typeof(IServiceAssemblyMarker)).Where(item => !item.IsAbstract))
                .AsImplementedInterfaces()
                .WithTransientLifetime());
        }
    }
}
