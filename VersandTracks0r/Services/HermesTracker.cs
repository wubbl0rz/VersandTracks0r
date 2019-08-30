using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using VersandTracks0r.Models;

namespace VersandTracks0r.Services
{
    public class HermesTracker : IShipmentTracker
    {
        private readonly string apiUrl = @"https://www.myhermes.de/services/tracking/shipments?search=";

        private readonly HttpClient httpClient = new HttpClient();

        public bool SupportsCarrier(Carrier carrier)
        {
            return carrier == Carrier.Hermes;
        }

        public IEnumerable<ShipmentProgress> Track(string id)
        {
            var ret = new List<ShipmentProgress>();

            var result = string.Empty;

            try
            {
                result = this.httpClient.GetStringAsync(this.apiUrl + id).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                ret.Add(new ShipmentProgress
                {
                    Status = ShipmentStatus.Invalid,
                    Message = "Invalid number",
                    UpdatedAt = DateTime.Now,
                });
                
                return ret;
            }


            var json = JArray.Parse(result).First;

            var history = json["statusHistory"];

            //            {
            //                "description": "Die Sendung wurde zugestellt. ",
            //  "dateTime": "31.07.2019 12:14"
            //}
            //            {
            //                "description": "Die Sendung befindet sich in der Zustellung.",
            //  "dateTime": "31.07.2019 08:35"
            //            }
            //            {
            //                "description": "Die Sendung ist im Hermes Verteilzentrum LC Berlin eingetroffen.",
            //  "dateTime": "30.07.2019 10:55"
            //            }
            //            {
            //                "description": "Die Sendung wurde im Hermes Logistikzentrum sortiert.",
            //  "dateTime": "30.07.2019 10:55"
            //            }
            //            {
            //                "description": "Die Sendung ist im Hermes Verteilzentrum LC Berlin eingetroffen.",
            //  "dateTime": "29.07.2019 17:37"
            //            }
            //            {
            //                "description": "Die Sendung wurde im Hermes Logistikzentrum sortiert.",
            //  "dateTime": "29.07.2019 17:37"
            //            }
            //            {
            //                "description": "Die Sendung wurde Hermes elektronisch angekündigt.",
            //  "dateTime": "29.07.2019 07:54"
            //            }

            foreach (var entry in history)
            {
                var desc = entry["description"].ToString().Trim();
                var time = entry["dateTime"].ToString().Trim();

                var dt = DateTime.ParseExact(time, "dd.MM.yyyy HH:mm", null);
                
                var match = Regex.Match(desc, "Die Sendung ist im Hermes Verteilzentrum (.*?) eingetroffen");

                var location = match.Groups.Count > 1 ? match.Groups[1].Value : "";

                var status = desc switch
                {
                    "Die Sendung wurde Hermes elektronisch angekündigt." => ShipmentStatus.Transit,
                    "Die Sendung wurde zugestellt." => ShipmentStatus.Done,
                    "Die Sendung befindet sich in der Zustellung." => ShipmentStatus.Delivery,
                    "Die Sendung wurde im Hermes Logistikzentrum sortiert." => ShipmentStatus.Transit,
                    _ when desc.Contains("Die Sendung ist im Hermes Verteilzentrum") => ShipmentStatus.Transit,
                    _ => throw new Exception("No valid status found.")
                };

                var progress = new ShipmentProgress
                {
                    Status = status,
                    Location = location,
                    Message = desc,
                    UpdatedAt = dt,
                };

                ret.Add(progress);
            }

            ret.Reverse();

            return ret;
        }
    }
}
