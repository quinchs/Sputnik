using Discord.WebSocket;
using Dynmap;
using Sputnik.DataModels;
using Sputnik.Generation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Sputnik.Handlers
{
    public class CustomEmoteHandler : DiscordHandler
    {
        public const string EmoteDirectory = "./ColorEmotes";
        private DiscordSocketClient _discordClient;
        private SocketGuild _guild
            => _discordClient.GetGuild(892543998495977493);
        
        public override void Initialize(DiscordSocketClient client, DynmapClient dynmap)
        {
            if (!Directory.Exists(EmoteDirectory))
                Directory.CreateDirectory(EmoteDirectory);

            _discordClient = client;
        }

        public async Task<CustomEmote> CreateEmoteAsync(Color color)
        {
            var image = ImageGenerator.CreateColorImage(color);

            var fpath = EmoteDirectory + $"/{color.ToArgb():X}.png";
            image.Save(fpath, ImageFormat.Png);

            var emote = await _guild.CreateEmoteAsync($"{color.ToArgb():X}", new Discord.Image(fpath));

            var customEmote = new CustomEmote()
            {
                ARGB = color.ToArgb(),
                Id = emote.Id,
            };

            await MongoService.CustomEmotes.ReplaceOneAsync(x => x.ARGB == customEmote.ARGB, customEmote, new ReplaceOptions() { IsUpsert = true });

            return customEmote;
        }

        public async Task<CustomEmote> GetEmoteAsync(Color color)
        {
            var result = await MongoService.CustomEmotes.FindAsync(x => x.ARGB == color.ToArgb());

            return result.FirstOrDefault();
        }

        public async Task<CustomEmote> GetOrCreateEmoteAsync(Color color)
        {
            return await GetEmoteAsync(color) ?? await CreateEmoteAsync(color);
        }

        public async Task<CustomEmote> DeleteEmoteAsync(Color color)
        {
            var emote = await GetEmoteAsync(color);

            if (emote == null)
                return null;

            await _guild.DeleteEmoteAsync(_guild.Emotes.FirstOrDefault(x => x.Id == emote.Id));

            return emote;
        }
    }
}
