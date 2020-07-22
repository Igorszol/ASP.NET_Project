using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ASP.NET_Project.EntityFramework;
using ASP.NET_Project.Models;
using ASP.NET_Project.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ASP.NET_Project
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });



            services.AddAuthentication(AzureADB2CDefaults.AuthenticationScheme)
                    .AddAzureADB2C(options => Configuration.Bind("AzureAdB2C", options));

            services.Configure<OpenIdConnectOptions>(AzureADB2CDefaults.OpenIdScheme, options =>
            {
                var onTokenValidated = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async context =>
                {
                   

                    var identity = context.Principal.Identities.First();

                    var db = context.HttpContext.RequestServices.GetRequiredService<DataContext>();
                    string email="";

                    foreach (Claim c in identity.Claims)
                    {
                        if (c.Type == "emails")
                        {
                            email = c.Value;
                        }
                    }
                    User user = await db.Users.FirstOrDefaultAsync(a => a.Email == email);
                    if (user==null)
                    {
                        user = new User();
                        user.Email = email;
                        user.Role = "User";
                        db.Users.Add(user);
                        await db.SaveChangesAsync();
                    }

                    identity.AddClaim(new Claim(identity.RoleClaimType, user.Role));


                };
            });



            var connection = Configuration["DatabaseConnectionString"];
            services.AddDbContext<DataContext>(options => options.UseSqlServer(connection));

            services.AddSingleton<IAzureBlobConnectionFactory, AzureBlobConnectionFactory>();
            services.AddSingleton<IAzureBlobService, AzureBlobService>();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

       
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        
    }
}
