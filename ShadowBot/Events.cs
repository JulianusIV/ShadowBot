using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace ShadowBot
{
    internal class Events
    {
        internal static Task OnClientReady(DiscordClient sender, ReadyEventArgs _)
        {
            DiscordActivity activity = new("you from the shadows.", ActivityType.ListeningTo);
            sender.UpdateStatusAsync(activity);
            return Task.CompletedTask;
        }

        internal static Task GuildDownloadCompleted(DiscordClient _, GuildDownloadCompletedEventArgs _1)
        {
            return Task.CompletedTask;
        }

        internal static Task MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            throw new NotImplementedException();
        }

        internal static Task MessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
        {
            throw new NotImplementedException();
        }

        internal static Task ClientErrored(DiscordClient sender, ClientErrorEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
