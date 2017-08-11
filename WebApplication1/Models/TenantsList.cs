using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebApplication1.Models
{
    public class TenantsList
    {
        [JsonProperty("failure")]
        public bool Failure;
        [JsonProperty("tenants")]
        public List<TenantsListTenant> Tenants;
    }
    public class TenantsListTenant
    {
        [JsonProperty("id")]
        public string Id;
        [JsonProperty("domainName")]
        public string DomainName;
        [JsonProperty("displayName")]
        public string DisplayName;
        [JsonProperty("isSignedInTenant")]
        public bool IsSignedInTenant;
    }
}
