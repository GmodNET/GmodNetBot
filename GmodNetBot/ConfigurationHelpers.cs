using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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
    }
}
