using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using WebApplication1.Helpers;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private AzureServiceManagement _asm;
        private MicrosoftGraph _graph;
        public HomeController(AzureServiceManagement asm, MicrosoftGraph graph)
        {
            _asm = asm;
            _graph = graph;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }
        public async Task<IActionResult> TenantSelection()
        {
            ViewData["TenantsList"] = await _asm.GetTenantsList();

            return View();
        }
        public async Task TenantSelect(Guid id)
        {
            var state = new Dictionary<string, string> { { "tenantId", id.ToString() } };
            await HttpContext.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties(state)
            {
                RedirectUri = Url.Action("TenantSelection", "Home")
            }, ChallengeBehavior.Unauthorized);
        }
        public async Task<IActionResult> GraphSample()
        {
            ViewData["Users"] = (await _graph.GetClient().Users.Request().GetAsync()).CurrentPage.ToList();

            return View();
        }
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}
