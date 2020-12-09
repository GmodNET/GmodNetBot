using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Octokit;

namespace GmodNetBot
{
    public class GitHubClientProvider
    {
        IConfiguration configuration;

        public GitHubClientProvider(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public GitHubClient GetClient()
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("GmodNetDiscordBot"));

            Credentials credentials = new Credentials(configuration["GithubSecret"]);

            client.Credentials = credentials;

            return client;
        }
    }
}
