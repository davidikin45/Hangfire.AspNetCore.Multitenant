﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies
{
    public sealed class DynamicTenantIdentificationService : IHangfireTenantIdentificationStrategy
    {
        private readonly Func<HttpContext, HangfireTenant> _currentTenant;
        private readonly Func<IEnumerable<HangfireTenant>> _allTenants;

        private readonly ILogger<IHangfireTenantIdentificationStrategy> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public DynamicTenantIdentificationService(IHttpContextAccessor contextAccessor, ILogger<IHangfireTenantIdentificationStrategy> logger, Func<HttpContext, HangfireTenant> currentTenant, Func<IEnumerable<HangfireTenant>> allTenants)
        {
            if (currentTenant == null)
            {
                throw new ArgumentNullException(nameof(currentTenant));
            }

            if (allTenants == null)
            {
                throw new ArgumentNullException(nameof(allTenants));
            }
            _contextAccessor = contextAccessor;
            _logger = logger;

            this._currentTenant = currentTenant;
            this._allTenants = allTenants;
        }

        public object TenantId { get; set; }

        public IEnumerable<HangfireTenant> GetAllTenants()
        {
            return this._allTenants();
        }

        public Task<HangfireTenant> GetTenantAsync(HttpContext httpContext)
        {
            var tenant = this._currentTenant(httpContext);
            TenantId = tenant?.Id;

            return Task.FromResult(tenant);
        }

        public bool TryIdentifyTenant(out object tenantId)
        {
            if (TenantId != null)
            {
                tenantId = TenantId;
                return true;
            }

            var httpContext = _contextAccessor.HttpContext;

            var tenant = GetTenantAsync(httpContext).GetAwaiter().GetResult();
            if (tenant != null)
            {
                tenantId = tenant.Id;
                return true;
            }

            tenantId = null;
            return false;
        }
    }
}