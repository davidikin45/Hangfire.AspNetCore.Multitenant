using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Hangfire.AspNetCore.Multitenant
{
    public static class AutofacMultiTenancyExtensions
    {
        public static IServiceCollection AddAutofacMultitenant(this IServiceCollection services, Action<MultitenantContainer> mtcSetter)
        {
            return services.AddSingleton<IServiceProviderFactory<ContainerBuilder>>(new AutofacMultiTenantServiceProviderFactory(mtcSetter));
        }

        public static IWebHostBuilder UseAutofacMultiTenant(this IWebHostBuilder builder)
        {
            MultitenantContainer multiTenantContainer = null;
            Func<MultitenantContainer> multitenantContainerAccessor = () => multiTenantContainer;
            Action<MultitenantContainer> multitenantContainerSetter = (mtc) => { multiTenantContainer = mtc; };
            builder.ConfigureServices(services => services.AddAutofacMultitenant(multitenantContainerSetter));
            builder.ConfigureServices(services => services.AddSingleton((sp) => multiTenantContainer));
            return builder.UseAutofacMultitenantRequestServices(multitenantContainerAccessor);
        }

        private class AutofacMultiTenantServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
        {
            private Action<MultitenantContainer> _mtcSetter;

            public AutofacMultiTenantServiceProviderFactory(Action<MultitenantContainer> mtcSetter)
            {
                _mtcSetter = mtcSetter;
            }

            public ContainerBuilder CreateBuilder(IServiceCollection services)
            {
                var containerBuilder = new ContainerBuilder();

                containerBuilder.Populate(services);

                return containerBuilder;
            }

            public IServiceProvider CreateServiceProvider(ContainerBuilder builder)
            {
                var container = builder.Build();

                var tenantIdentificationStrategy = container.Resolve<ITenantIdentificationStrategy>();
                var mtc = new MultitenantContainer(tenantIdentificationStrategy, container);

                var configuration = container.Resolve<IConfiguration>();
                var environment = container.Resolve<IHostingEnvironment>();

                _mtcSetter(mtc);

                return new AutofacServiceProvider(mtc);
            }
        }
    }
}
