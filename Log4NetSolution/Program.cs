using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Log4NetSolution
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, loggingBuilder) =>
                {
                    // block some system default logs

                    // loggingBuilder.AddFilter("System", LogLevel.Error);
                    // loggingBuilder.AddFilter("Microsoft", LogLevel.Error);

                    // optional config file,default name is log4net.config

                    // var path = Path.Combine(Directory.GetCurrentDirectory(), "Log4Net.config");
                    // loggingBuilder.AddLog4Net(path);

                    loggingBuilder.AddLog4Net();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
