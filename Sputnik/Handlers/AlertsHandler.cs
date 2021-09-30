using Discord.WebSocket;
using Dynmap;
using MongoDB.Driver;
using Sputnik.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Handlers
{
    public class AlertsHandler : DiscordHandler
    {
        public override void Initialize(DiscordSocketClient client, DynmapClient dynmap)
        {
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
                }
                else
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


                }
            }
        }
    }
}
