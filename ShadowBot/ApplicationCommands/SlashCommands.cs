using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity.Extensions;
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

                var alertPerms = alertChannel.PermissionsFor(await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id));
                var reportPerms = reportChannel.PermissionsFor(await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id));

                if (!alertPerms.HasPermission(Permissions.SendMessages))
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent($"I dont have permissions to send messages in {alertChannel.Mention}.\n" +
                        $"Please choose another channel, or grant me the necessary permissions!"));
                    return;
                }
                if (!reportPerms.HasPermission(Permissions.SendMessages))
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent($"I dont have permissions to send messages in {reportChannel.Mention}.\n" +
                        $"Please choose another channel, or grant me the necessary permissions!"));
                    return;
                }

                new DataAccess(Environment.GetEnvironmentVariable("ConnectionString")).UpdateGuild(new()
                {
                    Id = ctx.Guild.Id,
                    ModelAlertsChannelId = alertChannel.Id,
                    ReportChannelId = reportChannel.Id
                });
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Setup done :D"));
            });
        }

        [SlashCommand("Report", "Report a message as toxic.")]
        public async Task ReportMessageMenu(InteractionContext ctx,
            [Option("Message", "The id or link of the message to report.")] string messageId)
        {
            var guild = new DataAccess(Environment.GetEnvironmentVariable("ConnectionString")).GetGuild(ctx.Guild.Id);

            if (guild.ReportChannelId is null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                   new DiscordInteractionResponseBuilder().WithContent("Report not set up in this Guild")
                   .AsEphemeral());
                return;
            }

            DiscordMessage targetMessage;
            try
            {
                if (ulong.TryParse(messageId, out var messageUlong))
                {
                    targetMessage = await ctx.Channel.GetMessageAsync(messageUlong);
                }
                else
                {
                    targetMessage = await ctx.Channel.GetMessageAsync(ulong.Parse(messageId[messageId.LastIndexOf('/')..]));
                }
            }
            catch (NotFoundException)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                   new DiscordInteractionResponseBuilder().WithContent("Message not found!")
                   .AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Report this message as toxic:\n" + targetMessage.JumpLink)
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
                if (targetMessage.Author is null || targetMessage.Author.IsBot)
                    return;

                var embedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Message by {targetMessage.Author.Username}#{targetMessage.Author.Discriminator} ({targetMessage.Author.Id}) was reported as toxic\n\nContent:")
                    .WithDescription(targetMessage.Content)
                    .AddField("Message:", $"[Click me!]({targetMessage.JumpLink})")
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

        [SlashCommand("Support", "Get a link to the support Discord server")]
        public async Task Support(InteractionContext ctx)
            => await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("https://discord.gg/s7fShrSaXa"));
    }
}
