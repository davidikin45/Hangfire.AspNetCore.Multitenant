using Autofac.Multitenant;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Mvc
{
    [HtmlTargetElement("tenant")]
    public sealed class TenantTagHelper : TagHelper
    {
        private readonly ITenantIdentificationStrategy _tenantIdentificationStrategy;

        public TenantTagHelper(ITenantIdentificationStrategy tenantIdentificationStrategy)
        {
            _tenantIdentificationStrategy = tenantIdentificationStrategy;
        }

        [HtmlAttributeName("tenant-id")]
        public string TenantId { get; set; }

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
             _tenantIdentificationStrategy.TryIdentifyTenant(out var tenantId);
            if (TenantId != tenantId?.ToString())
            {
                output.SuppressOutput();
            }

            return base.ProcessAsync(context, output);
        }
    }
}
