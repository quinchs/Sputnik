using Discord.WebSocket;
using Dynmap;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Handlers
{
    public class CommandAutocompleteHandler : DiscordHandler
    {
        private DiscordSocketClient _client;
        private DynmapClient _dynmapClient;
        public override void Initialize(DiscordSocketClient client, DynmapClient dynmap)
        {
            _client = client;
            _dynmapClient = dynmap;

            client.InteractionCreated += Client_InteractionCreated;
        }

        private async Task Client_InteractionCreated(SocketInteraction arg)
        {
            if (arg is not SocketAutocompleteInteraction auto)
                return;

            switch (auto.Data.CommandName)
            {
                case "alerts":
                    {
                        switch (auto.Data.Current?.Name)
                        {
                            case "name":
                                Stopwatch s = new Stopwatch();
                                s.Start();
                                var names = (await MongoService.AlertCollection.Find(x => true).ToListAsync()).Select(x => x.Name);

                                if (string.IsNullOrEmpty(auto.Data.Current.Value?.ToString()))
                                {
                                    await auto.RespondAsync(names.Select(x => new Discord.AutocompleteResult(x, x)).Take(20));
                                }
                                else
                                {
                                    var orderedNames = names.OrderByDescending(x => Compute(x, (string)auto.Data.Current.Value));

                                    await auto.RespondAsync(orderedNames.Select(x => new Discord.AutocompleteResult(x, x)).Take(20));
                                }

                                s.Stop();

                                Logger.Debug($"Autocomplete executed in {s.ElapsedMilliseconds}ms", Severity.Socket);
                                break;
                        }
                    }
                    break;
                case "whitelist":
                    {
                        switch (auto.Data.Current?.Name)
                        {
                            case "username":
                                {
                                    Stopwatch s = new Stopwatch();
                                    s.Start();

                                    var names = MongoService.Whitelist.Find(x => true).FirstOrDefault()?.Usernames ?? new List<string>();

                                    if (string.IsNullOrEmpty(auto.Data.Current.Value?.ToString()))
                                    {
                                        await auto.RespondAsync(names.Select(x => new Discord.AutocompleteResult(x, x)).Take(20));
                                    }
                                    else
                                    {
                                        var orderedNames = names.OrderByDescending(x => Compute(x, (string)auto.Data.Current.Value));

                                        await auto.RespondAsync(orderedNames.Select(x => new Discord.AutocompleteResult(x, x)).Take(20));
                                    }

                                    s.Stop();

                                    Logger.Debug($"Autocomplete executed in {s.ElapsedMilliseconds}ms", Severity.Socket);
                                }
                                break;
                        }
                    }
                    break;
                case "satellite":
                    {
                        switch (auto.Data.Current?.Name)
                        {
                            case "target":
                                {
                                    Stopwatch s = new Stopwatch();
                                    s.Start();

                                    var names = _dynmapClient.CurrentPlayers.Select(x => x.Account);

                                    if (string.IsNullOrEmpty(auto.Data.Current.Value?.ToString()))
                                    {
                                        await auto.RespondAsync(names.Select(x => new Discord.AutocompleteResult(x, x)).Take(20));
                                    }
                                    else
                                    {
                                        var orderedNames = names.OrderByDescending(x => Compute(x, (string)auto.Data.Current.Value));

                                        await auto.RespondAsync(orderedNames.Select(x => new Discord.AutocompleteResult(x, x)).Take(20));
                                    }

                                    s.Stop();

                                    Logger.Debug($"Autocomplete executed in {s.ElapsedMilliseconds}ms", Severity.Socket);
                                }
                                break;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        ///     Compute the distance between two strings.
        /// </summary>
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}
