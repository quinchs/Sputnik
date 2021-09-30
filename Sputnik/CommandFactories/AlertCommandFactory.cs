using Discord;
using Sputnik.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.CommandFactories
{
    public class AlertCommandFactory : ApplicationCommandFactory
    {
        [GuildSpecificCommand(892543998495977493)]
        public override IEnumerable<ApplicationCommandProperties> BuildCommands()
        {
            var alertCommand = new SlashCommandBuilder()
                .WithName("alerts")
                .WithDescription("Manage alerts with this command.")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("add")
                    .WithDescription("Adds a set of coords to alert for")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("name", ApplicationCommandOptionType.String, "The name of the location", true)
                    .AddOption("dimention", ApplicationCommandOptionType.String, "The dimention of the location", true, choices: new ApplicationCommandOptionChoiceProperties[] 
                    { 
                        new ApplicationCommandOptionChoiceProperties()
                        {
                            Name = "overworld",
                            Value = "world"
                        },
                        new ApplicationCommandOptionChoiceProperties()
                        {
                            Name = "nether",
                            Value = "world_nether"
                        },
                        new ApplicationCommandOptionChoiceProperties()
                        {
                            Name = "end",
                            Value = "world_the_end"
                        }
                    })
                    .AddOption("x", ApplicationCommandOptionType.Integer, "The X Coordinate", true)
                    .AddOption("z", ApplicationCommandOptionType.Integer, "The Z Coordinate", true)
                    .AddOption("radius", ApplicationCommandOptionType.Integer, "The radius/distance that a player has to be within to trigger the alert", true)
                )
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("list")
                    .WithDescription("Lists the current alert areas")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                )
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("remove")
                    .WithDescription("Removes a alert from the list of alerts")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("name", ApplicationCommandOptionType.String, "The name of alert to remove", true, isAutocomplete: true)
            ).Build();

            return new ApplicationCommandProperties[] { alertCommand };
        }
    }
}
