using Dynmap;
using Sputnik.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Handlers
{
    public class PlayerLoggingHandler
    {
        public PlayerLoggingHandler(DynmapClient client)
        {
            client.PlayersUpdated += Client_PlayersUpdated; 
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
