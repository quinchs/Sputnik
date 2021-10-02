using Discord.WebSocket;
using Dynmap;
using Sputnik.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Handlers
{
    public class PlayerLoggingHandler : DiscordHandler
    {
        public override void Initialize(DiscordSocketClient client, DynmapClient dynmap)
        {
            dynmap.PlayersUpdated += Client_PlayersUpdated;
        }

        private async Task Client_PlayersUpdated(IReadOnlyCollection<Dynmap.API.Player> arg)
        { 
            foreach(var player in arg)
            {
                var model = new UserCoordinates(player);

                await MongoService.UserCoordinatesCollection.InsertOneAsync(model);
            }
        }
    }
}
