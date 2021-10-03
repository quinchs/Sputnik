using Dynmap;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        private ConcurrentQueue<string> _downloadedTiles = new ConcurrentQueue<string>();

        private static readonly Regex _urlRegex = new Regex(@"\/tiles\/(.*?)\/flat.*?\/(z{0,6})(?>_|)(-\d*?|\d*?)_(-\d*?|\d*?)\.");

        private static readonly Regex _tileRegex = new Regex(@"flat.*?\/(z{0,6})(?>_|)(-\d*?|\d*?)_(-\d*?|\d*?)\.");

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

            foreach(var world in client.Worlds)
            {
                if (!Directory.Exists(MapsDirectory + $"/{world.Name}"))
                    Directory.CreateDirectory(MapsDirectory + $"/{world.Name}");
            }

            _config = JsonConvert.DeserializeObject<MapsConfig>(File.ReadAllText(MapsConfig));

            _client.WorldStateUpdated += _client_WorldStateUpdated;
        }

        private async Task _client_WorldStateUpdated(IReadOnlyDictionary<string, Dynmap.API.WorldState> arg1, IReadOnlyDictionary<string, Dynmap.API.WorldState> arg2)
        {
            SemaphoreSlim maxThread = new SemaphoreSlim(6);
            ConcurrentBag<(Image image, string path)> results = new ConcurrentBag<(Image image, string path)>();
            ConcurrentBag<Task> waitTasks = new();

            foreach (var world in arg2)
            {
                foreach (var tileUpdate in world.Value.Updates.Where(x => x.Type == Dynmap.API.TypeEnum.Tile))
                {
                    var match = _tileRegex.Match(tileUpdate.Name);

                    if (!match.Success)
                        continue;

                    var fPath = $"{MapsDirectory}/{match.Groups[1].Value}/{6 - match.Groups[2].Value.Count(x => x == 'z')}_{match.Groups[3].Value}_{match.Groups[4].Value}.png";

                    var tcs = new TaskCompletionSource();
                    waitTasks.Add(tcs.Task);
                    var tileName = $"tiles/{world.Key}/{tileUpdate.Name}";
                    var url = ConfigService.Config.DynmapUri + tileName;
                    async Task getImage()
                    {
                        await maxThread.WaitAsync().ConfigureAwait(false);

                        Image img = null;

                        using (HttpClient c = new HttpClient())
                        {
                            var s = await c.GetStreamAsync(url).ConfigureAwait(false);
                            img = Image.FromStream(s);
                        }

                        Logger.Write($"Downloaded update tile {tileName}", new Severity[] { Severity.Dynmap, Severity.Log }, nameof(MapDownloaderService));

                        results.Add((img, fPath));
                        maxThread.Release();
                        tcs.SetResult();
                    };

                    _ = Task.Factory.StartNew(getImage, TaskCreationOptions.LongRunning);
                }
            }

            await Task.WhenAll(waitTasks);

            foreach (var d in results)
            {
                d.image.Save(d.path);
                d.image.Dispose();
            }
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
                int retries = 0;
                Stream stream = null;

            getStream:

                try
                {
                    stream = await client.GetStreamAsync(uri).ConfigureAwait(false);
                }
                catch(HttpRequestException x) when (x.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    await Task.Delay(250);
                    retries++;

                    if (retries >= 5)
                        throw;

                    goto getStream;
                }

                stream.CopyTo(fs);
                await fs.FlushAsync().ConfigureAwait(false);
                fs.Close();
            }
            s.Stop();

            Logger.Write($"Got tile {match.Groups[1].Value}_{6 - match.Groups[2].Value.Count(x => x == 'z')}_{match.Groups[3].Value}_{match.Groups[4].Value} : {s.ElapsedMilliseconds}ms", new Severity[] { Severity.Dynmap, Severity.Core}, nameof(MapDownloaderService));

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
