using Application.Interfaces.AssemblyMaker;
using Common.Config;
using Infrastructure.Interfaces.AssemblyMaker;

namespace API.DI
{
    public static class DependencyInjectionConfig
    {
        public static IServiceCollection AddConfig(
             this IServiceCollection services, IConfiguration config)
        {
            services.Configure<GenericHttpClientOptions>(
                config.GetSection("GenericHttpClientOptions"));
            services.Configure<HackerNewsOptions>(
                config.GetSection("HackerNewsOptions"));

            return services;
        }

        public static IServiceCollection AddMyDependencyInjection(
             this IServiceCollection services)
        {
            services.Scan(scan => scan
                .FromAssemblyOf<IServiceAssemblyMarker>()
                .AddClasses(classes => classes.AssignableTo(typeof(IServiceAssemblyMarker)).Where(item => !item.IsAbstract))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.Scan(scan => scan
                .FromAssemblyOf<IInfrastructureAssemblyMarker>()
                .AddClasses(classes =>
                    classes.AssignableTo(typeof(IInfrastructureAssemblyMarker)).Where(item => !item.IsAbstract))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }
    }
}
