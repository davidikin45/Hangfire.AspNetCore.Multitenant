using Hangfire.Server;
using System.Collections.Generic;
using System.Linq;

namespace Hangfire.AspNetCore.Multitenant
{
    public class HangfireTenant
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public string[] RequestIpAddresses { get; set; }
        public string[] HostNames { get; set; }
        public string HangfireConnectionString { get; set; } = "";

        public JobStorage Storage {get; set;}

        public DashboardOptions DashboardOptions { get; set; }

        public IEnumerable<IBackgroundProcess> AdditionalProcesses { get; set; }

        public bool IpAddressAllowed(string ip)
        {
            return
                RequestIpAddresses == null
                || RequestIpAddresses.Length == 0
                || RequestIpAddresses.Where(i => !i.Contains("*") || i.EndsWith("*")).Any(i => ip.StartsWith(i.Replace("*", "")))
                || RequestIpAddresses.Where(i => i.StartsWith("*")).Any(i => ip.EndsWith(i.Replace("*", "")));
        }
    }
}
