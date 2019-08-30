using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VersandTracks0r.Data;
using VersandTracks0r.Models;

namespace VersandTracks0r.Services
{
    public class BackgroundTracker : IHostedService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly IEnumerable<IShipmentTracker> trackers;
        private readonly GeoCoder geoCoder = new GeoCoder();
        private readonly AppSettings appSettings = new AppSettings();

        public Task StartAsync(CancellationToken cancellationToken)
        {

            Task.Run(Tick);
            
            return Task.CompletedTask;
        }

        public BackgroundTracker(IEnumerable<IShipmentTracker> trackers, IServiceScopeFactory scopeFactory)
        {
            this.trackers = trackers;
            this.scopeFactory = scopeFactory;
        }

        private void Tick()
        {
            Thread.Sleep(2000);
            
            while (true)
            {
                Console.WriteLine("=====================");
                Console.WriteLine("TICK");
                Console.WriteLine("=====================");

                using var scope = this.scopeFactory.CreateScope();
                using var ctx = scope.ServiceProvider.GetService<AppDbContext>();
                //ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys=OFF;");
                //ctx.Database.ExecuteSqlRaw("PRAGMA ignore_check_constraints=true;");

                // TODO: updated und status keine custom getter sondern direkt db sonst kann man hier nicht filtern mit where

                var shipments = ctx.Shipment
                    .Where(s => !s.IsDeleted)
                    .Include(s => s.History);

                var rwl = new ReaderWriterLockSlim();
                
                var tasks = new List<Task>();
                foreach (var shipment in shipments)
                {
                    var task = Task.Run(() =>
                    {
                        if (shipment.Status == ShipmentStatus.Done || shipment.Status == ShipmentStatus.Invalid)
                        {
                            return;
                        }

                        //wurde innerhalb der letzten 5min geupdatet also skippen
                        if (shipment.UpdatedAt.AddMinutes(5) > DateTime.Now && shipment.HasData)
                        {
                            Console.WriteLine("ALREADY UPDATED");
                            Console.WriteLine(shipment.Id);
                            Console.WriteLine(shipment.TrackingId);
                            Console.WriteLine(shipment.UpdatedAt);
                            Console.WriteLine(shipment.Status);
                            return;
                        }

                        try
                        {
                            var tracker = this.trackers.FirstOrDefault(t => t.SupportsCarrier(shipment.Carrier));

                            if (tracker == null)
                            {
                                return;
                            }

                            Console.WriteLine(shipment.TrackingId);
                            var history = tracker.Track(shipment.TrackingId);

                            if (history.FirstOrDefault(h => h.Status == ShipmentStatus.Done) is ShipmentProgress p)
                            {
                                if (p.Location == string.Empty)
                                {
                                    p.Location = this.appSettings.DefaultLocation;
                                }
                            }

                            // geocode location (ort zu längen und breitengrad)
                            foreach (var entry in history)
                            {
                                if (this.geoCoder.TryLookupCoordinates(entry.Location, out var lon, out var lat))
                                {
                                    entry.Lat = lat;
                                    entry.Long = lon;
                                }
                            }

                            var hasInvalid = history.Any(h => h.Status == ShipmentStatus.Invalid);

                            // invalid nur reinschreiben wenn leer
                            if (hasInvalid && shipment.History.Count == 0)
                            {
                                shipment.History.AddRange(history);
                            }
                            // nicht überschreiben mit was kaputtenem wenn  was drin ist
                            // wenn invalid und vorher schon was drin war dann nix machen
                            else if (!hasInvalid)
                            {
                                // gucken ob das ding schon drin is dann nicht löschen
                                shipment.History.Clear();
                                shipment.History.AddRange(history);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        shipment.UpdatedAt = DateTime.Now;

                        if (!shipment.Manual && shipment.Status == ShipmentStatus.Invalid)
                        {
                            shipment.IsDeleted = true;
                        }
                        
                        rwl.EnterWriteLock();
                        ctx.SaveChanges();
                        rwl.ExitWriteLock();
                    });
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
                Thread.Sleep(5000);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
