using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using ShadowBot.ApplicationCommands;
using ShadowBot.MLComponent;

namespace ShadowBot
{
    internal class Bot
    {
        #region Singleton
        private static Bot _instance;
        private static readonly object _padlock = new();
        public static Bot Instance
        {
            get
            {
                lock (_padlock)
                {
                    if (_instance is null)
                        _instance = new Bot();
                    return _instance;
                }
            }
        }
        #endregion

        public DiscordClient Client { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public SlashCommandsExtension SlashCommands { get; private set; }
        public int InsertCounter
        {
            get => _insertCounter;
            set
            {
                if (value > 20)
                {
                    _ = Task.Run(() => ToxicModelManager.RetrainModel());
                    _insertCounter = 0;
                }
                else
                    _insertCounter = value;
            }
        }

        private readonly System.Timers.Timer timer = new(86400000);

        private int _insertCounter;

        public async Task RunAsync()
        {
            DiscordConfiguration config = new()
            {
                Token = Environment.GetEnvironmentVariable("Token"),
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                AlwaysCacheMembers = false,
                MessageCacheSize = 0,
#if DEBUG
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
#endif
                Intents = DiscordIntents.GuildMessages | DiscordIntents.Guilds
            };

            Client = new(config);

            //Register client events
            Client.Ready += Events.OnClientReady;
            Client.GuildDownloadCompleted += Events.GuildDownloadCompleted;
            Client.GuildCreated += Events.GuildCreated;
            Client.MessageCreated += Events.MessageCreated;
            Client.MessageUpdated += Events.MessageUpdated;
            Client.ComponentInteractionCreated += Events.ComponentInteractionCreated;
            Client.ClientErrored += Events.ClientErrored;

            Interactivity = Client.UseInteractivity(new()
            {
                Timeout = TimeSpan.FromMinutes(2),
                AckPaginationButtons = true
            });

            SlashCommands = Client.UseSlashCommands();

            //Register ApplicationCommands
            SlashCommands.RegisterCommands<SlashCommands>(512370308532142091);
            SlashCommands.RegisterCommands<ContextMenus>(512370308532142091);

            //Register Slash events

            //Register timer events
            timer.Elapsed += Events.TimerElapsed;

            if (File.GetLastWriteTime("model.zip") < DateTime.Now - TimeSpan.FromSeconds(5))
                _ = Task.Run(() => ToxicModelManager.RetrainModel());

            timer.Start();

            await Client.ConnectAsync();

            await Task.Delay(-1);
        }
    }
}
