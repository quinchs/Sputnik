using Discord;
using Discord.Commands;
using MongoDB.Driver;
using Sputnik.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Modules
{
    public class CommandModule : DualPurposeModuleBase
    {
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

            var alertFields = alerts.Take(25).Select(x => new EmbedFieldBuilder().WithName($"{x.Name}").WithValue($"X:{x.X}\nY:{x.Z}\nR:{x.Radius}\nOwner: <@{x.Owner}>"));

            var embed = new EmbedBuilder()
                .WithTitle("Alerts")
                .WithDescription(alerts.Count > 0 ? "Here's a list of the current alert areas" : "There are no current alert areas.")
                .WithColor(Color.Green)
                .WithFields(alertFields);


            await ReplyAsync(embed: embed.Build());
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
    }
}
