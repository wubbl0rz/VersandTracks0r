using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VersandTracks0r.Data;
using VersandTracks0r.Models;

namespace VersandTracks0r.Services
{
    public class BackgroundScanner : IHostedService
    {
        private readonly IServiceScopeFactory scopeFactory;

        public BackgroundScanner(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var config = new AppSettings();
            if (string.IsNullOrWhiteSpace(config.Account))
            {
                return Task.CompletedTask;
            }

            Task.Run(() => 
            {
                var scanner = new IMAPMailScanner();

                scanner.Found += Scanner_Found;

                while (true)
                {
                    Console.WriteLine("SCANNING MAILS....");
                    Console.WriteLine("-------------");
                    scanner.Scan();
                    Thread.Sleep(1000 * 30);
                }
            });

            return Task.CompletedTask;
        }

        private void Scanner_Found(Shipment shipment)
        {
            using var scope = this.scopeFactory.CreateScope();
            using var ctx = scope.ServiceProvider.GetService<AppDbContext>();

            shipment.CreatedAt = DateTime.Now;

            Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@");
            Console.WriteLine(shipment.Comment);
            Console.WriteLine(shipment.Carrier);
            Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@");
            
            // exit if already tracking
            if (ctx.Shipment.All(s => s.TrackingId != shipment.TrackingId))
            {
                ctx.Shipment.Add(shipment);

                ctx.SaveChanges();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
