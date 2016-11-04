using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace AutobahnTestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseUrls("http://localhost:5000", "https://localhost:5443")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();

            if (string.Equals(builder.GetSetting("server"), "Microsoft.AspNetCore.Server.WebListener", System.StringComparison.Ordinal))
            {
                builder.UseWebListener();
            }
            else
            {
                builder.UseKestrel(options =>
                {
                    var certPath = Path.Combine(AppContext.BaseDirectory, "TestResources", "testCert.pfx");
                    options.UseHttps(certPath, "testPassword");
                });
            }

            var host = builder.Build();
            host.Run();
        }
    }
}
