using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace GmodNetBot
{
    public static class ConfigurationHelpers
    {
        public static ulong GetDiscordServerId(this IConfiguration configuration)
        {
            return ulong.Parse(configuration["DiscordServerId"]);
        }

        public static ulong GetContributorRoleId(this IConfiguration configuration)
        {
            return ulong.Parse(configuration["ContributorRoleId"]);
        }

        public static ulong GetGeneralChannelId(this IConfiguration configuration)
        {
            return ulong.Parse(configuration["GeneralChannelId"]);
        }

        public static string GetDiscordEndpoint(this IConfiguration configuration)
        {
            return configuration["DiscordEndpoint"];
        }

        public static ulong GetDiscordErrorLogId(this IConfiguration configuration)
        {
            return ulong.Parse(configuration["DiscordErrorLogId"]);
        }
    }

    public static class ApllicationExtensions
    {
        public static IApplicationBuilder UseMyExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseExceptionHandler(errorApp =>
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";

                IExceptionHandlerPathFeature exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();

                await context.Response.WriteAsync($"Error 500: Internal Server Error. You unique request id is {context.TraceIdentifier}. " +
                    "Contact administrator and provide request id to get help.");

                StringBuilder request_headers = new StringBuilder();

                foreach(KeyValuePair<string, StringValues> p in context.Request.Headers)
                {
                    request_headers.Append($"\n{p.Key}: {p.Value}");
                }

                StringBuilder request_cookies = new StringBuilder();

                foreach(KeyValuePair<string, string> p in context.Request.Cookies)
                {
                    request_cookies.Append($"\n{p.Key}: {p.Value}");
                }

                ILogger logger = context.RequestServices.GetService<ILogger>();

                logger.LogError($"Exception was thrown while processing request {context.TraceIdentifier}.\n" +
                    $"Request path: {exceptionHandlerPathFeature.Path}" +
                    $"Exception: {exceptionHandlerPathFeature.Error}\n" +
                    $"Request query: {context.Request.QueryString.Value}\n" +
                    $"Request headers: {request_headers}\n" +
                    $"Request cookies: {request_cookies}");

                DiscordSocketClient discord_client = context.RequestServices.GetService<DiscordClientProvider>().Client;

                IConfiguration configuration = context.RequestServices.GetService<IConfiguration>();

                _ = discord_client.GetGuild(configuration.GetDiscordServerId()).GetTextChannel(configuration.GetDiscordErrorLogId())
                    .SendMessageAsync($"Web error 500. Request id {context.TraceIdentifier}. Error type: {exceptionHandlerPathFeature.Error.GetType()}. "
                    + $"Request path: {exceptionHandlerPathFeature.Path}.");
            }));
        }
    }
}
