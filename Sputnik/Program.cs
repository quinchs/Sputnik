using Discord;
using Discord.WebSocket;
using Dynmap;
using Sputnik.Generation;
using Sputnik.Services;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Sputnik
{
    public class Program
    {
        static void Main(string[] args)
        {
            ConfigService.LoadConfig();
            new Program().StartAsync().GetAwaiter().GetResult();
        }

        public static DualPurposeCommandService CommandService;

        private DiscordSocketClient _discordClient;
        public static DynmapClient DynmapClient;
        public async Task StartAsync()
        {
            _discordClient = new DiscordSocketClient(new DiscordSocketConfig()
            {
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Debug,
            });

            DynmapClient = new DynmapClient(new DynmapClientConfig()
            {
                RefreshInterval = 2000,
                Uri = ConfigService.Config.DynmapUri
            });

            DynmapClient.Log += _dynmapClient_Log;
            _discordClient.Log += _discordClient_Log;

            var c = new ApplicationCommandCoordinator(_discordClient);

            await _discordClient.LoginAsync(TokenType.Bot, ConfigService.Config.Token);

            await _discordClient.StartAsync();

            await _discordClient.SetGameAsync("the Stars!", type: ActivityType.Listening);

            await DynmapClient.ConnectAsync();


            CommandService = new DualPurposeCommandService();
            CommandService.Log += _discordClient_Log;

            var handlerService = new HandlerService(_discordClient, DynmapClient);

            var mapService = new MapDownloaderService(DynmapClient);

            var image = new Bitmap(2048, 2048);

            var graphics = Graphics.FromImage(image);

            await Generation.Utils.DrawBackgroundAsync(graphics, 2048, 2048, new Point(0, 0), 3100);

            image.Save("test.png", System.Drawing.Imaging.ImageFormat.Png);

            await Task.Delay(-1);
        }


        private Task _discordClient_Log(LogMessage log)
        {
            var msg = log.Message;

            if (log.Source.StartsWith("Audio ") && (msg?.StartsWith("Sent") ?? false))
                return Task.CompletedTask;

            Severity? sev = null;

            if (log.Source.StartsWith("Audio "))
                sev = Severity.Music;
            if (log.Source.StartsWith("Gateway"))
                sev = Severity.Socket;
            if (log.Source.StartsWith("Rest"))
                sev = Severity.Rest;

            Logger.Write($"{log.Message} {log.Exception}", sev.HasValue ? new Severity[] { sev.Value, log.Severity.ToLogSeverity() } : new Severity[] { log.Severity.ToLogSeverity() });

            return Task.CompletedTask;
        }

        private Task _dynmapClient_Log(string arg)
        {
            Logger.Write(arg, Severity.Dynmap, Severity.Log);
            return Task.CompletedTask;
        }
    }
}
