# Hangfire ASP.NET Core Multitenant
[![nuget](https://img.shields.io/nuget/v/Hangfire.AspNetCore.Multitenant.svg)](https://www.nuget.org/packages/Hangfire.AspNetCore.Multitenant/) ![Downloads](https://img.shields.io/nuget/dt/Hangfire.AspNetCore.Multitenant.svg "Downloads")

## Installation

### NuGet
```
PM> Install-Package Hangfire.AspNetCore.MultiTenant
```

### .Net CLI
```
> dotnet add package Hangfire.AspNetCore.MultiTenant
```

## Example
```
 public class HangfireTenantsStore : HangfireTenantsInMemoryStore
{
	public HangfireTenantsStore()
	{
		Tenants = new List<HangfireTenant>(){
			new HangfireTenant(){ Id ="default", HostNames = new string []{ "*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} },
			new HangfireTenant(){ Id ="tenant1", HostNames = new string []{ "tenant1.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }}} ,
			new HangfireTenant(){ Id ="tenant2", HostNames = new string []{ "tenant2.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
			new HangfireTenant(){ Id ="tenant3", HostNames = new string []{ "tenant3.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
			new HangfireTenant(){ Id ="tenant4", HostNames = new string []{ "tenant4.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
			new HangfireTenant(){ Id ="tenant5", HostNames = new string []{ "tenant5.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
			new HangfireTenant(){ Id ="tenant6", HostNames = new string []{ "tenant6.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
			new HangfireTenant(){ Id ="tenant7", HostNames = new string []{ "tenant7.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
			new HangfireTenant(){ Id ="tenant8", HostNames = new string []{ "tenant8.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
			new HangfireTenant(){ Id ="tenant9", HostNames = new string []{ "tenant9.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} } ,
			new HangfireTenant(){ Id ="tenant10", HostNames = new string []{ "tenant10.*" }, HangfireConnectionString = "", DashboardOptions = new DashboardOptions{ Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }} },
		};
	}

	//C:\WINDOWS\system32\drivers\etc\hosts
	//127.0.0.1 tenant1.localhost
	//127.0.0.1 tenant2.localhost
	//127.0.0.1 tenant3.localhost
	//127.0.0.1 tenant4.localhost
	//127.0.0.1 tenant5.localhost
	//127.0.0.1 tenant6.localhost
	//127.0.0.1 tenant7.localhost
	//127.0.0.1 tenant8.localhost
	//127.0.0.1 tenant9.localhost
	//127.0.0.1 tenant10.localhost
}
```

```
public class Program
{
	public static async Task Main(string[] args)
	{
		var webHost = CreateWebHostBuilder(args).Build();

		using (var scope = webHost.Services.CreateScope())
		{
			var serviceProvider = scope.ServiceProvider;

			try
			{
				var multitenantContainer = serviceProvider.GetRequiredService<MultitenantContainer>();
				var configuration = serviceProvider.GetRequiredService<IConfiguration>();
				var environment = serviceProvider.GetRequiredService<IHostingEnvironment>();
				var applicationLifetime = serviceProvider.GetRequiredService<IApplicationLifetime>();

				var tenantsStore = serviceProvider.GetRequiredService<IHangfireTenantsStore>();
				var tenantConfigurations = serviceProvider.GetServices<IHangfireTenantConfiguration>();

				foreach (var tenant in (await tenantsStore.GetAllTenantsAsync()).ToList())
				{
					var actionBuilder = new ConfigurationActionBuilder();

					var tenantInitializer = tenantConfigurations.FirstOrDefault(i => i.TenantId.ToString() == tenant.Id);

					if (tenantInitializer != null)
					{
						tenantInitializer.ConfigureServices(actionBuilder, configuration, environment);
					}

					if (tenant.HangfireConnectionString != null)
					{
						var serverDetails = HangfireMultiTenantLauncher.StartHangfireServer(tenant.Id, "web-background", tenant.HangfireConnectionString, multitenantContainer, options => { options.ApplicationLifetime = applicationLifetime; options.AdditionalProcesses = tenant.AdditionalProcesses; });

						tenant.Storage = serverDetails.Storage;

						if (tenantInitializer != null)
						{
							tenantInitializer.ConfigureHangfireJobs(serverDetails.recurringJobManager, configuration, environment);
						}

						actionBuilder.Add(b => b.RegisterInstance(serverDetails.recurringJobManager).As<IRecurringJobManager>().SingleInstance());
						actionBuilder.Add(b => b.RegisterInstance(serverDetails.backgroundJobClient).As<IBackgroundJobClient>().SingleInstance());
					}
					else
					{
						actionBuilder.Add(b => b.RegisterInstance<IRecurringJobManager>(null).As<IRecurringJobManager>().SingleInstance());
						actionBuilder.Add(b => b.RegisterInstance<IBackgroundJobClient>(null).As<IBackgroundJobClient>().SingleInstance());
					}

					multitenantContainer.ConfigureTenant(tenant.Id, actionBuilder.Build());
				}
			}
			catch (Exception ex)
			{
				var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("UserInitialisation");
				logger.LogError(ex, "Failed to Initialize");
			}
		}
		
		await webHost.RunAsync();
	}

	public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
		WebHost.CreateDefaultBuilder(args)
			.UseAutofacMultiTenant()
			.UseStartup<Startup>();
}
```

```
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
```

## Authors

* **Dave Ikin** - [davidikin45](https://github.com/davidikin45)


## License

This project is licensed under the MIT License


## Acknowledgments

* [Autofac Multitenant](https://autofaccn.readthedocs.io/en/latest/advanced/multitenant.html)
* [Writing Multitenant ASP.NET Core Applications](https://stackify.com/writing-multitenant-asp-net-core-applications/)