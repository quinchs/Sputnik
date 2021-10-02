using Discord;
using Sputnik.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.CommandFactories
{
    public class SlashCommandFactory : ApplicationCommandFactory
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

            var whitelistCommand = new SlashCommandBuilder()
                .WithName("whitelist")
                .WithDescription("List or modify the witelist for alerts")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("add")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithDescription("Adds a user to the whitelist")
                    .AddOption("username", ApplicationCommandOptionType.String, "The username of the user to add", true)
                )
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("remove")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithDescription("Removes a user from the whitelist")
                    .AddOption("username", ApplicationCommandOptionType.String, "The user to remove from the whitelist", true, isAutocomplete: true)
                )
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("list")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithDescription("Lists the current users in the whitelist")
                ).Build();

            var trackCommand = new SlashCommandBuilder()
                .WithName("satellite")
                .WithDescription("Execute tasks as the satellite")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("track")
                    .WithDescription("Tracks a given user")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("target", ApplicationCommandOptionType.String, "The user to track", true, isAutocomplete: true)
                    .AddOption("world", ApplicationCommandOptionType.String, "The world to track the user in, defaults to their current world", false, choices: new ApplicationCommandOptionChoiceProperties[]
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
                )
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("image")
                    .WithDescription("Images a certian part of the map")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("x", ApplicationCommandOptionType.Integer, "The X coordinate to image", true)
                    .AddOption("z", ApplicationCommandOptionType.Integer, "The Z coordinate to image", true)
                    .AddOption("radius", ApplicationCommandOptionType.Integer, "The radius (in blocks) of the image to take", true)
                    .AddOption("world", ApplicationCommandOptionType.String, "The world to take the image of, defaults to the overworld", false)
                ).Build();

            return new ApplicationCommandProperties[] { alertCommand, whitelistCommand, trackCommand };
        }
    }
}
