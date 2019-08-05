using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Data
{
    public abstract class HangfireTenantsJsonStore : HangfireTenantsInMemoryCachingStore
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly HangfireTenantsJsonStoreOptions _options;

        public HangfireTenantsJsonStore(ILogger logger, IServiceProvider serviceProvider, IHostingEnvironment hostingEnvironment, IOptions<HangfireTenantsJsonStoreOptions> options)
            :base(logger, serviceProvider)
        {
            _hostingEnvironment = hostingEnvironment;
            _options = options.Value;
        }

        public override Task<IEnumerable<HangfireTenant>> GetAllActiveTenantsFromStoreAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(JsonConvert.DeserializeObject<List<HangfireTenant>>(File.ReadAllText(Path.Combine(_hostingEnvironment.ContentRootPath, _options.Path)), _options.SerializerSettings).Cast<HangfireTenant>());
        }
    }

    public class HangfireTenantsJsonStoreOptions
    {
        public string Path { get; set; } = "tenants.json";
        public JsonSerializerSettings SerializerSettings { get; set; } = new JsonSerializerSettings()
        {
            Converters = new List<JsonConverter>() { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };
    }
}
