using System;
using System.Collections.Generic;
using VersandTracks0r.Models;

namespace VersandTracks0r.Services
{
    public class AmazonTracker : IShipmentTracker
    {
        public bool SupportsCarrier(Carrier carrier)
        {
            return carrier == Carrier.Amazon;
        }

        public IEnumerable<ShipmentProgress> Track(string id)
        {
            var ret = new List<ShipmentProgress>();

            ret.Add(new ShipmentProgress 
            {
                Message = "Amazon tracking not supported yet.",
                Status = ShipmentStatus.Unknown,
                UpdatedAt = DateTime.Now
            });

            return ret;
        }
    }
}
