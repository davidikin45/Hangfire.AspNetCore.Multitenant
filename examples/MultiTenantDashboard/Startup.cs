using Hangfire;
using Hangfire.AspNetCore.Multitenant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire.AspNetCore.Multitenant.Request;
using Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies;
using Hangfire.AspNetCore.Multitenant.Data;
using Autofac;
using AspNetCore.Base.Hangfire;

namespace MultiTenantDashboard
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddControllersAsServices();

            services.AddHangfireMultiTenantStore<HangfireTenantsStore>();
            services.AddHangfireTenantRequestIdentification().TenantFromHostQueryStringSourceIP();
            services.AddHangfireTenantConfiguration();

            services.AddHttpContextAccessor();

            services.AddHangfire(config => {
                config.UseFilter(new HangfireLoggerAttribute());
                config.UseFilter(new HangfirePreserveOriginalQueueAttribute());
            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {

        }

         // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
         public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseHangfireDashboardMultiTenant("/hangfire", async (context) =>
            {
                var hangfireTenantStore = context.RequestServices.GetRequiredService<IHangfireTenantsStore>();
                if (context.RequestServices.GetRequiredService<IHangfireTenantIdentificationStrategy>().TryIdentifyTenant(out var tenantId))
                    return (await hangfireTenantStore.GetTenantByIdAsync(tenantId))?.DashboardOptions;
                else
                    return null;
            }, async (context) =>
            {
                var hangfireTenantStore = context.RequestServices.GetRequiredService<IHangfireTenantsStore>();
                if (context.RequestServices.GetRequiredService<IHangfireTenantIdentificationStrategy>().TryIdentifyTenant(out var tenantId))
                    return (await hangfireTenantStore.GetTenantByIdAsync(tenantId))?.Storage;
                else
                    return null;
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
