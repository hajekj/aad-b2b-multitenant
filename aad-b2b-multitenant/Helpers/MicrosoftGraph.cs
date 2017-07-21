using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using WebApplication1.Models;

namespace WebApplication1.Helpers
{
    public class MicrosoftGraph
    {
        private GraphServiceClient _graphClient;
        private string _userObjectId;
        private string _tenantId;
        private IHttpContextAccessor _httpContextAccessor;
        private AuthenticationContext _authContext;
        private ClientCredential _clientCredential;
        public MicrosoftGraph(IHttpContextAccessor httpContextAccessor, IConfigurationRoot configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _userObjectId = _httpContextAccessor.HttpContext.User.FindFirst(AzureAdClaimTypes.ObjectId)?.Value;
            _tenantId = _httpContextAccessor.HttpContext.User.FindFirst(AzureAdClaimTypes.TenantId)?.Value;
            _authContext = new AuthenticationContext("https://login.microsoftonline.com/common", new NaiveSessionCache(_tenantId, _httpContextAccessor.HttpContext.Session));
            _clientCredential = new ClientCredential(configuration["Authentication:AzureAd:ClientId"], configuration["Authentication:AzureAd:ClientSecret"]);
        }
        public GraphServiceClient GetClient()
        {
            if(_graphClient == null)
            {
                _graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(async requestMessage =>
                {
                    var authResult = await _authContext.AcquireTokenSilentAsync("https://graph.microsoft.com", _clientCredential, new UserIdentifier(_userObjectId, UserIdentifierType.UniqueId));

                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(authResult.AccessTokenType, authResult.AccessToken);
                }));
            }
            return _graphClient;
        }
    }
}
