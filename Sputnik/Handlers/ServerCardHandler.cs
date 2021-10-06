using Discord;
using Discord.WebSocket;
using Dynmap;
using Dynmap.API;
using Newtonsoft.Json;
using Sputnik.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Handlers
{
    public class ServerCardHandler : DiscordHandler
    {
        public const string OfflineImageUrl = "https://emoji.gg/assets/emoji/offline.png";
        public const string OnlineImageUrl = "https://emoji.gg/assets/emoji/online.png";

        private DiscordSocketClient _discordClient;
        private DynmapClient _dynmapClient;
        private IUserMessage _statusCard;
        private TaskCompletionSource _clientConnectionSource;

        private SocketTextChannel StatusChannel
            => _discordClient.GetGuild(892543998495977493).GetTextChannel(894635126435250207);


        public override async Task InitializeAsync(DiscordSocketClient client, DynmapClient dynmap)
        {
            _discordClient = client;
            _dynmapClient = dynmap;
            _clientConnectionSource = new TaskCompletionSource();


            Task HandleInitClient(IReadOnlyDictionary<string, WorldState> a, IReadOnlyDictionary<string, WorldState> b)
            {
                _clientConnectionSource.SetResult();
                _dynmapClient.WorldStateUpdated -= HandleInitClient;

                _dynmapClient.WorldStateUpdated += _dynmapClient_WorldStateUpdated;

                return Task.CompletedTask;
            }

            _dynmapClient.WorldStateUpdated += HandleInitClient;

            await GetOrCreateStatusCardAsync();
        }

        private async Task _dynmapClient_WorldStateUpdated(IReadOnlyDictionary<string, WorldState> arg1, IReadOnlyDictionary<string, WorldState> arg2)
        {
            var status = await GetServerStatusAsync();
            var bytes = Convert.FromBase64String(status.Favicon.Replace("data:image/png;base64,", ""));
            var imageUrl = await GetOrCreateThumbnailImageAsync(bytes, "monke.lol").ConfigureAwait(false);

            var embed = BuildEmbed(imageUrl, await GetServerStatusAsync(), arg2);

            await _statusCard.ModifyAsync(x => x.Embed = embed.Build());
        }

        private async Task GetOrCreateStatusCardAsync()
        {
            await _clientConnectionSource.Task.ConfigureAwait(false);

            if (ConfigService.Config.StatusCardId.HasValue)
            {
                var msg = (IUserMessage) await StatusChannel.GetMessageAsync(ConfigService.Config.StatusCardId.Value).ConfigureAwait(false);

                if (msg == null)
                {
                    var conf = ConfigService.Config;

                    conf.StatusCardId = null;

                    ConfigService.SaveConfig(conf);

                    await GetOrCreateStatusCardAsync().ConfigureAwait(false);
                }
                else
                    _statusCard = msg;
            }
            else
            {

                var status = await GetServerStatusAsync();
                var bytes = Convert.FromBase64String(status.Favicon.Replace("data:image/png;base64,", ""));
                var imageUrl = await GetOrCreateThumbnailImageAsync(bytes, "monke.lol").ConfigureAwait(false);

                var embed = BuildEmbed(imageUrl, status, _dynmapClient.CurrentWorldStates);

                _statusCard = await StatusChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);

                var conf = ConfigService.Config;

                conf.StatusCardId = _statusCard.Id;

                ConfigService.SaveConfig(conf);
            }
        }
        
        private EmbedBuilder BuildEmbed(string imageUrl, ServerStatus info, IReadOnlyDictionary<string, WorldState> data = null)
        {
            data ??= _dynmapClient.CurrentWorldStates;

            return new EmbedBuilder()
                .WithTitle($"Jared anarchy")
                .WithThumbnailUrl(imageUrl)
                .WithDescription($"{info.Description.Text}")
                .AddField("Players", $"**{info.Players.Online}/{info.Players.Max}**\n\n{string.Join("\n\n", _dynmapClient.CurrentPlayers.Select(x => $"**{x.Account}**\n> {GetPlayerHealth(x)}\n> X: {x.X} Z: {x.Z}"))}")
                .AddField("Version", info.Version.Name)
                .WithColor(Discord.Color.Green);
        }

        private string GetPlayerHealth(Player plr)
        {
            string health = "";
            var pH = Math.Ceiling(plr.Health) / 2;

            for (int i = 0; i != 10; i++)
            {
                if (pH - i >= 1)
                    health += ":heart:";
                else if (pH - i > 0)
                    health += ":broken_heart:";
                else health += ":black_heart:";
            }

            return health;
        }

        private Task<ServerStatus> GetServerStatusAsync()
        {
            return MinecraftClient.GetAsync("monke.lol", 25565);
        }

        private async Task<string> GetOrCreateThumbnailImageAsync(byte[] image, string serverAddres)
        {
            var hash = BitConverter.ToString(MD5.Create().ComputeHash(image)).Replace("-", "");

            if (!File.Exists("./ServerThumbnails.json"))
                File.Create("./ServerThumbnails.json").Close();

            var json = File.ReadAllText("./ServerThumbnails.json");

            var icons = JsonConvert.DeserializeObject<List<ServerIcons>>(json == "" ? "[]" : json);

            var icon = icons.FirstOrDefault(x => x.ImageHash == hash);

            if(icon != null)
            {
                return icon.Url;
            }

            var link = await HapsyService.GetImageLinkAsync(image, $"{serverAddres}.png");

            icons.Add(new ServerIcons()
            {
                Url = link,
                Address = serverAddres,
                ImageHash = hash
            });

            File.WriteAllText("./ServerThumbnails.json", JsonConvert.SerializeObject(icons));

            return link;
        }

        private class ServerIcons
        {
            public string Address { get; set; }
            public string ImageHash { get; set; }
            public string Url { get; set; }
        }
    }
}
