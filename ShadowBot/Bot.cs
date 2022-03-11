using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

namespace ShadowBot
{
    internal class Bot
    {
        public DiscordClient Client { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public SlashCommandsExtension SlashCommands { get; private set; }

        public async Task RunAsync()
        {
            DiscordConfiguration config = new()
            {
                Token = Environment.GetEnvironmentVariable("Token"),
                TokenType = TokenType.Bot,
                AutoReconnect = true,
#if DEBUG
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
#endif
                Intents = DiscordIntents.GuildMessages | DiscordIntents.Guilds
            };

            Client = new(config);

            //Register client events
            Client.Ready += Events.OnClientReady;
            Client.GuildDownloadCompleted += Events.GuildDownloadCompleted;
            Client.MessageCreated += Events.MessageCreated;
            Client.MessageUpdated += Events.MessageUpdated;
            Client.ClientErrored += Events.ClientErrored;

            Interactivity = Client.UseInteractivity(new()
            {
                Timeout = TimeSpan.FromMinutes(2),
                AckPaginationButtons = true
            });

            SlashCommands = Client.UseSlashCommands();

            //Register ApplicationCommands

            //Register Slash events
            
            await Client.ConnectAsync();

            await Task.Delay(-1);
        }
    }
}
