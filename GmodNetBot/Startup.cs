using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Discord;
using Discord.WebSocket;

namespace GmodNetBot
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
            services.AddRazorPages();
            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.AddSingleton<GitHubClientProvider>();
            services.AddSingleton<DiscordClientProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.ApplicationServices.GetService<DiscordClientProvider>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(errorApp => 
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/plain";

                    IExceptionHandlerPathFeature exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();

                    await context.Response.WriteAsync($"Error 500: Internal Server Error. You unique request id is {context.TraceIdentifier}. " +
                        "Contact administrator and provide request id to get help.");

                    DiscordSocketClient discord_client = context.RequestServices.GetService<DiscordClientProvider>().Client;

                    IConfiguration configuration = context.RequestServices.GetService<IConfiguration>();

                    _ = discord_client.GetGuild(configuration.GetDiscordServerId()).GetTextChannel(configuration.GetDiscordErrorLogId())
                        .SendMessageAsync($"Web error 500. Request id {context.TraceIdentifier}. Error type: {exceptionHandlerPathFeature.Error.GetType()}. "
                        + $"Request path: {exceptionHandlerPathFeature.Path}.");
                }));
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None,
                Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always
            });

            app.Use(async (context, next) =>
            {
                if (!context.Request.Cookies.ContainsKey("GenericUserId"))
                {
                    using RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

                    byte[] random_seq = new byte[64];
                    rngCsp.GetBytes(random_seq);

                    string random_string = Convert.ToBase64String(random_seq);

                    context.Response.Cookies.Append("GenericUserId", random_string);

                    context.Items.Add("GenericUserId", random_string);
                }

                await next();
            });

            app.Use(async (context, next) =>
            {
                if(context.Request.Path == "/discord-callback")
                {
                    IQueryCollection query = context.Request.Query;

                    if(query.ContainsKey("state"))
                    {
                        try
                        {
                            DiscordCallbackState callbackState = JsonSerializer.Deserialize<DiscordCallbackState>(query["state"]);

                            if(!String.IsNullOrEmpty(callbackState.InternalRedirect))
                            {
                                QueryString redirectQuery = QueryString.Create(query.Where(p => p.Key != "state"));
                                redirectQuery = redirectQuery.Add("state", callbackState.State);

                                context.Response.Redirect(context.Request.Scheme + "://" +  context.Request.Host.Value 
                                    + callbackState.InternalRedirect + redirectQuery.Value);

                                return;
                            }
                        }
                        catch
                        {

                        }
                    }
                }

                await next();
            });

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapFallbackToPage("/Index");
            });

#if DEBUG
            try
            {
                File.WriteAllText("update.browserlink", DateTime.Now.ToString());
            }
            catch
            {

            }
#endif
        }
    }
}
