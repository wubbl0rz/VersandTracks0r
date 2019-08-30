using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.IO;

namespace VersandTracks0r.Models
{
    public class AppSettings
    {
        public string DefaultLocation { get; set; }
        public string Name { get; set; }
        public string Account { get; set; }
        public string Server { get; set; }
        public string Password { get; set; }

        public AppSettings()
        {
            var config = File.ReadAllText("config.json");
            var json = JObject.Parse(config);
            this.Name = json["name"].ToString();
            this.DefaultLocation = json["location"].ToString();
            this.Account = json["account"].ToString();
            this.Password = json["password"].ToString();
            this.Server = json["server"].ToString();

        }
    }
}
