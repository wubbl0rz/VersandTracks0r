using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using VersandTracks0r.Models;

namespace VersandTracks0r.Services
{
    public class DHLTracker : IShipmentTracker
    {
        private readonly string API_KEY = @"QPXUC9P7J0C0F2idkPUeriu8gevvOLUV";

        private readonly string apiUrl = @"https://api-eu.dhl.com/track/shipments?language=de&trackingNumber=";

        private readonly HttpClient httpClient = new HttpClient();

        public DHLTracker()
        {
            this.httpClient.DefaultRequestHeaders.Add("DHL-API-Key", this.API_KEY);
        }

        public bool SupportsCarrier(Carrier carrier)
        {
            return carrier == Carrier.DHL;
        }

        public IEnumerable<ShipmentProgress> Track(string id)
        {
            var ret = new List<ShipmentProgress>();

            string result;

            try
            {
                result = this.httpClient.GetStringAsync(this.apiUrl + id).GetAwaiter().GetResult();
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("404") || ex.Message.Contains("400"))
                {
                    ret.Add(new ShipmentProgress
                    {
                        Status = ShipmentStatus.Invalid,
                        UpdatedAt = DateTime.Now,
                        Message = "Invalid tracking id.",
                    });
                    return ret;
                }
                else if (ex.Message.Contains("429"))
                {
                    ret.Add(new ShipmentProgress
                    {
                        Status = ShipmentStatus.Unknown,
                        UpdatedAt = DateTime.Now,
                        Message = "API ERROR: TOO MANY REQUESTS",
                    });
                    return ret;
                }
                throw ex;
            }

            var json = JObject.Parse(result);

            var history = json["shipments"][0]["events"];

            foreach (var entry in history)
            {
                var time = entry["timestamp"].ToString();
                var location = entry["location"]?["address"]?["addressLocality"]?.ToString();
                var message = entry["status"].ToString();

                //Console.WriteLine(message);
                //Console.WriteLine(entry["message"].ToString());

                var statusCode = entry["statusCode"].ToString() switch
                {
                    "transit" when message.Contains("Die Sendung wurde in das Zustellfahrzeug geladen.") => ShipmentStatus.Delivery,
                    "transit" => ShipmentStatus.Transit,
                    "delivered" => ShipmentStatus.Done,
                    "pre-transit" => ShipmentStatus.Transit,
                    "unknown" => ShipmentStatus.Transit,
                    "failure" => ShipmentStatus.Transit,
                    _ => throw new Exception("Status not found."),
                };

                if (statusCode == ShipmentStatus.Done)
                {
                    location = string.Empty;
                }

                var progress = new ShipmentProgress
                {
                    Message = message,
                    Location = location ?? string.Empty,
                    UpdatedAt = DateTime.Parse(time),
                    Status = statusCode,
                };

                ret.Add(progress);
            }

            ret.Reverse();

            return ret;
        }
    }
}
