﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using System.Net.Http.Headers;
using Octokit;
using Discord.WebSocket;
using Discord;
using System.Net.Http;
using Microsoft.Extensions.Primitives;
using System.Security.Cryptography;
using System.Text;

namespace GmodNetBot.Components
{
    public partial class ContributorCallback
    {
        RequestStatus requestStatus;

        protected override async Task OnInitializedAsync()
        {
            Uri request_uri = new Uri(navigationManager.Uri);

            Dictionary<string, StringValues> request_query = QueryHelpers.ParseQuery(request_uri.Query);

            if(!request_query.ContainsKey("code"))
            {
                requestStatus = new RequestStatus(false, "There is no Discord auth code in the current request");
                return;
            }

            string discord_auth_code = request_query["code"];

            if(!request_query.ContainsKey("state"))
            {
                requestStatus = new RequestStatus(false, "There is no Anti-CSRF token in this request");
                return;
            }

            byte[] state_token = Convert.FromBase64String(request_query["state"]);

            if(!httpContextAccessor.HttpContext.Request.Cookies.ContainsKey("GenericUserId"))
            {
                requestStatus = new RequestStatus(false, "There is no id cookie in the request");
                return;
            }

            byte[] generic_user_id = Convert.FromBase64String(httpContextAccessor.HttpContext.Request.Cookies["GenericUserId"]);

            using SHA256 sha256 = SHA256.Create();

            if(!state_token.SequenceEqual(sha256.ComputeHash(generic_user_id)))
            {
                requestStatus = new RequestStatus(false, "Anti-CSRF token is invalid");
                return;
            }

            using HttpClient httpClient = httpFactory.CreateClient();

            FormUrlEncodedContent token_request_content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", configuration["DiscordClient"]),
                new KeyValuePair<string, string>("client_secret", configuration["DiscordSecret"]),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", discord_auth_code),
                new KeyValuePair<string, string>("redirect_uri", navigationManager.BaseUri + "discord-callback"),
                new KeyValuePair<string, string>("scope", "identify connections")
            });

            HttpResponseMessage discord_response = await httpClient.PostAsync(configuration.GetDiscordEndpoint() + "/oauth2/token", token_request_content);

            if (!discord_response.IsSuccessStatusCode)
            {
                requestStatus = new RequestStatus(false, $"Unable to perform Discord API call (response status code {discord_response.StatusCode})");
                return;
            }

            DiscordOauthTokenResponse oauthTokenResponse = await JsonSerializer
                .DeserializeAsync<DiscordOauthTokenResponse>(await discord_response.Content.ReadAsStreamAsync());

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(oauthTokenResponse.token_type, oauthTokenResponse.access_token);

            discord_response = await httpClient.GetAsync(configuration.GetDiscordEndpoint() + "/users/@me");

            if (!discord_response.IsSuccessStatusCode)
            {
                requestStatus = new RequestStatus(false, $"Unable to get user identity (response status code {discord_response.StatusCode})");
                return;
            }

            DiscordUserRecord userRecord = await JsonSerializer.DeserializeAsync<DiscordUserRecord>(await discord_response.Content.ReadAsStreamAsync());

            discord_response = await httpClient.GetAsync(configuration.GetDiscordEndpoint() + "/users/@me/connections");

            if (!discord_response.IsSuccessStatusCode)
            {
                requestStatus = new RequestStatus(false, $"Unable to get user connections (response status code {discord_response.StatusCode})");
                return;
            }

            DiscordConnectionRecord[] userConnections = await JsonSerializer.DeserializeAsync<DiscordConnectionRecord[]>(await discord_response.Content.ReadAsStreamAsync());

            if (!userConnections.Any(c => c.type == "github" && c.verified))
            {
                requestStatus = new RequestStatus(false, "You don't have any verified GitHub accounts connected with Discord :(");
                return;
            }

            string gitHubUserName = userConnections.First(c => c.type == "github" && c.verified).name;

            GitHubClient githubClient = githubProvider.GetClient();

            var orgRepos = await githubClient.Repository.GetAllForOrg("GmodNET");

            bool foundUser = false;

            foreach (Repository r in orgRepos)
            {
                var contributors = await githubClient.Repository.GetAllContributors(r.Id);
                if (contributors.Any(c => c.Login == gitHubUserName))
                {
                    foundUser = true;
                    break;
                }
            }

            if (!foundUser)
            {
                requestStatus = new RequestStatus(false, @"We are not able to find you among our contributors :(. Maybe information is just not up to date yet. " +
                    "Wait a few hours and try again");
                return;
            }

            DiscordSocketClient discordClient = discordProvider.Client;

            if (!discordClient.GetGuild(configuration.GetDiscordServerId()).Users.Any(u => u.Username == userRecord.username))
            {
                requestStatus = new RequestStatus(false, "We are not able to find you on our server :(");
                return;
            }

            IRole contributorRole = discordClient.GetGuild(configuration.GetDiscordServerId()).GetRole(configuration.GetContributorRoleId());

            if(discordClient.GetGuild(configuration.GetDiscordServerId()).GetUser(ulong.Parse(userRecord.id)).Roles.Any(r => r.Id == contributorRole.Id))
            {
                requestStatus = new RequestStatus(false, "You are already Contributor 😕");
                return;
            }

            await discordClient.GetGuild(configuration.GetDiscordServerId()).GetUser(ulong.Parse(userRecord.id)).AddRoleAsync(contributorRole);

            await discordClient.GetGuild(configuration.GetDiscordServerId()).GetTextChannel(configuration.GetGeneralChannelId())
                .SendMessageAsync($"We have a new contributor! Welcome {MentionUtils.MentionUser(ulong.Parse(userRecord.id))}!");

            requestStatus = new RequestStatus(true, "Your Discord roles were successfully updated!");
        }
    }
}
