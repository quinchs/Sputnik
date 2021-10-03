using Discord;
using Discord.WebSocket;
using Dynmap;
using Sputnik.DataModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Handlers
{
    public class ChatLoggingHandler : DiscordHandler
    {
        private ConcurrentQueue<ulong> _chatMessages = new ConcurrentQueue<ulong>();
        private DiscordSocketClient _discordClient;
        private SocketTextChannel _logChannel
            => _discordClient.GetGuild(892543998495977493).GetTextChannel(894223652504088637);

        public override void Initialize(DiscordSocketClient client, DynmapClient dynmap)
        {
            _discordClient = client;
            dynmap.WorldStateUpdated += Dynmap_WorldStateUpdated;
        }

        private async Task Dynmap_WorldStateUpdated(IReadOnlyDictionary<string, Dynmap.API.WorldState> arg1, IReadOnlyDictionary<string, Dynmap.API.WorldState> arg2)
        {
            var updates = arg2.SelectMany(x => x.Value.Updates).ToArray();

            foreach(var chatEvent in updates.Where(x => x.Type == Dynmap.API.TypeEnum.Chat))
            {
                var model = new ChatMessage(chatEvent);

                if (_chatMessages.Contains(chatEvent.Timestamp))
                    continue;

                _chatMessages.Enqueue(chatEvent.Timestamp);

                while(_chatMessages.Count > 100)
                {
                    _chatMessages.TryDequeue(out var _);
                }
                
                await MongoService.ChatMessages.InsertOneAsync(model).ConfigureAwait(false);
                await _logChannel.SendMessageAsync($"**[{model.MinecraftAccount} {TimestampTag.FromDateTime(chatEvent.Timestamp.JavaTimeStampToDateTime())}]** - {model.Message}", allowedMentions: new Discord.AllowedMentions(Discord.AllowedMentionTypes.None)).ConfigureAwait(false);
            
            }

            foreach(var leaveEvent in updates.Where(x => x.Type == Dynmap.API.TypeEnum.PlayerQuit))
            {
                if (_chatMessages.Contains(leaveEvent.Timestamp))
                    continue;

                _chatMessages.Enqueue(leaveEvent.Timestamp);

                while (_chatMessages.Count > 25)
                {
                    _chatMessages.TryDequeue(out var _);
                }

                await _logChannel.SendMessageAsync($"**📤 {TimestampTag.FromDateTime(leaveEvent.Timestamp.JavaTimeStampToDateTime())} - {leaveEvent.Account} has left the game**").ConfigureAwait(false);
            }
            foreach(var joinEvent in updates.Where(x => x.Type == Dynmap.API.TypeEnum.PlayerJoin))
            {
                if (_chatMessages.Contains(joinEvent.Timestamp))
                    continue;

                _chatMessages.Enqueue(joinEvent.Timestamp);

                while (_chatMessages.Count > 25)
                {
                    _chatMessages.TryDequeue(out var _);
                }

                await _logChannel.SendMessageAsync($"**📥 {TimestampTag.FromDateTime(joinEvent.Timestamp.JavaTimeStampToDateTime())} - {joinEvent.Account} has joined the game**").ConfigureAwait(false);
            }
        }
    }
}
