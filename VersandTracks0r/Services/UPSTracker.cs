using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Web;
using VersandTracks0r.Models;

namespace VersandTracks0r.Services
{
    public class UPSTracker : IShipmentTracker
    {
        private readonly string apiUrl = @"https://www.ups.com/track/api/Track/GetStatus?loc=de_DE";

        private readonly HttpClient httpClient = new HttpClient();

        public bool SupportsCarrier(Carrier carrier)
        {
            return carrier == Carrier.UPS;
        }

        public UPSTracker()
        {
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        }

        public IEnumerable<ShipmentProgress> Track(string id)
        {
            var post = new StringContent("{\"TrackingNumber\":[\"" + id + "\"]}", Encoding.UTF8, "application/json");

            var result = this.httpClient.PostAsync(this.apiUrl, post).Result;

            var content = result.Content.ReadAsStringAsync().Result;

            var json = JObject.Parse(content);

            var details = json["trackDetails"];

            var ret = new List<ShipmentProgress>();

            if (!details.HasValues)
            {
                ret.Add(new ShipmentProgress
                {
                    Message = "Tracking id invalid",
                    UpdatedAt = DateTime.Now,
                    Status = ShipmentStatus.Invalid
                });
                return ret;
            }

            var error = details[0]["errorText"];

            if (error.Type != JTokenType.Null)
            {
                ret.Add(new ShipmentProgress
                {
                    Message = error.ToString(),
                    UpdatedAt = DateTime.Now,
                    Status = ShipmentStatus.Invalid
                });
                return ret;
            }

            var history = details[0]["shipmentProgressActivities"];

            foreach (var entry in history)
            {
                var date = entry["date"].ToString();
                var time = entry["time"].ToString();
                var location = entry["location"].ToString();
                var message = entry["activityScan"].ToString();

                //var milestone = entry["milestone"]?["name"]?.ToString();
                var milestone = entry["milestone"];

                var status = ShipmentStatus.Unknown;

                var name = milestone.Type != JTokenType.Null ? milestone["name"].ToString() : string.Empty;

                status = name.ToString() switch
                {
                    "cms.stapp.delivered" => ShipmentStatus.Done,
                    "cms.stapp.outForDelivery" => ShipmentStatus.Delivery,
                    "cms.stapp.shipped" => ShipmentStatus.Transit,
                    "cms.stapp.orderReceived" => ShipmentStatus.Transit,
                    _ => ShipmentStatus.Transit
                };

                //var status = milestone["name"].ToString() switch
                //{
                //    "cms.stapp.delivered" => ShipmentStatus.Done,
                //    _ => throw new Exception("Status not found.")
                //};

                ret.Add(new ShipmentProgress
                {
                    UpdatedAt = DateTime.Parse(date + " " + time),
                    Status = status,
                    Location = location.ToString(),
                    Message = HttpUtility.HtmlDecode(message.ToString())
                });
            }

            return ret;
        }
    }
}
