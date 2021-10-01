using Dynmap;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sputnik.Services
{
    public class MapDownloaderService
    {
        public const string MapsDirectory = "./Maps";
        public const string MapsConfig = "./Maps/Config.json";
        public const string PlayerheadDir = "./PlayerHeads";
        private readonly DynmapClient _client;
        private MapsConfig _config;

        private static readonly Regex _urlRegex = new Regex(@"\/tiles\/(.*?)\/flat.*?\/(z{0,6})(?>_|)(-\d*?|\d*?)_(-\d*?|\d*?)\.");

        public MapDownloaderService(DynmapClient client)
        {
            _client = client;

            if (!Directory.Exists(MapsDirectory))
                Directory.CreateDirectory(MapsDirectory);

            if (!Directory.Exists(PlayerheadDir))
                Directory.CreateDirectory(PlayerheadDir);

            if (!File.Exists(MapsConfig))
            {
                File.WriteAllText(MapsConfig, JsonConvert.SerializeObject(new MapsConfig()
                {
                    LastDownloaded = DateTime.UtcNow - TimeSpan.FromDays(1),
                }));
            }

            _config = JsonConvert.DeserializeObject<MapsConfig>(File.ReadAllText(MapsConfig));

        }

        public static async Task<Image> GetTileAsync(string uri)
        {
            var match = _urlRegex.Match(uri);

            var fPath = $"{MapsDirectory}/{match.Groups[1].Value}/{6 - match.Groups[2].Value.Count(x => x == 'z')}_{match.Groups[3].Value}_{match.Groups[4].Value}.png";

            if (File.Exists(fPath))
                return Image.FromFile(fPath);

            var s = new Stopwatch();
            s.Start();

            using (HttpClient client = new HttpClient())
            using(var fs = File.OpenWrite(fPath))
            {
                var stream = await client.GetStreamAsync(uri).ConfigureAwait(false);
                stream.CopyTo(fs);
                await fs.FlushAsync().ConfigureAwait(false);
                fs.Close();
            }
            s.Stop();

            Logger.Log($"Got tile {match.Groups[1].Value}_{6 - match.Groups[2].Value.Count(x => x == 'z')}_{match.Groups[3].Value}_{match.Groups[4].Value} : {s.ElapsedMilliseconds}ms", Severity.Dynmap);

            return Image.FromFile(fPath);
        }

        public static async Task<Image> GetPlayerheadAsync(string username)
        {
            var fPath = PlayerheadDir + $"/{username}.png";
            
            if (File.Exists(fPath))
                return Image.FromFile(fPath);

            using (var client = new HttpClient())
            {
                var r = await client.GetAsync(Generation.Utils.GetPlayerheadUrl(username));
                var imageStream = await r.Content.ReadAsStreamAsync().ConfigureAwait(false);

                using(var fs = File.OpenWrite(fPath))
                {
                    imageStream.CopyTo(fs);
                    await fs.FlushAsync();
                    fs.Close();
                }
            }

            return Image.FromFile(fPath);
        }
    }

    public class MapsConfig
    {
        public DateTime LastDownloaded { get; set; }
    }
}
