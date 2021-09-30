using Dynmap.API;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Dynmap
{
    public class RestClient
    {
        private readonly HttpClient _client;
        private string _baseUri;
        private DynmapClient _dynmapClient;

        public RestClient(string uri, DynmapClient dynmapClient)
        {
            _client = new();
            _dynmapClient = dynmapClient;

            _baseUri = uri;

            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.82 Safari/537.36");

            _client.BaseAddress = new Uri(uri);
        }

        public Task<Configuration> GetConfigurationAsync()
        {
            return SendAsync<Configuration>("up/configuration", "GET");
        }

        public Task<WorldState> GetWorldStateAsync(string world = "world", DateTimeOffset? dateTimeOffset = null)
        {
            dateTimeOffset ??= DateTime.UtcNow;

            return SendAsync<WorldState>($"up/world/{world}/{dateTimeOffset.Value.ToJavaDateTime()}", "GET");
        }

        public Task<Markers> GetMarkersAsync(string world = "world")
        {
            return SendAsync<Markers>($"tiles/_markers_/marker_{world}.json", "GET");
        }

        private async Task<T> SendAsync<T>(string endpoint, string method, object payload = null) where T : class
        {
            var message = new HttpRequestMessage(GetMethod(method), _baseUri + endpoint);

            if (payload != null)
            {
                message.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            }

            var s = new Stopwatch();
            s.Start();
            var response = await _client.SendAsync(message);
            s.Stop();

            _dynmapClient.LogInternal($"{method} {endpoint}: {response.StatusCode} {s.ElapsedMilliseconds}ms");
            var content = await response.Content.ReadAsStringAsync();

            if (content != null)
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            else return null;
        }

        private async Task SendAsync(string endpoint, string method, object payload = null)
        {
            var message = new HttpRequestMessage(GetMethod(method), _baseUri + endpoint);

            if (payload != null)
            {
                message.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            }

            var s = new Stopwatch();
            s.Start();
            var r =  await _client.SendAsync(message);
            s.Stop();

            _dynmapClient.LogInternal($"{method} {endpoint}: {r.StatusCode} {s.ElapsedMilliseconds}ms");
        }

        private HttpMethod GetMethod(string method)
        {
            return method switch
            {
                "DELETE" => HttpMethod.Delete,
                "GET" => HttpMethod.Get,
                "PATCH" => HttpMethod.Patch,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                _ => throw new ArgumentOutOfRangeException(nameof(method), $"Unknown HttpMethod: {method}"),
            };
        }
    }
}
