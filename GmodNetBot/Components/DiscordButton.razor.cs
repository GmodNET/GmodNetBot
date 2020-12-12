using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http;
using System.Web;

namespace GmodNetBot.Components
{
    public partial class DiscordButton
    {
        [Parameter]
        public string InternalRedirect { get; set; }

        [Parameter]
        public string Scope { get; set; }

        string DiscordAuthUrl()
        {
            using SHA256 hash_algorithm = SHA256.Create();

            byte[] hashed_user_id = hash_algorithm.ComputeHash(Convert.FromBase64String(httpContextAccessor.HttpContext.Request.Cookies["GenericUserId"]));
            string hashed_user_id_string = Convert.ToBase64String(hashed_user_id);

            string serilized_state = Encoding.UTF8
                .GetString(JsonSerializer.SerializeToUtf8Bytes<DiscordCallbackState>(new DiscordCallbackState(hashed_user_id_string, InternalRedirect)));

            QueryString button_query = new QueryString("");
            button_query = button_query.Add("response_type", "code");
            button_query = button_query.Add("client_id", configuration["DiscordClient"]);
            button_query = button_query.Add("scope", Scope);
            button_query = button_query.Add("prompt", "consent");
            button_query = button_query.Add("redirect_uri", navigationManager.BaseUri + "discord-callback");
            button_query = button_query.Add("state", serilized_state);

            return configuration.GetDiscordEndpoint() + "/oauth2/authorize" + button_query.Value;
        }
    }
}
