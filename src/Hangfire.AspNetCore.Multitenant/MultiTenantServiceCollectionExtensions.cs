using Hangfire.AspNetCore.Multitenant.Data;
using Hangfire.AspNetCore.Multitenant.Request;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace Hangfire.AspNetCore.Multitenant
{
    public static class MultiTenantServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the hangfire tenant request identification service to the application.
        /// </summary>
        public static HangfireTenantRequestIdentificationBuilder AddHangfireTenantRequestIdentification(this IServiceCollection services)
        {
            return new HangfireTenantRequestIdentificationBuilder(services);
        }

        /// <summary>
        /// Adds the hangfire multi tenant store to the application.
        /// </summary>
        public static IServiceCollection AddHangfireMultiTenantStore<TTenantStore>(this IServiceCollection services, ServiceLifetime contextLifetime = ServiceLifetime.Scoped)
        where TTenantStore : class, IHangfireTenantsStore
        {
            services.Add(new ServiceDescriptor(typeof(IHangfireTenantsStore), typeof(TTenantStore), contextLifetime));
            return services;
        }

        /// <summary>
        /// Adds the hangfire tenant configurations to the application.
        /// </summary>
        public static IServiceCollection AddHangfireTenantConfiguration(this IServiceCollection services, Assembly assembly)
        {
            var types = assembly
                .GetExportedTypes()
                .Where(type => typeof(IHangfireTenantConfiguration).IsAssignableFrom(type))
                .Where(type => (type.IsAbstract == false) && (type.IsInterface == false));

            foreach (var type in types)
            {
                services.AddSingleton(typeof(IHangfireTenantConfiguration), type);
            }

            return services;
        }

        /// <summary>
        /// Adds the hangfire tenant configurations to the application.
        /// </summary>
        public static IServiceCollection AddHangfireTenantConfiguration(this IServiceCollection services)
        {
            var target = Assembly.GetCallingAssembly();
            return services.AddHangfireTenantConfiguration(target);
        }
    }
}
