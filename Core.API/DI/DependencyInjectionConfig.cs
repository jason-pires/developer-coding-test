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
            services.Configure<GenericRestClientOptions>(
                config.GetSection("GenericRestClientOptions"));

            return services;
        }

        public static IServiceCollection AddMyDependencyInjection(
             this IServiceCollection services)
        {
            //services.AddTransient<IMobyRestClient, MobyRestClient>();

            // Services
            services.Scan(scan => scan
                .FromAssemblyOf<IServiceAssemblyMarker>()
                .AddClasses(classes => classes.AssignableTo(typeof(IServiceAssemblyMarker)).Where(item => !item.IsAbstract))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            //Repositories
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
