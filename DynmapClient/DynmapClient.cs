using Dynmap.API;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Dynmap
{
    public class DynmapClient
    {
        public event Func<IReadOnlyDictionary<string, WorldState>, IReadOnlyDictionary<string, WorldState>, Task> WorldStateUpdated;
        public event Func<IReadOnlyCollection<Player>, Task> PlayersUpdated;
        public event Func<Player, Task> PlayerJoined;
        public event Func<Player, Task> PlayerLeft;
        public event Func<string, Task> Log;

        public Configuration ServerConfiguration { get; private set; }
        public IReadOnlyCollection<World> Worlds
            => ServerConfiguration.Worlds.ToImmutableArray();
        public IReadOnlyDictionary<string, WorldState> CurrentWorldStates
            => _worldStates;
        public IReadOnlyCollection<Player> CurrentPlayers
            => CurrentWorldStates.SelectMany(x => x.Value.Players).GroupBy(x => x.Account).Select(x => x.First()).ToImmutableArray();

        public readonly RestClient Rest;

        private Dictionary<string, WorldState> _worldStates = new Dictionary<string, WorldState>();
        private Dictionary<string, ulong> _timestamps = new Dictionary<string, ulong>();

        private readonly DynmapClientConfig _config;

        private bool _connected = false;

        private object _lock = new object();

        private Task _poolTask;

        public DynmapClient(string baseUri)
            : this(new DynmapClientConfig() { Uri = baseUri }) { }

        public DynmapClient(DynmapClientConfig config)
        {
            Rest = new RestClient(config.Uri, this);
            _config = config;
        }

        public async Task ConnectAsync()
        {
            this.ServerConfiguration = await Rest.GetConfigurationAsync();

            foreach(var world in ServerConfiguration.Worlds)
            {
                _timestamps.Add(world.Name, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            _connected = true;

            _poolTask = Task.Factory.StartNew(async () => await GetWorldStatesAsync());
        }

        public async Task DisconnectAsync()
        {
            _connected = false;
            await _poolTask;
        }

        private async Task GetWorldStatesAsync()
        {
            bool con = _connected;
            while (con)
            {
                try {
                    Dictionary<string, WorldState> worldStates = new();
                    foreach (var world in ServerConfiguration.Worlds)
                    {
                        var state = await Rest.GetWorldStateAsync(world.Name, _timestamps[world.Name]);
                        _timestamps[world.Name] = state.Timestamp;
                        worldStates.Add(world.Name, state);
                    }

                    var oldStates = _worldStates;

                    _worldStates = worldStates;

                    _ = Task.Run(async () =>
                    {
                        if (WorldStateUpdated != null)
                            await WorldStateUpdated?.Invoke(oldStates, worldStates);
                    });

                    var oldPlayersState = oldStates.SelectMany(x => x.Value.Players).GroupBy(x => x.Account).Select(x => x.First());
                    var newPlayersState = worldStates.SelectMany(x => x.Value.Players).GroupBy(x => x.Account).Select(x => x.First());

                    var newPlayers = newPlayersState.Where(x => !oldPlayersState.Any(y => y.Name != x.Name));
                    var oldPlayers = oldPlayersState.Where(x => !newPlayersState.Any(y => y.Name != x.Name));

                    if (newPlayers.Any())
                    {
                        foreach (var p in newPlayers)
                        {
                            _ = Task.Run(async () =>
                            {
                                if (PlayerJoined != null)
                                    await PlayerJoined.Invoke(p);
                            });
                        }
                    }

                    if (oldPlayers.Any())
                    {
                        foreach (var p in oldPlayers)
                        {
                            _ = Task.Run(async () =>
                            {
                                if (PlayerLeft != null)
                                    await PlayerLeft.Invoke(p);
                            });
                        }
                    }

                    if (newPlayersState.Any() && PlayersUpdated != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var t = PlayersUpdated.Invoke(newPlayersState.ToImmutableArray());
                                await t;

                                if (t.Exception != null)
                                    LogInternal($"Exception on handler: {t.Exception}");
                            }
                            catch (Exception x)
                            {
                                LogInternal($"Exception on handler: {x}");
                            }
                        });
                    }
                }
                catch(Exception x)
                {
                    LogInternal($"Exception on main loop: {x}");
                }
                finally
                {
                    await Task.Delay((int)ServerConfiguration.Updaterate);

                    con = _connected;
                }
            }
        }

        internal void LogInternal(string log)
        {
            _ = Task.Run(async () =>
            {
                await Log?.Invoke(log);
            });
        }
    }
}
