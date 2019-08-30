using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VersandTracks0r.Models;

namespace VersandTracks0r.Services
{

    public class DPDTracker : IShipmentTracker
    {
        private readonly string url = @"https://status.dpd.lv/external/tracking?lang=en&show_all=1&pknr=";
        private readonly HttpClient httpClient = new HttpClient();
        private readonly GeoCoder geoCoder = new GeoCoder();

        private ShipmentStatus FindStatus(string message) => message.ToLower() switch
        {
            "in transit." => ShipmentStatus.Transit,
            "at parcel delivery centre." => ShipmentStatus.Transit,
            "we're sorry but your parcel couldn't be delivered as arranged." => ShipmentStatus.Transit,
            "unfortunately we have not been able to deliver your parcel." => ShipmentStatus.Transit,
            "received by dpd from consignor." => ShipmentStatus.Done,
            "delivered by driver to pickup parcelshop." => ShipmentStatus.Pickup,
            "delivered to pickup point." => ShipmentStatus.Pickup,
            "picked up by consignee from pickup point." => ShipmentStatus.Done,
            "dropped in pickup point." => ShipmentStatus.Pickup,
            "parcel redirected." => ShipmentStatus.Transit,
            "out for delivery." => ShipmentStatus.Delivery,
            "delivered." => ShipmentStatus.Done,
            _ => throw new Exception("Status not found."),
        };

        public bool CheckForError(string id, out string errorMessage)
        {
            errorMessage = string.Empty;

            for (int i = 0; i < 5; i++)
            {
                var result = this.httpClient.GetStringAsync(this.url + id).Result;

                var json = JArray.Parse(result).First;

                var error = json["error"];

                errorMessage = string.Empty;

                if (!string.IsNullOrWhiteSpace(error.ToString()))
                {
                    errorMessage = error["message"].ToString();

                    if (error["code"].ToString() == "200" &&  i != 4)
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        public IEnumerable<ShipmentProgress> Track(string id)
        {
            var ret = new List<ShipmentProgress>();

            // TODO: retry 5 times because api is buggy and sometimes says "not found" but number is valid

            if (this.CheckForError(id, out var error))
            {
                Console.WriteLine("=============");
                Console.WriteLine(id + " ID NOT FOUND");
                Console.WriteLine("=============");
                
                ret.Add(new ShipmentProgress { Status = ShipmentStatus.Invalid, Message = error, UpdatedAt = DateTime.Now });
                return ret;
            }

            var csv = this.httpClient.GetStringAsync(@"https://status.dpd.lv/external/tracking?typ=10&pknr="+ id).Result;

            var lines = csv.Split('\n');

            Console.WriteLine(csv);

            if (lines.Length < 2)
            {
                throw new Exception("DPDTRACKER: NO VALID CSV RESPONSE FOR " + id);
            }

            var header = lines[0];
            var content = lines[1..^1];

            //Parcel no.	Date Time    Depot City    Type of scan Delivery no.Route ZIP code Delivered to Service Tour
            //1465034571217   7022019 1654    146 Marl(DE)   In transit.		10179   77716           136 816
            //1465034571217   7032019 644 179 Freiburg(DE)   At parcel delivery centre.		10179   77716           136 158
            //1465034571217   7032019 647 179 Freiburg(DE)   Out for delivery.       10179   77716           136 81
            //1465034571217   7032019 932 179 Freiburg(DE)   Delivered.      10179   77716           136 81

            foreach (var records in content.Select(c => c.Split(',')))
            {
                var date = records[1].Replace("\"", "");
                var time = records[2].Replace("\"", "");
                var city = records[4].Replace("\"", "");
                var message = records[5].Replace("\"", "");

                var dt = DateTime.ParseExact(date + time, "MMddyyyyHHmm", null);

                Console.WriteLine(message);

                var progress = new ShipmentProgress
                {
                    Location = city,
                    UpdatedAt = dt,
                    Message = message,
                    Status = this.FindStatus(message),
                };

                ret.Add(progress);

                Console.WriteLine(progress.Location);
                Console.WriteLine(progress.Message);
                Console.WriteLine(progress.Status);
                Console.WriteLine(progress.UpdatedAt);
            }

            return ret;
        }

        public bool SupportsCarrier(Carrier carrier)
        {
            return carrier == Carrier.DPD;
        }
    }
}
