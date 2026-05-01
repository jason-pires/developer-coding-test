using Asp.Versioning;
using Common.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using System.IO.Compression;

namespace API.Config
{
    public static class ApiConfigAndResilience
    {
        public static void AddApiServicesAndResilience(this IServiceCollection services, ConfigurationManager configuration,
            IWebHostEnvironment environment)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddResiliencePipeline(GenericHttpClientOptions.ClientName, (builder, context) =>
            {
                var httpClientOptions = context.ServiceProvider.GetRequiredService<IOptions<GenericHttpClientOptions>>().Value;

                builder
                    .AddTimeout(TimeSpan.FromSeconds(httpClientOptions.TimeoutSeconds))
                    .AddRetry(new RetryStrategyOptions
                    {
                        MaxRetryAttempts = 3,
                        ShouldHandle = new PredicateBuilder()
                            .Handle<HttpRequestException>()
                            .Handle<TimeoutRejectedException>(),
                        Delay = TimeSpan.FromSeconds(1),
                        BackoffType = DelayBackoffType.Exponential,
                        OnRetry = retryArguments =>
                        {
                            var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();
                            var logger = loggerFactory.CreateLogger("HackerNewsHttpResilience");

                            logger.LogWarning(
                                "Retry {Attempt} for Hacker News after {Delay}. Exception: {ExceptionMessage}",
                                retryArguments.AttemptNumber + 1,
                                retryArguments.RetryDelay,
                                retryArguments.Outcome.Exception?.Message);

                            return ValueTask.CompletedTask;
                        }
                    });
            });

            services
                .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest)
                .Configure<RouteOptions>(options => options.LowercaseUrls = true)
                .Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

            services.AddControllers(options =>
            {
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            });

            services
                .AddEndpointsApiExplorer()
                .AddCors(options =>
                {
                    options.AddPolicy("AllowPolicy", builder =>
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
                .AddHttpClient(GenericHttpClientOptions.ClientName, (serviceProvider, client) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<GenericHttpClientOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.Timeout = Timeout.InfiniteTimeSpan;
                });

            services.AddHealthChecks();

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
        }
    }
}
