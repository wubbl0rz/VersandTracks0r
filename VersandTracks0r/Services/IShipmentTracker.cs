using System.Collections.Generic;
using VersandTracks0r.Models;

namespace VersandTracks0r.Services
{
    public interface IShipmentTracker
    {
        bool SupportsCarrier(Carrier carrier);
        IEnumerable<ShipmentProgress> Track(string id);
    }
}
