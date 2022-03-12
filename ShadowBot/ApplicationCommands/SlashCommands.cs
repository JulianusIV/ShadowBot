using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
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
    }
}
