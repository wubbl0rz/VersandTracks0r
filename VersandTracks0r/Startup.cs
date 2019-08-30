using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using VersandTracks0r.Data;
using VersandTracks0r.Models;
using VersandTracks0r.Services;

namespace VersandTracks0r
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IShipmentTracker, DPDTracker>();
            services.AddTransient<IShipmentTracker, HermesTracker>();
            services.AddTransient<IShipmentTracker, DHLTracker>();
            services.AddTransient<IShipmentTracker, AmazonTracker>();
            services.AddTransient<IShipmentTracker, UPSTracker>();
            services.AddControllers();
            services.AddHostedService<BackgroundTracker>();
            services.AddHostedService<BackgroundScanner>();
            services.AddDbContext<AppDbContext>();
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AppDbContext ctx)
        {
            // TODO: make pwa webmanifest

            //ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();

            //ctx.Shipment.Add(new Shipment { Carrier = Carrier.DPD, CreatedAt = DateTime.Now, TrackingId = "01465034571217" });
            //ctx.Shipment.Add(new Shipment { Carrier = Carrier.DPD, CreatedAt = DateTime.Now, TrackingId = "09445609292421" });
            ////ctx.Shipment.Add(new Shipment { Carrier = Carrier.DPD, CreatedAt = DateTime.Now, TrackingId = "152711970276" });
            //ctx.Shipment.Add(new Shipment { Carrier = Carrier.DPD, CreatedAt = DateTime.Now, TrackingId = "09446480518845" });
            //ctx.Shipment.Add(new Shipment { Carrier = Carrier.DPD, CreatedAt = DateTime.Now, TrackingId = "09445682641265" });

            ctx.SaveChanges();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseFileServer();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
