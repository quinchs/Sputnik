using Discord;
using Discord.WebSocket;
using Dynmap;
using MongoDB.Driver;
using Sputnik.DataModels;
using Sputnik.Generation;
using Sputnik.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sputnik.Handlers
{
    public class AlertsHandler : DiscordHandler
    {
        public const string ActiveAlertsDir = "./ActiveAlerts";

        private DiscordSocketClient _discordClient;
        private DynmapClient _dynmapClient;
        private static int padding;
        private SemaphoreSlim _semaphoreSlim;

        private ButtonBuilder _refreshButton = new ButtonBuilder()
            .WithLabel("Refresh")
            .WithCustomId("alert_reshed")
            .WithStyle(ButtonStyle.Primary);


        private ButtonBuilder _resovleButton = new ButtonBuilder()
            .WithLabel("Resolve alert")
            .WithCustomId("resolve_alert")
            .WithStyle(ButtonStyle.Danger);
            

        public override void Initialize(DiscordSocketClient client, DynmapClient dynmap)
        {
            _discordClient = client;
            _dynmapClient = dynmap;

            if (!Directory.Exists(ActiveAlertsDir))
                Directory.CreateDirectory(ActiveAlertsDir);

            _semaphoreSlim = new SemaphoreSlim(1, 1);

            dynmap.PlayersUpdated += CheckAlerts;

            client.ButtonExecuted += Client_ButtonExecuted;
        }

        private async Task Client_ButtonExecuted(SocketMessageComponent arg)
        {
            await arg.DeferAsync();
            switch (arg.Data.CustomId)
            {
                case "refresh_alert":
                    {
                        var alert = (await MongoService.ActiveAlerts.FindAsync(x => x.MessageId == arg.Message.Id).ConfigureAwait(false)).FirstOrDefault();

                        if (alert == null)
                            return;

                        var image = await CreateAlertImageAsync(alert).ConfigureAwait(false);
                        await UpdateAlertAsync(image, alert, true).ConfigureAwait(false);
                    }
                    break;
                case "resolve_alert":
                    {
                        var alert = (await MongoService.ActiveAlerts.FindAsync(x => x.MessageId == arg.Message.Id).ConfigureAwait(false)).FirstOrDefault();

                        if (alert == null)
                            return;

                        var image = await CreateAlertImageAsync(alert).ConfigureAwait(false);
                        await CloseAlertAsync(image, alert, arg.User.Id).ConfigureAwait(false);
                    }
                    break;
            }
        }

        private async Task CheckAlerts(IReadOnlyCollection<Dynmap.API.Player> arg)
        {
            await _semaphoreSlim.WaitAsync();

            Interlocked.Add(ref padding, 1);

            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var alertAreas = await MongoService.AlertCollection.Find(x => true).ToListAsync().ConfigureAwait(false);
                var whitelistedPlayers = await MongoService.Whitelist.Find(x => true).ToListAsync().ConfigureAwait(false);

                var unwhitelistedPlayers = arg.Where(x => !whitelistedPlayers.Any(y => y.Usernames.Contains(x.Account)));

                foreach (var area in alertAreas)
                {
                    var activeAlert = await MongoService.ActiveAlerts.Find(x => x.AlertArea.Name == area.Name).FirstOrDefaultAsync().ConfigureAwait(false);
                    bool hasActiveAlert = activeAlert != null;

                    var intersectingPlayers = unwhitelistedPlayers.Where(x => MathUtils.CalculateDistance(area.X, area.Z, (int)x.X, (int)x.Z) <= area.Radius);

                    if (intersectingPlayers.Any() && !hasActiveAlert)
                    {
                        // new alert
                        activeAlert = new ActiveAlert()
                        {
                            AlertArea = area,
                            MessageId = 0,
                            Positions = new Dictionary<string, AlertUser>(),
                        };

                        foreach (var p in intersectingPlayers)
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

                        var img = await CreateAlertImageAsync(activeAlert).ConfigureAwait(false);

                        await SendAlertAsync(img, activeAlert).ConfigureAwait(false);
                    }
                    else if (hasActiveAlert && intersectingPlayers.Any())
                    {
                        // update the player positions
                        foreach (var player in intersectingPlayers)
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

                        foreach (var l in leftPlayers)
                        {
                            l.Value.DateLeft = DateTime.UtcNow;

                            activeAlert.Positions[l.Key] = l.Value;
                        }

                        foreach (var user in activeAlert.Positions.Where(x => !intersectingPlayers.Any(y => y.Name == x.Key)))
                        {
                            var pl = arg.FirstOrDefault(x => x.Account == user.Key);

                            if (pl != null)
                            {
                                user.Value.Positions.Add(new UserCoordinates(pl));

                                activeAlert.Positions[user.Key] = user.Value;
                            }
                        }

                        // update the alert

                        var image = await CreateAlertImageAsync(activeAlert).ConfigureAwait(false);

                        await UpdateAlertAsync(image, activeAlert).ConfigureAwait(false);
                    }
                    else if (!intersectingPlayers.Any() && hasActiveAlert)
                    {
                        // close alert
                        var img = await CreateAlertImageAsync(activeAlert).ConfigureAwait(false);
                        await CloseAlertAsync(img, activeAlert).ConfigureAwait(false);
                    }
                }

                sw.Stop();

                Logger.Write($"Alert handler took {sw.ElapsedMilliseconds}ms to execute", new Severity[] { Severity.Core, Severity.Debug }, nameof(AlertsHandler));
            }
            catch(Exception x)
            {
                Logger.Write(x, new Severity[] { Severity.Core, Severity.Error }, nameof(AlertsHandler));
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task SendAlertAsync(AlertImageResult img, ActiveAlert alert)
        {
            SetColorsAsync(ref alert, img);
            await CreateOrUpdateEmotes(alert).ConfigureAwait(false);

            var map = GetColorMap(alert);

            var fname = Path.GetFullPath(ActiveAlertsDir + $"/alert-{alert.GetHashCode()}.png");

            img.Image.Save(fname, System.Drawing.Imaging.ImageFormat.Png);

            var channel = _discordClient.GetGuild(892543998495977493).GetTextChannel(ConfigService.Config.AlertsChannelId);

            var imageLink = await GetImageUrl(fname, ref alert, null).ConfigureAwait(false);

            var message = await channel.SendMessageAsync(
                embed: new EmbedBuilder()
                    .WithColor(Discord.Color.Orange)
                    .WithTitle("Alert triggered")
                    .WithDescription($"Alert area {alert.AlertArea.Name} has been triggered by {alert.Positions.Count} player{(alert.Positions.Count > 1 ? "s" : "")}!")
                    .AddField("Area Details", $"X: {alert.AlertArea.X}\nZ: {alert.AlertArea.Z}\nR: {alert.AlertArea.Radius}\nOwner: <@{alert.AlertArea.Owner}>\nWorld: {alert.AlertArea.World}")
                    .AddField("Colors", string.Join("\n", map.Select(x => $"<:{x.Value.ARGB:X}:{x.Value.Id}> - {x.Key}")))
                    .AddField("Players", string.Join("\n\n", alert.Positions.Select(x => $"**{x.Key}**:\n> X: {x.Value.Positions.Last().X}\n> Z: {x.Value.Positions.Last().Z}\n> Entered at {TimestampTag.FromDateTime(x.Value.DateEntered, TimestampTagStyles.Relative)}{(x.Value.DateLeft.HasValue ? $"\n> Date left: {TimestampTag.FromDateTime(x.Value.DateLeft.Value, TimestampTagStyles.Relative)}" : "")}")))
                    .WithImageUrl(imageLink)
                    .Build(),
                component: new ComponentBuilder()
                    .WithButton(_refreshButton)
                    .WithButton(_resovleButton)
                    .Build()
            ).ConfigureAwait(false);

            alert.MessageId = message.Id;

            await MongoService.ActiveAlerts.ReplaceOneAsync(x => x.MessageId == message.Id, alert, new ReplaceOptions() { IsUpsert = true }).ConfigureAwait(false);
        }

        public async Task UpdateAlertAsync(AlertImageResult img, ActiveAlert alert, bool refreshImage = false)
        {
            SetColorsAsync(ref alert, img);
            await CreateOrUpdateEmotes(alert).ConfigureAwait(false);

            var map = GetColorMap(alert);

            var fname = Path.GetFullPath(ActiveAlertsDir + $"/alert-{alert.GetHashCode()}.png");

            img.Image.Save(fname, System.Drawing.Imaging.ImageFormat.Png);

            var channel = _discordClient.GetGuild(892543998495977493).GetTextChannel(ConfigService.Config.AlertsChannelId);

            var message = await channel.GetMessageAsync(alert.MessageId).ConfigureAwait(false) as IUserMessage;

            var imageLink = await GetImageUrl(fname, ref alert, refreshImage ? null : message).ConfigureAwait(false);

            await message.ModifyAsync(x => x.Embed = new EmbedBuilder()
                .WithColor(Discord.Color.Orange)
                .WithTitle("Alert triggered")
                .WithDescription($"Alert area {alert.AlertArea.Name} has been triggered by {alert.Positions.Count} player{(alert.Positions.Count > 1 ? "s" : "")}!")
                .AddField("Area Details", $"X: {alert.AlertArea.X}\nZ: {alert.AlertArea.Z}\nR: {alert.AlertArea.Radius}\nOwner: <@{alert.AlertArea.Owner}>\nWorld: {alert.AlertArea.World}")
                .AddField("Colors", string.Join("\n", map.Select(x => $"<:{x.Value.ARGB:X}:{x.Value.Id}> - {x.Key}")))
                .AddField("Players", string.Join("\n\n", alert.Positions.Select(x => $"**{x.Key}**:\n> X: {x.Value.Positions.Last().X}\n> Z: {x.Value.Positions.Last().Z}\n> Entered at {TimestampTag.FromDateTime(x.Value.DateEntered, TimestampTagStyles.Relative)}{(x.Value.DateLeft.HasValue ? $"\n> Date left: {TimestampTag.FromDateTime(x.Value.DateLeft.Value, TimestampTagStyles.Relative)}" : "")}")))
                .WithImageUrl(imageLink)
                .Build()).ConfigureAwait(false);

            await MongoService.ActiveAlerts.ReplaceOneAsync(x => x.MessageId == message.Id, alert, new ReplaceOptions() { IsUpsert = true }).ConfigureAwait(false);
        }

        public async Task CloseAlertAsync(AlertImageResult img, ActiveAlert alert, ulong? closedBy = null)
        {
            SetColorsAsync(ref alert, img);
            await CreateOrUpdateEmotes(alert).ConfigureAwait(false);

            var map = GetColorMap(alert);

            var fname = Path.GetFullPath(ActiveAlertsDir + $"/alert-{alert.GetHashCode()}.png");

            img.Image.Save(fname, System.Drawing.Imaging.ImageFormat.Png);

            var channel = _discordClient.GetGuild(892543998495977493).GetTextChannel(ConfigService.Config.AlertsChannelId);

            var message = await channel.GetMessageAsync(alert.MessageId).ConfigureAwait(false) as IUserMessage;

            var imageLink = await GetImageUrl(fname, ref alert, message).ConfigureAwait(false);

            await message.ModifyAsync(x =>
            {
                x.Embed = new EmbedBuilder()
                    .WithColor(Discord.Color.Green)
                    .WithTitle("Alert cleared")
                    .WithDescription($"{(closedBy.HasValue ? $"Closed by <@{closedBy.Value}>\n" : "")}Alert area {alert.AlertArea.Name} was triggered by {alert.Positions.Count} player{(alert.Positions.Count > 1 ? "s" : "")}!")
                    .AddField("Area Details", $"X: {alert.AlertArea.X}\nZ: {alert.AlertArea.Z}\nR: {alert.AlertArea.Radius}\nOwner: <@{alert.AlertArea.Owner}>\nWorld: {alert.AlertArea.World}")
                    .AddField("Colors", string.Join("\n", map.Select(x => $"<:{x.Value.ARGB:X}:{x.Value.Id}> - {x.Key}")))
                    .AddField("Players", string.Join("\n\n", alert.Positions.Select(x => $"**{x.Key}**:\n> X: {x.Value.Positions.Last().X}\n> Z: {x.Value.Positions.Last().Z}\n> Entered {TimestampTag.FromDateTime(x.Value.DateEntered, TimestampTagStyles.Relative)}\n> Left: {TimestampTag.FromDateTime(x.Value.DateLeft.GetValueOrDefault(DateTime.UtcNow), TimestampTagStyles.Relative)}")))
                    .WithImageUrl(imageLink)
                    .Build();
                x.Components = new ComponentBuilder()
                    .WithButton(_refreshButton.WithDisabled(true))
                    .WithButton(_resovleButton.WithDisabled(true))
                    .Build();
            });

            await MongoService.ActiveAlerts.DeleteOneAsync(x => x.MessageId == alert.MessageId).ConfigureAwait(false);
            RemoveColorEmotes(map.Select(x => x.Value));
        }

        private Task<string> GetImageUrl(string fPath, ref ActiveAlert alert, IUserMessage msg)
        {
            if (msg != null && (DateTime.UtcNow - alert.LastUpdateImage).TotalSeconds > 10)
            {
                return Task.FromResult(msg.Embeds.First().Image.Value.Url);
            }
            else
            {
                alert.LastUpdateImage = DateTime.UtcNow;
                return HapsyService.GetImageLinkAsync(fPath);
            }

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
                        await guild.DeleteEmoteAsync(m).ConfigureAwait(false);
                    }

                    await MongoService.CustomEmotes.DeleteOneAsync(x => x.Id == emote.Id).ConfigureAwait(false);
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

        public async Task CreateOrUpdateEmotes(ActiveAlert al = null)
        {
            var handler = HandlerService.GetHandlerInstance<CustomEmoteHandler>();

            List<ActiveAlert> alerts = new List<ActiveAlert>(MongoService.ActiveAlerts.AsQueryable().ToList());
            alerts.Add(al);

            foreach(var alert in alerts)
            {
                foreach(var pos in alert.Positions)
                {
                    if(pos.Value.Color != 0 && !MongoService.CustomEmotes.Find(x => x.ARGB == pos.Value.Color).Any())
                    {
                        await handler.CreateEmoteAsync(System.Drawing.Color.FromArgb(pos.Value.Color)).ConfigureAwait(false);
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

            return await ImageGenerator.CreateAlertAsync(alert.AlertArea, alert.AlertArea.Radius, pos, new Dictionary<string, System.Drawing.Color>(colors)).ConfigureAwait(false);
        }
    }
}
