using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using WebApplication1.Models;

namespace WebApplication1.Helpers
{
    public class AzureServiceManagement
    {
        private string _userObjectId;
        private string _tenantId;
        private IHttpContextAccessor _httpContextAccessor;
        private AuthenticationContext _authContext;
        private ClientCredential _clientCredential;
        public AzureServiceManagement(IHttpContextAccessor httpContextAccessor, IConfigurationRoot configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _userObjectId = _httpContextAccessor.HttpContext.User.FindFirst(AzureAdClaimTypes.ObjectId)?.Value;
            _tenantId = _httpContextAccessor.HttpContext.User.FindFirst(AzureAdClaimTypes.TenantId)?.Value;
            _authContext = new AuthenticationContext("https://login.microsoftonline.com/common", new NaiveSessionCache(_tenantId, _httpContextAccessor.HttpContext.Session));
            _clientCredential = new ClientCredential(configuration["Authentication:AzureAd:ClientId"], configuration["Authentication:AzureAd:ClientSecret"]);
        }
        public async Task<TenantsList> GetTenantsList()
        {
            var authResult = await _authContext.AcquireTokenSilentAsync("https://management.core.windows.net/", _clientCredential, new UserIdentifier(_userObjectId, UserIdentifierType.UniqueId));

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

                var response = httpClient.PostAsync("https://portal.azure.com/AzureHubs/api/tenants/List", null).Result; 
                string responseContent = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    TenantsList tenants = JsonConvert.DeserializeObject<TenantsList>(responseContent);
                    return tenants;
                }
            }
        }
    }
}
