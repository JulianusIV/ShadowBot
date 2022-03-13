using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

namespace ShadowBot.ApplicationCommands
{
    internal class ContextMenus : ApplicationCommandModule
    {
        [ContextMenu(ApplicationCommandType.MessageContextMenu, "Report")]
        public async Task ReportMessageMenu(ContextMenuContext ctx)
        {
            var guild = new DataAccess(Environment.GetEnvironmentVariable("ConnectionString")).GetGuild(ctx.Guild.Id);

            if (guild.ReportChannelId is null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                   new DiscordInteractionResponseBuilder().WithContent("Report not set up in this Guild")
                   .AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Report this message as toxic:\n" + ctx.TargetMessage.JumpLink)
                    .AddComponents(new DiscordButtonComponent(ButtonStyle.Danger, "submit", "Submit"),
                        new DiscordButtonComponent(ButtonStyle.Primary, "cancel", "Nevermind"))
                    .AsEphemeral());

            var interactivity = ctx.Client.GetInteractivity();
            var buttonResult = await interactivity.WaitForButtonAsync(await ctx.GetOriginalResponseAsync());

            if (buttonResult.TimedOut)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Timed out..."));
                return;
            }

            if (buttonResult.Result.Id == "submit")
            {
                if (ctx.TargetMessage.Author is null || ctx.TargetMessage.Author.IsBot)
                    return;

                var embedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Message by {ctx.TargetMessage.Author.Username}#{ctx.TargetMessage.Author.Discriminator} ({ctx.TargetMessage.Author.Id}) was reported as toxic\n\nContent:")
                    .WithDescription(ctx.TargetMessage.Content)
                    .AddField("Message:", $"[Click me!]({ctx.TargetMessage.JumpLink})")
                    .WithColor(DiscordColor.Black)
                    .WithFooter($"{ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id})", ctx.User.AvatarUrl);

                DiscordComponent[] components = new DiscordComponent[]
                {
                    new DiscordButtonComponent(ButtonStyle.Danger, "report_correct", "Confirm Toxic", emoji: new DiscordComponentEmoji("✔️")),
                    new DiscordButtonComponent(ButtonStyle.Success, "report_incorrect", "Not Toxic", emoji: new DiscordComponentEmoji("✖️")),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "report_duplicate", "Mark as duplicate", emoji: new DiscordComponentEmoji("🔲"))
                };

                var messageBuilder = new DiscordMessageBuilder()
                    .WithEmbed(embedBuilder)
                    .AddComponents(components);

                await (await ctx.Client.GetChannelAsync((ulong)guild.ReportChannelId)).SendMessageAsync(messageBuilder);

                await buttonResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().WithContent("Report successful"));
            }
            else if (buttonResult.Result.Id == "cancel")
            {
                await buttonResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Okay I wont post it then."));
                return;
            }
        }
    }
}
