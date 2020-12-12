using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GmodNetBot
{
    public record RequestStatus(bool Success, string Message);
    
    public record DiscordOauthTokenResponse(string token_type, string access_token);

    public record DiscordUserRecord(string id, string username, string discriminator);

    public record DiscordConnectionRecord(string name, string type, bool verified);

    public record DiscordCallbackState(string State, string InternalRedirect);
}
