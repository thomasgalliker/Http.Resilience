using System;
using Http.Resilience;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpResilience(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            // Configuration
            serviceCollection.Configure<HttpRetryOptions>(configuration);

            // Register services
            serviceCollection.AddHttpResilience();

            return serviceCollection;
        }

        public static IServiceCollection AddHttpResilience(
            this IServiceCollection services,
            Action<HttpRetryOptions> options = null)
        {
            // Configuration
            if (options != null)
            {
                services.Configure(options);
            }

            // Register services
            services.AddSingleton<IHttpRetryHelper, HttpRetryHelper>();

            return services;
        }
    }
}