using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ShadowBot.DatabaseModels;
using ShadowBot.MLComponent;
using System.Data.SqlClient;
using System.Timers;

namespace ShadowBot
{
    internal class Events
    {
        internal static Task OnClientReady(DiscordClient sender, ReadyEventArgs _1)
        {
            _ = Task.Run(() =>
            {
                DiscordActivity activity = new("you from the shadows.", ActivityType.ListeningTo);
                sender.UpdateStatusAsync(activity);
            });
            return Task.CompletedTask;
        }

        internal static Task GuildDownloadCompleted(DiscordClient _, GuildDownloadCompletedEventArgs _1)
        {
            return Task.CompletedTask;
        }

        internal static Task GuildCreated(DiscordClient sender, GuildCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                new DataAccess(Environment.GetEnvironmentVariable("ConnectionString")).CreateGuild(new Guild { Id = e.Guild.Id });
                var logChannelId = Environment.GetEnvironmentVariable("LogChannelId");
                if (logChannelId is not null)
                    await(await sender.GetChannelAsync(ulong.Parse(logChannelId))).SendMessageAsync($"Added new guild {e.Guild.Name} ({e.Guild.Id}) to the database.");
            });
            return Task.CompletedTask;
        }

        internal static Task MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                if (e.Author is null || e.Author.IsBot)
                    return;
                var guild = new DataAccess(Environment.GetEnvironmentVariable("ConnectionString")).GetGuild(e.Guild.Id);

                if (guild.ModelAlertsChannelId is null)
                    return;

                var res = ToxicModelManager.Predict(e.Message.Content);

                if (res.PredictedLabel)
                {
                    var embedBuilder = new DiscordEmbedBuilder()
                        .WithTitle($"Message by {e.Author.Username}#{e.Author.Discriminator} ({e.Author.Id}) evaluated as toxic\n\nContent:")
                        .WithDescription(e.Message.Content)
                        .AddField("Confidence:", $"{res.Probability * 100}%")
                        .AddField("Message:", $"[Click me!]({e.Message.JumpLink})")
                        .WithColor(new DiscordColor(1, 1 - res.Probability, 0));

                    DiscordComponent[] components = new DiscordComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Danger, "correct", "Confirm Toxic", emoji: new DiscordComponentEmoji("✔️")),
                        new DiscordButtonComponent(ButtonStyle.Success, "incorrect", "Not Toxic", emoji: new DiscordComponentEmoji("✖️")),
                        new DiscordButtonComponent(ButtonStyle.Secondary, "duplicate", "Mark as duplicate", emoji: new DiscordComponentEmoji("🔲"))
                    };

                    var messageBuilder = new DiscordMessageBuilder()
                        .WithEmbed(embedBuilder)
                        .AddComponents(components);

                    await (await sender.GetChannelAsync((ulong)guild.ModelAlertsChannelId)).SendMessageAsync(messageBuilder);
                }
            });
            return Task.CompletedTask;
        }

        internal static Task MessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                if (e.Author is null || e.Author.IsBot)
                    return;
                var guild = new DataAccess(Environment.GetEnvironmentVariable("ConnectionString")).GetGuild(e.Guild.Id);

                if (guild.ModelAlertsChannelId is null)
                    return;

                var res = ToxicModelManager.Predict(e.Message.Content);

                if (res.PredictedLabel)
                {
                    var embedBuilder = new DiscordEmbedBuilder()
                        .WithTitle($"Message by {e.Author.Username}#{e.Author.Discriminator} ({e.Author.Id}) evaluated as toxic\n\nContent:")
                        .WithDescription(e.Message.Content)
                        .AddField("Confidence:", $"{res.Probability * 100}%")
                        .AddField("Message:", $"[Click me!]({e.Message.JumpLink})")
                        .WithColor(new DiscordColor(1, 1 - res.Probability, 0));

                    DiscordComponent[] components = new DiscordComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Danger, "model_correct", "Confirm Toxic", emoji: new DiscordComponentEmoji("✔️")),
                        new DiscordButtonComponent(ButtonStyle.Success, "model_incorrect", "Not Toxic", emoji: new DiscordComponentEmoji("✖️")),
                        new DiscordButtonComponent(ButtonStyle.Secondary, "model_duplicate", "Mark as duplicate", emoji: new DiscordComponentEmoji("🔲"))
                    };

                    var messageBuilder = new DiscordMessageBuilder()
                        .WithEmbed(embedBuilder)
                        .AddComponents(components);

                    await (await sender.GetChannelAsync((ulong)guild.ModelAlertsChannelId)).SendMessageAsync(messageBuilder);
                }
            });
            return Task.CompletedTask;
        }

        internal static void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            _ = Task.Run(() =>
            {
                ToxicModelManager.RetrainModel();
            });
        }

        internal static Task ComponentInteractionCreated(DiscordClient _1, ComponentInteractionCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                if (!(await e.Guild.GetMemberAsync(e.User.Id)).Permissions.HasPermission(Permissions.BanMembers))
                    return;
                if (e.Id != "model_incorrect" && e.Id != "model_correct" && e.Id != "model_duplicate" &&
                    e.Id != "report_incorrect" && e.Id != "report_correct" && e.Id != "report_duplicate")
                    return;
                var rows = e.Message.Components.First();
                var components = rows.Components.ToList();
                components.ForEach(x => ((DiscordButtonComponent)x).Disable());

                DiscordMessageBuilder builder = new DiscordMessageBuilder().AddEmbed(e.Message.Embeds[0]).AddComponents(components);

                await e.Message.ModifyAsync(builder);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                if (e.Id == "model_correct")
                {
                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Message confirmed as toxic.").AsEphemeral());
                    return;
                }
                if (e.Id == "model_duplicate" || e.Id == "report_duplicate")
                {
                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Message marked as duplicate.").AsEphemeral());
                    return;
                }
                if (e.Id == "report_incorrect")
                {
                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Message confirmed as not toxic.").AsEphemeral());
                    return;
                }
                bool isToxic = e.Id != "model_incorrect" && e.Id == "report_correct";
                using SqlConnection connection = new(Environment.GetEnvironmentVariable("ConnectionString"));
                using SqlCommand command = new($"INSERT INTO dbo.CommentData (comment_text, toxic) VALUES (@commentText, {(isToxic ? 1 : 0)});", connection);
                command.Parameters.Clear();
                command.Parameters.AddWithValue("commentText", e.Message.Embeds[0].Description);
                connection.Open();
                command.ExecuteNonQuery();

                Bot.Instance.InsertCounter++;

                await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Successfully submitted.").AsEphemeral());
            });
            return Task.CompletedTask;
        }

        internal static Task ClientErrored(DiscordClient sender, ClientErrorEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await File.AppendAllTextAsync("error.txt", DateTime.Now + ":\n" + e.Exception.Message + Environment.NewLine + e.Exception.StackTrace + Environment.NewLine);
                var logChannelId = Environment.GetEnvironmentVariable("LogChannelId");
                if (logChannelId is not null)
                    await (await sender.GetChannelAsync(ulong.Parse(logChannelId))).SendMessageAsync(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
            });
            return Task.CompletedTask;
        }
    }
}
