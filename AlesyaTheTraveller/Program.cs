using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AlesyaTheTraveller
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IWebHost webHost = CreateWebHostBuilder(args).Build();

            using (var scope = webHost.Services.CreateScope())
            {
                var cacheService = scope.ServiceProvider.GetRequiredService<Services.IFlightDataCacheService>();
                var dataService = scope.ServiceProvider.GetRequiredService<Services.IFlightDataService>();

                var tasks = new List<Task<Entities.DestinationEntity[]>>
                {
                    dataService.GetData(Entities.DestinationType.Country),
                    dataService.GetData(Entities.DestinationType.City)
                };

                foreach (var task in tasks)
                {
                    Parallel.ForEach(await task, (x) => cacheService.AddData(x.Code, x));
                }
            }

            webHost.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
