using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using ShadowBot.DatabaseModels;
using ShadowBot.MLComponent;

namespace ShadowBot.ApplicationCommands
{
    internal class SlashCommands : ApplicationCommandModule
    {
        [SlashCommand("ForceRetrain", "Force the model manager to retrain the ML model")]
        public async Task ForceRetrainCommand(InteractionContext ctx)
        {
            if (!ctx.Client.CurrentApplication.Owners.Any(x => x.Id == ctx.User.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You are not my father!")
                        .AsEphemeral());
                return;
            }

            _ = Task.Run(async () =>
            {
                await ctx.DeferAsync();
                ToxicModelManager.RetrainModel();
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Retraining done"));
            });
        }

        [SlashCommand("Setup", "Set the bot up for your guild")]
        public async Task SetupCommand(InteractionContext ctx,
            [Option("ModelAlertChannel", "The channel to send alerts from the machine learning model to")] DiscordChannel alertChannel,
            [Option("ReportChannel", "The channel to send reports to")] DiscordChannel reportChannel)
        {
            if (!ctx.Member.Permissions.HasPermission(Permissions.ManageGuild))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You do not have the required permissions to execute this command!")
                        .AsEphemeral());
                return;
            }

            _ = Task.Run(async () =>
            {
                await ctx.DeferAsync();
                new DataAccess(Environment.GetEnvironmentVariable("ConnectionString")).UpdateGuild(new()
                {
                    Id = ctx.Guild.Id,
                    ModelAlertsChannelId = alertChannel.Id,
                    ReportChannelId = reportChannel.Id
                });
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Setup done :D"));
            });
        }
    }
}
