using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using Sputnik.DataModels;
using Sputnik.Generation;
using Sputnik.Handlers;
using Sputnik.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Modules
{
    public class CommandModule : DualPurposeModuleBase
    {
        private CustomEmoteHandler _emoteHandler
            => HandlerService.GetHandlerInstance<CustomEmoteHandler>();

        #region Alerts

        [Command("alerts add")]
        public async Task AddAlertCoords(string name, string dimention, int x, int z, int radius)
        {
            var current = await MongoService.AlertCollection.Find(x => x.Name == name).FirstOrDefaultAsync();

            if(current != null)
            {
                await ReplyAsync($"An alert already exists with the name \"{name}\"!", ephemeral: true);
                return;
            }

            var model = new CoordAlerts()
            {
                Name = name,
                X = x,
                Z = z,
                Radius = radius,
                Owner = Context.User.Id,
                World = dimention
            };

            await DeferAsync(false);

            await MongoService.AlertCollection.InsertOneAsync(model);

            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(Color.Green)
                .WithAuthor(Context.User)
                .WithTitle("Success")
                .WithDescription($"I've added {name} to the list of coordinates to watch for and will alert you when someone enters them.")
                .Build()
            );
        }

        [Command("alerts list")]
        public async Task AlertsList()
        {
            await DeferAsync(false);

            var alerts = await (await MongoService.AlertCollection.FindAsync(x => true)).ToListAsync();

            var markers = await Program.DynmapClient.Rest.GetMarkersAsync().ConfigureAwait(false);

            var border = markers.Sets.Markers.Areas.First().Value;

            using var alertImage = await ImageGenerator.CreateAlertAreaMapAsync(alerts, Math.Abs((int)border.X[0] - (int)border.X[1]));

            Dictionary<string, CustomEmote> emotes = new();

            foreach(var c in alertImage.Colors)
            {
                var em = await _emoteHandler.GetOrCreateEmoteAsync(c.Value).ConfigureAwait(false);
                emotes.Add(c.Key, em);
            }

            if (!Directory.Exists("./AlertsList"))
                Directory.CreateDirectory("./AlertsList");

            var fname = $"./AlertsList/alert-list-{DateTime.UtcNow.Ticks}.png";

            alertImage.Image.Save(fname, System.Drawing.Imaging.ImageFormat.Png);

            var link = await HapsyService.GetImageLinkAsync(fname).ConfigureAwait(false);

            var alertFields = alerts.Take(25).Select(x => new EmbedFieldBuilder().WithName($"{x.Name}").WithValue($"Color: <:{emotes[x.Name].ARGB:X}:{emotes[x.Name].EmoteId}>\nX: {x.X}\nY: {x.Z}\nR: {x.Radius}\nOwner: <@{x.Owner}>"));

            var embed = new EmbedBuilder()
                .WithTitle("Alerts")
                .WithDescription(alerts.Count > 0 ? "Here's a list of the current alert areas" : "There are no current alert areas.")
                .WithColor(Color.Green)
                .WithImageUrl($"attachment://alert-map.png")
                .WithFields(alertFields);


            if (Context.IsInteraction)
            {
                await Context.Interaction.FollowupWithFileAsync(filePath: Path.GetFullPath(fname), fileName: "alert-map.png", embed: embed.Build());
            }
            else
            {
                await Context.Channel.SendFileAsync(fname, embed: embed.Build());
            }
            
        }

        [Command("alerts remove")]
        public async Task AlertsRemove(string name)
        {
            var item = MongoService.AlertCollection.Find(x => x.Name == name);

            if (!item.Any())
            {
                await ReplyAsync($"No alerts found with the name of \"{name}\"", ephemeral: true);
                return;
            }

            var alert = item.FirstOrDefault();

            if(alert.Owner != Context.User.Id)
            {
                await ReplyAsync($"You cannot remove <@{alert.Owner}>'s alert!", ephemeral: true);
                return;
            }

            await DeferAsync();

            await MongoService.AlertCollection.DeleteOneAsync(x => x.Name == alert.Name);

            await ReplyAsync(embed: new EmbedBuilder()
                .WithTitle("Success!")
                .WithDescription($"I've remove the alert {name} from my database!")
                .WithColor(Color.Green)
                .Build()
            );
        }

        #endregion

        #region Whitelist

        [Command("whitelist add")]
        public async Task AddWhitelist(string username)
        {
            var whitelist = MongoService.Whitelist.Find(x => true).FirstOrDefault() ?? new Whitelist() { Usernames = new List<string>() };

            if (whitelist.Usernames.Contains(username))
            {
                await ReplyAsync($"{username} is already whitelisted!", ephemeral: true);
            }
            else
            {
                whitelist.Usernames.Add(username);

                await MongoService.Whitelist.ReplaceOneAsync(x => x.Id == whitelist.Id, whitelist, new ReplaceOptions() { IsUpsert = true });

                await ReplyAsync($"{username} has been added to the whitelist!");
            }
        }

        [Command("whitelist list")]
        public async Task WhitelistList()
        {
            var whitelist = MongoService.Whitelist.Find(x => true).FirstOrDefault() ?? new Whitelist() { Usernames = new List<string>() };

            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("Whitelist")
                .WithDescription(whitelist.Usernames.Count == 0 ? "There are no current users whitelisted" : $"Current whitelisted users:\n{string.Join("\n", whitelist.Usernames.Select(x => $"> {x}"))}")
                .Build()
            );
        }

        [Command("whitelist remove")]
        public async Task WhitelistRemove(string username)
        {
            var whitelist = MongoService.Whitelist.Find(x => true).FirstOrDefault() ?? new Whitelist() { Usernames = new List<string>() };

            if(!whitelist.Usernames.Contains(username))
            {
                await ReplyAsync($"{username} is not on the whitelist", ephemeral: true);
                return;
            }

            whitelist.Usernames.Remove(username);

            await MongoService.Whitelist.ReplaceOneAsync(x => x.Id == whitelist.Id, whitelist, new ReplaceOptions() { IsUpsert = true });

            await ReplyAsync($"{username} has been removed from the whitelist");
        }

        #endregion

        #region Sat Lookup

        [Command("satellite track")]
        public async Task SatLookup(string target, string world = null)
        {
            if (world == "")
                world = null;

            if(!Program.DynmapClient.CurrentPlayers.Any(x => x.Name == target))
            {
                await ReplyAsync($"Cannot find player {target}!");
                return;
            }

            await DeferAsync();

            using var trackResult = await ImageGenerator.GetPlayerSatelliteImageAsync(target, world).ConfigureAwait(false);

            if(trackResult.Image == null)
            {
                await ReplyAsync($"Cannot track {target}.");
                return;
            }

            var fPath = $"./SatTracks/track-{target}-{trackResult.GetHashCode()}.png";

            if (!Directory.Exists("./SatTracks"))
                Directory.CreateDirectory("./SatTracks");

            trackResult.Image.Save(fPath);

            Dictionary<string, CustomEmote> emotes = new();

            foreach (var c in trackResult.Colors)
            {
                var em = await _emoteHandler.GetOrCreateEmoteAsync(c.Value).ConfigureAwait(false);
                emotes.Add(c.Key, em);
            }

            var embed = new EmbedBuilder()
                .WithTitle($"Track results of {target}")
                .WithColor(Color.Green)
                .WithDescription($"Image radius: {trackResult.BlockRadius} blocks\nCenter position: X: {trackResult.Center.X} Z: {trackResult.Center.Y}\nTrack duration: 2 minutes")
                .AddField("Player key", $"{string.Join("\n", emotes.Select(x => $"> <:{x.Value.ARGB:X}:{x.Value.EmoteId}> - {x.Key}"))}")
                .WithImageUrl($"attachment://track-{target}-{trackResult.GetHashCode()}.png");

            if (Context.IsInteraction)
            {
                await Context.Interaction.FollowupWithFileAsync(fPath, embed: embed.Build());
            }
            else
            {
                await Context.Channel.SendFileAsync(fPath, embed: embed.Build());
            }
        }

        [Command("satellite image")]
        public async Task SatelliteImage(int x, int z, int radius, string world = "world")
        {
            if(!Program.DynmapClient.Worlds.Any(x => x.Name == world))
            {
                await ReplyAsync($"World \"{world}\" not found!", ephemeral: true);
                return;
            }

            await DeferAsync();
            
            var result = await ImageGenerator.GetBackgroundAsync(x, z, radius, world);

            result.Image.Dispose();

            var embed = new EmbedBuilder()
                .WithTitle("Image result")
                .WithColor(Color.Green)
                .WithDescription($"Heres the map at X: {x} Z: {z} in the world \"{world}\"")
                .WithImageUrl($"attachment://{world}_{x}_{z}_{radius}.png.png");

            if (Context.IsInteraction)
            {
                await Context.Interaction.FollowupWithFileAsync($"./BackgroundsCache/{world}_{x}_{z}_{radius}.png", embed: embed.Build());
            }
            else
            {
                await Context.Channel.SendFileAsync($"./BackgroundsCache/{world}_{x}_{z}_{radius}.png", embed: embed.Build());
            }

            result.Image.Dispose();
        }

        #endregion
    }
}
