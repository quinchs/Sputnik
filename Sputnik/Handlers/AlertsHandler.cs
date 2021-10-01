using Discord;
using Discord.WebSocket;
using Dynmap;
using MongoDB.Driver;
using Sputnik.DataModels;
using Sputnik.Generation;
using Sputnik.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Handlers
{
    public class AlertsHandler : DiscordHandler
    {
        public const string ActiveAlertsDir = "./ActiveAlerts";

        private DiscordSocketClient _discordClient;
        private DynmapClient _dynmapClient;

        public override void Initialize(DiscordSocketClient client, DynmapClient dynmap)
        {
            _discordClient = client;
            _dynmapClient = dynmap;

            if (!Directory.Exists(ActiveAlertsDir))
                Directory.CreateDirectory(ActiveAlertsDir);

            dynmap.PlayersUpdated += CheckAlerts;
        }

        private async Task CheckAlerts(IReadOnlyCollection<Dynmap.API.Player> arg)
        {
            var alertAreas = await MongoService.AlertCollection.Find(x => true).ToListAsync();
            var whitelistedPlayers = await MongoService.Whitelist.Find(x => true).ToListAsync();

            var unwhitelistedPlayers = arg.Where(x => !whitelistedPlayers.Any(y => y.Usernames.Contains(x.Account)));

            foreach(var area in alertAreas)
            {
                var activeAlert = await MongoService.ActiveAlerts.Find(x => x.AlertArea.Name == area.Name).FirstOrDefaultAsync();
                bool hasActiveAlert = activeAlert != null;

                var intersectingPlayers = unwhitelistedPlayers.Where(x => MathUtils.CalculateDistance(area.X, area.Z, x.X, x.Z) <= area.Radius);

                if (intersectingPlayers.Any() && !hasActiveAlert)
                {
                    // new alert
                    activeAlert = new ActiveAlert()
                    {
                        AlertArea = area,
                        MessageId = 0,
                        Positions = new Dictionary<string, AlertUser>(),
                    };

                    foreach(var p in intersectingPlayers)
                    {
                        var acc = new AlertUser()
                        {
                            Color = 0,
                            DateEntered = DateTime.UtcNow,
                            DateLeft = null,
                            Username = p.Account,
                            Positions = new List<UserCoordinates>()
                        };

                        acc.Positions.Add(new UserCoordinates(p));

                        activeAlert.Positions.Add(p.Account, acc);
                    }

                    var img = await CreateAlertImageAsync(activeAlert);

                    await SendAlertAsync(img, activeAlert);
                }
                else if(hasActiveAlert && intersectingPlayers.Any())
                {
                    // update the player positions
                    foreach(var player in intersectingPlayers)
                    {
                        var alertUser = activeAlert.Positions.GetValueOrDefault(player.Account) ?? new AlertUser()
                        {
                            Positions = new List<UserCoordinates>(),
                            DateEntered = DateTime.UtcNow,
                            Username = player.Account
                        };

                        alertUser.Positions.Add(new UserCoordinates(player));

                        activeAlert.Positions[player.Account] = alertUser;
                    }

                    var leftPlayers = activeAlert.Positions.Where(x => !intersectingPlayers.Any(y => y.Name == x.Key) && !x.Value.DateLeft.HasValue);

                    foreach(var l in leftPlayers)
                    {
                        l.Value.DateLeft = DateTime.UtcNow;
                        
                        activeAlert.Positions[l.Key] = l.Value;
                    }

                    foreach(var user in activeAlert.Positions.Where(x => !intersectingPlayers.Any(y => y.Name == x.Key)))
                    {
                        var pl = arg.FirstOrDefault(x => x.Account == user.Key);

                        if(pl != null)
                        {
                            user.Value.Positions.Add(new UserCoordinates(pl));

                            activeAlert.Positions[user.Key] = user.Value;
                        }
                    }

                    // update the alert

                    var image = await CreateAlertImageAsync(activeAlert);

                    await UpdateAlertAsync(image, activeAlert);
                }
                else if(!intersectingPlayers.Any() && hasActiveAlert)
                {
                    // close alert
                    var img = await CreateAlertImageAsync(activeAlert);
                    await CloseAlertAsync(img, activeAlert);
                }
            }
        }

        public async Task SendAlertAsync(AlertImageResult img, ActiveAlert alert)
        {
            SetColorsAsync(ref alert, img);
            await CreateOrUpdateEmotes();

            var map = GetColorMap(alert);

            var fname = ActiveAlertsDir + $"/alert-{alert.GetHashCode()}.png";

            img.Image.Save(fname, System.Drawing.Imaging.ImageFormat.Png);

            var channel = _discordClient.GetGuild(892543998495977493).GetTextChannel(ConfigService.Config.AlertsChannelId);

            var message = await channel.SendMessageAsync("", embed: new EmbedBuilder()
                .WithColor(Discord.Color.Orange)
                .WithTitle("Alert triggered")
                .WithDescription($"Alert area {alert.AlertArea.Name} has been triggered by {alert.Positions.Count} player{(alert.Positions.Count > 1 ? "s" : "")}!")
                .AddField("Area Details", $"X: {alert.AlertArea.X}\nZ: {alert.AlertArea.Z}\nR:{alert.AlertArea.Radius}\nOwner: <@{alert.AlertArea.Owner}>\nWorld: {alert.AlertArea.World}")
                .AddField("Colors", string.Join("\n", map.Select(x => $"")))
                .WithImageUrl($"attachments://{fname}")
                .Build()         
            );

            alert.MessageId = message.Id;

            await MongoService.ActiveAlerts.ReplaceOneAsync(x => x.MessageId == message.Id, alert, new ReplaceOptions() { IsUpsert = true });
        }

        public async Task UpdateAlertAsync(AlertImageResult img, ActiveAlert alert)
        {
            SetColorsAsync(ref alert, img);
            await CreateOrUpdateEmotes();

            var map = GetColorMap(alert);

            var fname = ActiveAlertsDir + $"/alert-{alert.GetHashCode()}.png";

            img.Image.Save(fname, System.Drawing.Imaging.ImageFormat.Png);

            var channel = _discordClient.GetGuild(892543998495977493).GetTextChannel(ConfigService.Config.AlertsChannelId);

            var message = await channel.GetMessageAsync(alert.MessageId) as IUserMessage;

            await message.ModifyAsync(x => x.Embed = new EmbedBuilder()
                .WithColor(Discord.Color.Orange)
                .WithTitle("Alert triggered")
                .WithDescription($"Alert area {alert.AlertArea.Name} has been triggered by {alert.Positions.Count} player{(alert.Positions.Count > 1 ? "s" : "")}!")
                .AddField("Area Details", $"X: {alert.AlertArea.X}\nZ: {alert.AlertArea.Z}\nR:{alert.AlertArea.Radius}\nOwner: <@{alert.AlertArea.Owner}>\nWorld: {alert.AlertArea.World}")
                .AddField("Colors", string.Join("\n", map.Select(x => $"<:{x.Value.ARGB}:{x.Value.Id}> - {x.Key}")))
                .AddField("Players", string.Join("\n\n", alert.Positions.Select(x => $"**{x.Key}**:\n> X: {x.Value.Positions.Last().X}\n> Z: {x.Value.Positions.Last().X}\n> Entered at {TimestampTag.FromDateTime(x.Value.DateEntered, TimestampTagStyles.Relative)}{(x.Value.DateLeft.HasValue ? $"\n> Date left: {TimestampTag.FromDateTime(x.Value.DateLeft.Value, TimestampTagStyles.Relative)}" : "")}")))
                .WithImageUrl($"attachments://{fname}")
                .Build());

            await MongoService.ActiveAlerts.ReplaceOneAsync(x => x.MessageId == message.Id, alert, new ReplaceOptions() { IsUpsert = true });
        }

        public async Task CloseAlertAsync(AlertImageResult img, ActiveAlert alert)
        {
            SetColorsAsync(ref alert, img);
            await CreateOrUpdateEmotes();

            var map = GetColorMap(alert);

            var fname = ActiveAlertsDir + $"/alert-{alert.GetHashCode()}.png";

            img.Image.Save(fname, System.Drawing.Imaging.ImageFormat.Png);

            var channel = _discordClient.GetGuild(892543998495977493).GetTextChannel(ConfigService.Config.AlertsChannelId);

            var message = await channel.GetMessageAsync(alert.MessageId) as IUserMessage;

            await message.ModifyAsync(x => x.Embed = new EmbedBuilder()
                .WithColor(Discord.Color.Green)
                .WithTitle("Alert cleared")
                .WithDescription($"Alert area {alert.AlertArea.Name} was triggered by {alert.Positions.Count} player{(alert.Positions.Count > 1 ? "s" : "")}!")
                .AddField("Area Details", $"X: {alert.AlertArea.X}\nZ: {alert.AlertArea.Z}\nR:{alert.AlertArea.Radius}\nOwner: <@{alert.AlertArea.Owner}>\nWorld: {alert.AlertArea.World}")
                .AddField("Colors", string.Join("\n", map.Select(x => $"")))
                .AddField("Players", string.Join("\n\n", alert.Positions.Select(x => $"**{x.Key}**:\n> X: {x.Value.Positions.Last().X}\n> Z: {x.Value.Positions.Last().X}\n> Entered at {TimestampTag.FromDateTime(x.Value.DateEntered, TimestampTagStyles.Relative)}{(x.Value.DateLeft.HasValue ? $"\n> Date left: {TimestampTag.FromDateTime(x.Value.DateLeft.Value, TimestampTagStyles.Relative)}" : "")}")))
                .WithImageUrl($"attachments://{fname}")
                .Build());

            await MongoService.ActiveAlerts.DeleteOneAsync(x => x.MessageId == alert.MessageId);
            RemoveColorEmotes(map.Select(x => x.Value));
        }

        private void RemoveColorEmotes(IEnumerable<CustomEmote> emotes)
        {
            _ = Task.Run(async () => 
            {
                var guild = _discordClient.GetGuild(892543998495977493);

                foreach(var emote in emotes)
                {
                    var m = guild.Emotes.FirstOrDefault(x => x.Id == emote.Id);
                    
                    if(m != null)
                    {
                        await guild.DeleteEmoteAsync(m);
                    }

                    await MongoService.CustomEmotes.DeleteOneAsync(x => x.Id == emote.Id);
                }
            });
        }

        private Dictionary<string, CustomEmote> GetColorMap(ActiveAlert alert)
        {
            var dict = new Dictionary<string, CustomEmote>();

            foreach(var p in alert.Positions)
            {
                var emote = MongoService.CustomEmotes.Find(x => x.ARGB == p.Value.Color).FirstOrDefault();

                if(emote != null)
                {
                    dict.Add(p.Key, emote);
                }
            }

            return dict;
        }

        public async Task CreateOrUpdateEmotes()
        {
            var handler = HandlerService.GetHandlerInstance<CustomEmoteHandler>();

            foreach(var alert in MongoService.ActiveAlerts.AsQueryable())
            {
                foreach(var pos in alert.Positions)
                {
                    if(pos.Value.Color != 0 && !MongoService.CustomEmotes.Find(x => x.ARGB == pos.Value.Color).Any())
                    {
                        await handler.CreateEmoteAsync(System.Drawing.Color.FromArgb(pos.Value.Color));
                    }
                }
            }
        }

        private void SetColorsAsync(ref ActiveAlert alert, AlertImageResult result)
        {
            foreach(var pos in alert.Positions.ToArray())
            {
                if (result.PlayerColors.TryGetValue(pos.Key, out var c))
                    pos.Value.Color = c.ToArgb();
            }
        }

        private async Task<AlertImageResult> CreateAlertImageAsync(ActiveAlert alert, bool drawNoHeads = false)
        {
            // generate the image
            var pos = alert.Positions.Select(x =>
            {
                var recentPos = x.Value.Positions.Last();
                var dist = MathUtils.CalculateDistance(recentPos.X, recentPos.Z, alert.AlertArea.X, alert.AlertArea.Z);

                return (drawNoHeads ? true : dist < alert.AlertArea.Radius, x.Value.Positions);
            }).ToList();

            var colors = alert.Positions.Where(x => x.Value.Color != 0).Select(x => new KeyValuePair<string, System.Drawing.Color>(x.Key, System.Drawing.Color.FromArgb(x.Value.Color)));

            return await ImageGenerator.CreateAlertAsync(alert.AlertArea, alert.AlertArea.Radius, pos, new Dictionary<string, System.Drawing.Color>(colors));
        }
    }
}
