using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace VersandTracks0r.Services
{
    public class GeoCoder
    {
        private readonly string apiUrl = @"http://photon.komoot.de/api/?limit=1&lat=50.110882&lon=8.679490&lang=de&q=";

        private readonly HttpClient httpClient = new HttpClient();

        private static readonly ConcurrentDictionary<string, (double lon, double lat)> cache = 
            new ConcurrentDictionary<string, (double lon, double lat)>();
        
        public bool TryLookupCoordinates(string city, out double @long, out double lat)
        {
            @long = 0.0;
            lat = 0.0;

            if (string.IsNullOrWhiteSpace(city))
            {
                return false;
            }

            var encodedCity = HttpUtility.UrlEncode(city);
            var url = apiUrl + encodedCity;

            if (GeoCoder.cache.TryGetValue(url, out var r))
            {
                @long = r.lon;
                lat = r.lat;
                return true;
            }

            var result = this.httpClient.GetStringAsync(url).Result;

            System.Console.WriteLine(apiUrl + encodedCity);
            
            var json = JObject.Parse(result);

            var tmp = json["features"].FirstOrDefault(f => f?["properties"]?["country"]?.ToString() == "Deutschland");
            
            if (tmp != null)
            {
                @long = double.Parse(tmp["geometry"]["coordinates"].First.ToString());
                lat = double.Parse(tmp["geometry"]["coordinates"].Last.ToString());

                // TODO: das caching hier suckt 
                GeoCoder.cache.TryAdd(url, (@long, lat));

                return true;
            }

            return false;
        }
    }
}
