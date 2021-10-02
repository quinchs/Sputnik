using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Dynmap;
using Sputnik.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Handlers
{
    public class ApplicationCommandHandler : DiscordHandler
    {
        private DiscordSocketClient _client;

        public override async Task InitializeAsync(DiscordSocketClient client, DynmapClient dynmap)
        {
            _client = client;

            _client.SlashCommandExecuted += _client_SlashCommandExecuted;

            await Program.CommandService.RegisterModulesAsync(Assembly.GetExecutingAssembly(), null);
        }

        private async Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            _ = Task.Run(async () =>
            {
                var name = arg.CommandName;

                if (arg.Data.Options.Count == 1 && arg.Data.Options.First().Type == Discord.ApplicationCommandOptionType.SubCommand)
                {
                    name += " " + GetSubName(arg.Data.Options.First());
                }

                var context = new DualPurposeContext(_client, arg);
                var r = await Program.CommandService.ExecuteAsync(context, name, null);

                if (r.IsSuccess)
                {
                    Logger.Write($"Command [{name}] executed <Green>Successfully</Green>", new Severity[] { Severity.CommandService, Severity.Log }, nameof(ApplicationCommandHandler));
                }
                else
                {
                    Logger.Write($"<Red>Failed</Red> to execute command [{name}] - {(r.Error.HasValue ? $"{r.Error.Value}: " : "")}{r.ErrorReason}", new Severity[] { Severity.CommandService, Severity.Error }, nameof(ApplicationCommandHandler));

                    if (r is ExecuteResult ex)
                    {
                        Logger.Write($"[{name}]: {ex.Exception}", new Severity[] { Severity.CommandService, Severity.Error }, nameof(ApplicationCommandHandler));
                    }
                }
            });
        }

        private string GetSubName(SocketSlashCommandDataOption opt)
        {
            if (opt == null)
                return "";

            if(opt.Type == Discord.ApplicationCommandOptionType.SubCommand)
            {
                var others = GetSubName(opt.Options?.FirstOrDefault());

                return opt.Name + " " + others;
            }

            return "";
        }
    }
}
