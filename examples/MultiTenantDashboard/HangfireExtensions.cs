using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MultiTenantDashboard
{
    public static class HangfireExtensions
    {
        public static IServiceCollection AddHangfireServerServices(this IServiceCollection services)
        {
            services.AddSingleton<IBackgroundJobClient>(x =>
            {
                ThrowIfNotConfigured(x);

                if (GetInternalServices(x, out var factory, out var stateChanger, out _))
                {
                    return new BackgroundJobClient(x.GetRequiredService<JobStorage>(), factory, stateChanger);
                }

                return new BackgroundJobClient(
                    x.GetRequiredService<JobStorage>(),
                    x.GetRequiredService<IJobFilterProvider>());
            });

            services.AddSingleton<IRecurringJobManager>(x =>
            {
                ThrowIfNotConfigured(x);

                if (GetInternalServices(x, out var factory, out _, out _))
                {
                    return new RecurringJobManager(
                        x.GetRequiredService<JobStorage>(),
                        factory,
                        x.GetService<ITimeZoneResolver>());
                }

                return new RecurringJobManager(
                   x.GetRequiredService<JobStorage>(),
                   x.GetRequiredService<IJobFilterProvider>(),
                    x.GetService<ITimeZoneResolver>());
            });

            return services;
        }

        internal static void ThrowIfNotConfigured(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetService<IGlobalConfiguration>();
            if (configuration == null)
            {
                throw new InvalidOperationException(
                    "Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddHangfire' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }
        }

        internal static bool GetInternalServices(
           IServiceProvider provider,
           out IBackgroundJobFactory factory,
           out IBackgroundJobStateChanger stateChanger,
           out IBackgroundJobPerformer performer)
        {
            factory = provider.GetService<IBackgroundJobFactory>();
            performer = provider.GetService<IBackgroundJobPerformer>();
            stateChanger = provider.GetService<IBackgroundJobStateChanger>();

            if (factory != null && performer != null && stateChanger != null)
            {
                return true;
            }

            factory = null;
            performer = null;
            stateChanger = null;

            return false;
        }
    }
}
