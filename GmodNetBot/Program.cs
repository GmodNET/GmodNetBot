using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using System.Security.Cryptography.X509Certificates;

namespace GmodNetBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Async(a => a.File("log.txt", rollOnFileSizeLimit: true, retainedFileCountLimit: 2))
                .CreateLogger();

            try
            {
                Log.Information("Starting bot");
                CreateHostBuilder(args).Build().Run();
                Log.Information("Bot exited");
            }
            catch(Exception e)
            {
                Log.Fatal(e, "Bot terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel((context, options) =>
                    {
                        IWebHostEnvironment env = context.HostingEnvironment;
                        if(env.IsProduction())
                        {
                            options.ListenAnyIP(80);
                            options.ListenAnyIP(443, config =>
                            {
                                config.UseHttps(X509Certificate2.CreateFromPemFile("cert.pem", "cert.key"));
                            });
                        }
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
