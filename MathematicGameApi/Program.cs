using MathematicGameApi.Infrastructure.Services.Contracts;
using MathematicGameApi.Infrastructure.Services.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MathematicGameApi
{
    public class Program
    {
        private static string _PathToContentRoot;

        public static void Main(string[] args)
        {
            _PathToContentRoot = Directory.GetCurrentDirectory();
            CreateHostBuilder(args).UseDefaultServiceProvider(options =>
                    options.ValidateScopes = false).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseContentRoot(_PathToContentRoot ?? Directory.GetCurrentDirectory())
                        .UseStartup<Startup>()
                        .ConfigureAppConfiguration((hostingContext, config) =>
                            {
                                // Config file doesn't working in Docker without this settings
                                config.SetBasePath(Directory.GetCurrentDirectory());
#if DEBUG
                                config.AddJsonFile("appsettings.Development.json", optional: false,
                                    reloadOnChange: false);
#else
                                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
#endif


                                config.AddEnvironmentVariables();
                            }
                        );
                }
            ).ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<BackgroundPrinter>();
            });
        //ConfigureServices(hostContext, services => 
        //            services.AddHostedService<BackgroundPrinter>());
        //.ConfigureServices((hostingContext, services) =>
        //{
        //    services.AddHttpClient();
        //    services.AddSingleton<IHttpContextAccessor,
        //      HttpContextAccessor>();
        //    services.AddRouting();
        //    services.AddTransient<ICoreService, CoreService>();
        //    services.AddHostedService<BackgroundPrinter>();
        //});
    }
}