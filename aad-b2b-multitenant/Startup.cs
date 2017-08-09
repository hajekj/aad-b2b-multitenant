using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Helpers;
using WebApplication1.Models;

namespace WebApplication1
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets<Startup>();
            }
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }
        public IConfigurationRoot Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            // Add session for NaiveSessionCache
            services.AddSession();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton(Configuration);
            services.AddScoped<AzureServiceManagement>();
            services.AddScoped<MicrosoftGraph>();

            services.AddAuthentication(
                SharedOptions => SharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
        }
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            // Configure session middleware.
            app.UseSession();

            app.UseCookieAuthentication();

            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions()
            {
                ClientId = Configuration["Authentication:AzureAd:ClientId"],
                ClientSecret = Configuration["Authentication:AzureAd:ClientSecret"],
                Authority = Configuration["Authentication:AzureAd:AADInstance"] + "common",
                CallbackPath = Configuration["Authentication:AzureAd:CallbackPath"],
                ResponseType = OpenIdConnectResponseType.CodeIdToken,
                GetClaimsFromUserInfoEndpoint = false,

                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                },
                Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = (context) =>
                    {
                        return Task.FromResult(0);
                    },
                    OnAuthenticationFailed = (context) =>
                    {
                        context.Response.Redirect("/Home/Error");
                        context.HandleResponse(); // Suppress the exception
                        return Task.FromResult(0);
                    },
                    OnAuthorizationCodeReceived = async (context) =>
                    {
                        // Exchange code for token using ADAL and save it into the token cache

                        // Either use tenant id with the token cache or clean the token cache each time user changes tenants or it will be confused
                        var tenantId = (context.Ticket.Principal.FindFirst(AzureAdClaimTypes.TenantId))?.Value;
                        var clientCred = new ClientCredential(context.Options.ClientId, context.Options.ClientSecret);
                        var authContext = new AuthenticationContext(context.Options.Authority.Replace("common", $"{tenantId}"), new NaiveSessionCache(tenantId, context.HttpContext.Session));
                        var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(context.ProtocolMessage.Code, new Uri(context.Properties.Items[OpenIdConnectDefaults.RedirectUriForCodePropertiesKey]), clientCred, "https://graph.microsoft.com");
                        context.HandleCodeRedemption(authResult.AccessToken, authResult.IdToken);
                    },
                    OnRedirectToIdentityProvider = (context) =>
                    {
                        string tenantId;
                        context.Properties.Items.TryGetValue("tenantId", out tenantId);
                        if(tenantId != null)
                        {
                            context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress.Replace("common", tenantId);
                        }
                        // Overwrite the common if specified
                        return Task.FromResult(0);
                    },
                    OnTokenValidated = (context) =>
                    {
                        string tenantId;
                        context.Properties.Items.TryGetValue("tenantId", out tenantId);
                        if(tenantId != null)
                        {
                            string userTenantId = context.Ticket.Principal.FindFirst(AzureAdClaimTypes.TenantId)?.Value;
                            if (userTenantId != tenantId)
                            {
                                throw new Exception($"You signed in with wrong tenant, expected: {tenantId} but got {userTenantId}");
                            }
                        }
                        // You would validate whether the organization exists in the system etc.
                        return Task.FromResult(0);
                    }
                }
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
