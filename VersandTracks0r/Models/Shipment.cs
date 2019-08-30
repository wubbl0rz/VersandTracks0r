using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VersandTracks0r.Models
{
    public class Shipment
    {
        public string Comment { get; set; } // email absender domain und betreff
        public bool IsDeleted { get; set; }
        public int Id { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Carrier Carrier { get; set; }
        public bool HasData => this.History.Count > 0;
        public bool Manual { get; set; }
        [Required]
        public string TrackingId { get; set; }
        //public DateTime NextCheck { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShipmentStatus Status => this.HasData ? this.History.OrderBy(h => h.UpdatedAt).Last().Status : ShipmentStatus.Unknown;
        public List<ShipmentProgress> History { get; set; } = new List<ShipmentProgress>();
    }
}
