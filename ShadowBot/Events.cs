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
            => Task.CompletedTask;

        internal static Task GuildCreated(DiscordClient sender, GuildCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                new DataAccess(Environment.GetEnvironmentVariable("ConnectionString")).CreateGuild(new Guild { Id = e.Guild.Id });
                var logChannelId = Environment.GetEnvironmentVariable("LogChannelId");
                if (logChannelId is not null)
                    await (await sender.GetChannelAsync(ulong.Parse(logChannelId))).SendMessageAsync($"Added new guild {e.Guild.Name} ({e.Guild.Id}) to the database.");
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

                await CheckMessageAsync(sender, e.Message, e.Author, (ulong)guild.ModelAlertsChannelId);
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

                await CheckMessageAsync(sender, e.Message, e.Author, (ulong)guild.ModelAlertsChannelId);
            });
            return Task.CompletedTask;
        }

        internal static void TimerElapsed(object? sender, ElapsedEventArgs e)
            => _ = Task.Run(() => ToxicModelManager.RetrainModel());

        internal static Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                //check if user is mod
                if (!(await e.Guild.GetMemberAsync(e.User.Id)).Permissions.HasPermission(Permissions.BanMembers))
                    return;

                //check if id fits
                if (!e.Id.StartsWith("model_") && !e.Id.StartsWith("report_") && !e.Id.StartsWith("master_"))
                    return;

                //disable buttons
                var rows = e.Message.Components.First();
                var components = rows.Components.ToList();
                components.ForEach(x => ((DiscordButtonComponent)x).Disable());

                DiscordMessageBuilder builder = new DiscordMessageBuilder().AddEmbed(e.Message.Embeds[0]).AddComponents(components);

                await e.Message.ModifyAsync(builder);

                switch (e.Id)
                {
                    case "model_correct":
                    case "model_duplicate":
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent(":+1:"));
                        break;
                    case "model_incorrect":
                        SendToMaster(client, e);
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Message submitted as non-toxic"));
                        break;
                    case "report_correct":
                        SendToMaster(client, e);
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Message submitted as toxic"));
                        break;
                    case "report_incorrect":
                    case "report_duplicate":
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent(":+1:"));
                        break;
                    case "master_confirm":
                        bool isToxic = bool.Parse(e.Message.Embeds[0].Fields.First(x => x.Name == "Reported as Toxic:").Value);
                        using (SqlConnection connection = new(Environment.GetEnvironmentVariable("ConnectionString")))
                        using (SqlCommand command = new($"INSERT INTO dbo.CommentData (comment_text, toxic) VALUES (@commentText, {(isToxic ? 1 : 0)});", connection))
                        {
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("commentText", e.Message.Embeds[0].Description);
                            connection.Open();
                            command.ExecuteNonQuery();

                            Bot.Instance.InsertCounter++;
                        }
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Added to Database."));
                        break;
                    case "master_reject":
                    case "master_duplicate":
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent(":+1:"));
                        break;
                    default:
                        break;
                }
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

        private static async Task CheckMessageAsync(DiscordClient sender, DiscordMessage message, DiscordUser user, ulong alertChannelId)
        {
            var res = ToxicModelManager.Predict(message.Content);

            if (res.PredictedLabel)
            {
                var embedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Message by {user.Username}#{user.Discriminator} ({user.Id}) evaluated as toxic\n\nContent:")
                    .WithDescription(message.Content)
                    .AddField("Confidence:", $"{res.Probability * 100}%")
                    .AddField("Message:", $"[Click me!]({message.JumpLink})")
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

                await (await sender.GetChannelAsync(alertChannelId)).SendMessageAsync(messageBuilder);
            }
        }

        private static async void SendToMaster(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"\n\nContent:")
                    .WithDescription(e.Message.Embeds[0].Description)
                    .AddField("Reported as Toxic:", (e.Id != "model_incorrect").ToString())
                    .WithColor(new DiscordColor(e.Id != "model_incorrect" ? 1 : 0, e.Id != "model_incorrect" ? 0 : 1, 0));

            DiscordComponent[] components = new DiscordComponent[]
            {
                new DiscordButtonComponent(ButtonStyle.Danger, "master_confirm", "Confirm", emoji: new DiscordComponentEmoji("✔️")),
                new DiscordButtonComponent(ButtonStyle.Success, "master_reject", "Reject", emoji: new DiscordComponentEmoji("✖️")),
                new DiscordButtonComponent(ButtonStyle.Secondary, "master_duplicate", "Mark as duplicate", emoji: new DiscordComponentEmoji("🔲"))
            };

            var messageBuilder = new DiscordMessageBuilder()
                .WithEmbed(embedBuilder)
                .AddComponents(components);

            await (await client.GetGuildAsync(962448797915553822)).GetChannel(962449532438863902).SendMessageAsync(messageBuilder);
        }
    }
}
