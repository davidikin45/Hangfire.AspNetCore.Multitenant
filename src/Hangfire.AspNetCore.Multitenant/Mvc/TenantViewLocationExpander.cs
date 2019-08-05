using Autofac.Multitenant;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Hangfire.AspNetCore.Multitenant.Mvc
{
    public class TenantViewLocationExpander<TTenant> : IViewLocationExpander
    {
        private const string ValueKey = "tenantId";

        public TenantViewLocationExpander()
        {
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            var tenantProvider = context.ActionContext.HttpContext.RequestServices.GetRequiredService<ITenantIdentificationStrategy>();
            tenantProvider.TryIdentifyTenant(out var tenantId);
            context.Values[ValueKey] = tenantId?.ToString();
        }

        //The view locations passed to ExpandViewLocations are:
        // /Views/{1}/{0}.cshtml
        // /Shared/{0}.cshtml
        // /Pages/{0}.cshtml
        //Where {0} is the view and {1} the controller name.
        public virtual IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            foreach (var location in viewLocations)
            {
                if(context.Values[ValueKey] != null)
                {
                    yield return location.Replace("{0}", context.Values[ValueKey] + "/{0}");
                }

                yield return location;
            }
        }
    }
}
