using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VersandTracks0r.Models;

namespace VersandTracks0r.Services
{
    public class IMAPMailScanner
    {
        private readonly Regex dhl = new Regex(@"(\ |\ )(\d{20}|JD\d{18}|JJD\d{18})");
        private readonly Regex ups = new Regex(@"(\ |\ )(1Z\w{16})");
        private readonly Regex amazon = new Regex(@"(\ |\ )(A\w{2}\d{9})", RegexOptions.IgnoreCase);
        private readonly Regex dpd = new Regex(@"(\ |\ )(0\d{13})");
        private readonly Regex hermes = new Regex(@"(\ |\ )(\d{14})");
        private readonly AppSettings appSettings;
        
        private DateTime lastScan = DateTime.Today.Subtract(TimeSpan.FromDays(30));

        public event Action<Shipment> Found;

        public IMAPMailScanner()
        {
            this.appSettings = new AppSettings();
        }

        public void Scan()
        {
            using var client = new ImapClient();

            client.Connect(this.appSettings.Server, 993, true);

            var config = new AppSettings();

            var username = config.Account;
            var pw = config.Password;

            client.Authenticate(username, pw);

            var inbox = client.Inbox;
            inbox.Open(FolderAccess.ReadOnly);
            
            var query = new DateSearchQuery(SearchTerm.DeliveredAfter, this.lastScan);
            this.lastScan = DateTime.Now.Subtract(TimeSpan.FromMinutes(5));

            var result = inbox.Search(query);

            var messages = result.Select(id => inbox.GetMessage(id)).OrderByDescending(msg => msg.Date);

            foreach (var message in messages)
            {
                this.Parse(message);
            }

        }

        public void Parse(MimeMessage message)
        {
            if (message.TextBody is string text)
            {
                text = text.ToLower();
                var address = message.From.Mailboxes.First().Address;
                var domain = address.Split('@').Last();

                if (!text.Contains(this.appSettings.Name.ToLower()))
                {
                    return;
                }

                var shipment = new Shipment
                {
                    CreatedAt = DateTime.Now,
                    Comment = domain,
                };

                if (this.dhl.Match(text) is Match dhlMatch && dhlMatch.Success && text.Contains("dhl"))
                {
                    shipment.Carrier = Carrier.DHL;
                    shipment.TrackingId = dhlMatch.Groups[2].Value;
                    this.Found?.Invoke(shipment);
                }
                else if (this.ups.Match(text) is Match upsMatch && upsMatch.Success && text.Contains("ups"))
                {
                    shipment.Carrier = Carrier.UPS;
                    shipment.TrackingId = upsMatch.Groups[2].Value;
                    this.Found?.Invoke(shipment);
                }
                else if (this.dpd.Match(text) is Match dpdMatch && dpdMatch.Success && text.Contains("dpd"))
                {
                    shipment.Carrier = Carrier.DPD;
                    shipment.TrackingId = dpdMatch.Groups[2].Value;
                    this.Found?.Invoke(shipment);
                }
                else if (this.hermes.Match(text) is Match hermesMatch && hermesMatch.Success && text.Contains("hermes"))
                {
                    shipment.Carrier = Carrier.Hermes;
                    shipment.TrackingId = hermesMatch.Groups[2].Value;
                    this.Found?.Invoke(shipment);
                }
                else if (this.amazon.Match(text) is Match amazonMatch && amazonMatch.Success && text.Contains("amazon"))
                {
                    shipment.Carrier = Carrier.Amazon;
                    shipment.TrackingId = amazonMatch.Groups[2].Value;
                    this.Found?.Invoke(shipment);
                }
            }
        }
    }
}
