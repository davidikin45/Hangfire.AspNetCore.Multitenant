using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Hangfire.Annotations;
using Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Hangfire.AspNetCore.Multitenant
{
    public class MultiTenantJobActivator : JobActivator
    {
        private readonly MultitenantContainer _mtc;
        private readonly object _tenantId;

        public MultiTenantJobActivator(MultitenantContainer mtc, object tenantId)
        {
            _mtc = mtc;
            _tenantId = tenantId;
        }

        public override JobActivatorScope BeginScope(JobActivatorContext context)
        {
            var scope = _mtc.GetTenantScope(_tenantId).BeginLifetimeScope();

            var tenantService = scope.Resolve<IHangfireTenantIdentificationStrategy>();
            tenantService.TenantId = _tenantId;

            return new AspNetCoreMultiTenantJobActivatorScope(scope);
        }

#pragma warning disable CS0672 // Member overrides obsolete member
        public override JobActivatorScope BeginScope()
#pragma warning restore CS0672 // Member overrides obsolete member
        {
            var scope = _mtc.GetTenantScope(_tenantId).BeginLifetimeScope();

            var tenantService = scope.Resolve<IHangfireTenantIdentificationStrategy>();
            tenantService.TenantId = _tenantId;

            return new AspNetCoreMultiTenantJobActivatorScope(scope);
        }

        public override object ActivateJob(Type jobType)
        {
            return base.ActivateJob(jobType);
        }
    }

    public class AspNetCoreMultiTenantJobActivatorScope : JobActivatorScope
    {
        private readonly ILifetimeScope _serviceScope;

        public AspNetCoreMultiTenantJobActivatorScope([NotNull] ILifetimeScope serviceScope)
        {
            if (serviceScope == null) throw new ArgumentNullException(nameof(serviceScope));
            _serviceScope = serviceScope;
        }

        public override object Resolve(Type type)
        {
            if (_serviceScope.IsRegistered(type))
                return _serviceScope.Resolve(type);

            return ActivatorUtilities.GetServiceOrCreateInstance(new AutofacServiceProvider(_serviceScope), type);
        }

        public override void DisposeScope()
        {
            _serviceScope.Dispose();
        }
    }
}
