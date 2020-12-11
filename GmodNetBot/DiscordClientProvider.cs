using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GmodNetBot
{
    public class DiscordClientProvider
    {
        DiscordSocketClient client;

        public DiscordSocketClient Client => client;

        public DiscordClientProvider(IConfiguration configuration, IHostApplicationLifetime applicationLifetime)
        {
            client = new DiscordSocketClient();
            client.LoginAsync(TokenType.Bot, configuration["BotSecret"]).Wait();
            client.StartAsync();

            applicationLifetime.ApplicationStopping.Register(OnShutdown);
        }

        void OnShutdown()
        {
            client.LogoutAsync().Wait();
            client.StopAsync().Wait();
        }
    }
}
