using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Rewrite;
using System.Net;

namespace YouTubePlaylistCopier
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddGoogle(options =>
                {
                    // Ensure your Google project's ClientID and ClientSecret are configured in the application's
                    // User Secrets using the configuration keys "google:clientID" and "google:clientSecret" respectively.
                    // See https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets for details on using Secret Manager.
                    Configuration.GetSection("google").Bind(options);

                    options.Scope.Add(Google.Apis.YouTube.v3.YouTubeService.Scope.Youtube);
                    options.SaveTokens = true;
                })
                .AddCookie();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseRewriter(new RewriteOptions()
                .AddRedirectToHttps((int)HttpStatusCode.MovedPermanently, env.IsDevelopment() ? 44362 : 443));

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc();
        }
    }
}
