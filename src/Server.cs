using System.Text.RegularExpressions;
using Mastonet;
using Microsoft.EntityFrameworkCore;
using mstg.Entities;
using Telegram.Bot;

namespace mstg
{
    public class Server
    {
        public async static Task Start()
        {
            var mastodonStream = Clients.Mastodon.Client.GetUserStreaming();
            var telegramClient = Clients.Telegram.Client;

            mastodonStream.OnUpdate += HandleMastodonUpdate;
            mastodonStream.OnNotification += HandleMastodonNotification;
            telegramClient.StartReceiving(
                updateHandler: HandleTelegramUpdate,
                pollingErrorHandler: HandleTelegramError,
                receiverOptions: new Telegram.Bot.Polling.ReceiverOptions
                {
                    AllowedUpdates = { }
                }
            );

            var pollPostsTask = PollPosts();
            var mastodonStreamTask = mastodonStream.Start();

            await Task.WhenAll(
                pollPostsTask,
                mastodonStreamTask
            );
        }

        static void HandleMastodonUpdate(object sender, StreamUpdateEventArgs e)
        {
            Task.Run(async () =>
            {
                var status = e.Status;

                if (status != null)
                {
                    await QueuePost(e.Status);
                }
            });
        }

        static void HandleMastodonNotification(object sender, StreamNotificationEventArgs e)
        {
            Task.Run(async () =>
            {
                var instance = await Clients.Mastodon.Client.GetInstanceV2();
                var notification = e.Notification;

                if (
                    notification != null &&
                    notification.Status != null
                )
                {
                    string fromUsername = GenerateFullAccountName(notification.Account.AccountName, instance.Domain);
                    var statusContent = Utilities.SanitizeMastodonContent(notification.Status.Content, true);

                    foreach (var mention in notification.Status.Mentions)
                    {
                        string username = GenerateFullAccountName(mention.AccountName, instance.Domain);
                        string me = GenerateFullAccountName(Clients.Mastodon.Me.AccountName, instance.Domain);

                        if (username == me)
                        {
                            statusContent = statusContent.Replace($"@{mention.AccountName}", "");
                        }
                    }

                    var command = await ExecuteCommand(
                        statusContent,
                        new Entities.User {
                            Service = Entities.User.Enums.Service.Mastodon,
                            ServiceId = notification.Account.Id,
                            ServiceName = notification.Account.DisplayName,
                            ServiceUsername = fromUsername
                        }
                    );

                    if (command != null)
                    {
                        string commandResult = command.Success ?
                            Utilities.SanitizeMastodonContent(command.Text, true) :
                            $"⚠️ {command.FailureReason}";

                        string commandResultFooter = $"{Environment.NewLine}—{Environment.NewLine}{fromUsername}";

                        await Clients.Mastodon.Client.PublishStatus(
                            replyStatusId: notification.Status.Id,
                            status: $"{commandResult}{commandResultFooter}",
                            visibility: Visibility.Direct
                        );

                        if (notification.Status.Visibility != Visibility.Direct)
                        {
                            await Clients.Mastodon.Client.PublishStatus(
                                replyStatusId: notification.Status.Id,
                                status: $"So as to not pollute your timeline, don't forget to send this as a direct message next time! You can do this by setting the 🌐 Post Privacy to Mentioned People Only.{commandResultFooter}",
                                visibility: Visibility.Direct
                            );
                        }
                    }
                }
            });
        }

        static async Task HandleTelegramError(ITelegramBotClient telegramBotClient, Exception e, CancellationToken cancellationToken)
        {
            Program.HandleError(e);
        }

        static async Task HandleTelegramUpdate(ITelegramBotClient telegramBotClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            if (
                update.Type == Telegram.Bot.Types.Enums.UpdateType.Message &&
                update.Message != null &&
                update.Message.Text != null &&
                update.Message.Text.StartsWith("/")
            )
            {
                var message = update.Message;
                var command = await ExecuteCommand(
                    message.Text,
                    new Entities.User {
                        Service = Entities.User.Enums.Service.Telegram,
                        ServiceId = update.Message.From.Id.ToString(),
                        ServiceName = $"{update.Message.From.FirstName} {update.Message.From.LastName}",
                        ServiceUsername = $"@{update.Message.From.Username}"
                    });

                if (command != null)
                {
                    string commandResult = command.Success ? command.Text : $"⚠️ {command.FailureReason}";

                    if(command.MediaUrl == null)
                    {
                        await Clients.Telegram.Client.SendTextMessageAsync(
                            chatId: message.Chat,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                            text: commandResult
                        );
                    }
                    else
                    {
                        switch(command.MediaType)
                        {
                            case Entities.Media.Enums.Type.Photo:
                                await Clients.Telegram.Client.SendPhotoAsync(
                                    caption: commandResult,
                                    chatId: message.Chat,
                                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                    photo: new Telegram.Bot.Types.InputFileUrl(command.MediaUrl)
                                );
                                break;
                        }
                    }
                }
            }
        }

        async static Task QueuePost(Mastonet.Entities.Status mastodonStatus)
        {
            if (mastodonStatus != null)
            {
                try
                {
                    var instance = await Clients.Mastodon.Client.GetInstanceV2();
                    var statusMedia = mastodonStatus.MediaAttachments;

                    List<Entities.Media> mediaItems = new List<Entities.Media>();
                    Entities.Post postItem = new Entities.Post();
                    Entities.Queue queueItem = new Entities.Queue();

                    if (mastodonStatus.Reblogged == true)
                    {
                        mastodonStatus = mastodonStatus.Reblog;
                        statusMedia = mastodonStatus.MediaAttachments;
                    }

                    if (
                        mastodonStatus.Visibility != Visibility.Public &&
                        mastodonStatus.Visibility != Visibility.Unlisted
                    )
                    {
                        return;
                    }

                    if (statusMedia != null)
                    {
                        foreach (var statusMediaItem in statusMedia)
                        {
                            Entities.Media.Enums.Type type = Entities.Media.Enums.Type.Photo;
                            string url =
                                statusMediaItem.RemoteUrl != null ? statusMediaItem.RemoteUrl : statusMediaItem.Url;

                            switch (statusMediaItem.Type)
                            {
                                case "image":
                                    type = Entities.Media.Enums.Type.Photo;
                                    break;
                                case "video":
                                    type = Entities.Media.Enums.Type.Video;
                                    break;
                            }

                            mediaItems.Add(
                                new Entities.Media
                                {
                                    MediaId = statusMediaItem.Id,
                                    Type = type,
                                    Url = url
                                }
                            );
                        }
                    }

                    postItem = new Entities.Post
                    {
                        AccountId = mastodonStatus.Account.Id,
                        AccountName = GenerateFullAccountName(mastodonStatus.Account.AccountName, instance.Domain),
                        AccountProfileUrl = mastodonStatus.Account.ProfileUrl,
                        CreatedAt = mastodonStatus.CreatedAt,
                        Instance = instance.Domain,
                        MediaItems = mediaItems,
                        Sensitive = (bool)mastodonStatus.Sensitive,
                        StatusContent = mastodonStatus.Content,
                        StatusId = mastodonStatus.Id,
                        StatusUrl = mastodonStatus.Url
                    };

                    queueItem = new Entities.Queue
                    {
                        CreatedAt = DateTime.UtcNow,
                        Post = postItem,
                        Status = Entities.Queue.Enums.Status.Queued
                    };

                    Program.PrintMessage(new Entities.ConsoleMessage
                    {
                        Text = $"⬇️ Queuing status: {mastodonStatus.Url}"
                    });

                    using (var db = new Database())
                    {
                        await db.Queue.AddAsync(queueItem);
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception e)
                {
                    Program.HandleError(e);
                }
            }
        }

        async static Task PollPosts()
        {

            int waitAmountShort = 10000;
            int waitAmountLong = 60000;
            int maxFailedRetries = 10;

            while (true)
            {
                try
                {
                    List<Entities.Queue> waitingItems = null;
                    int waitingItemsCount = 0;

                    using (var db = new Database())
                    {
                        waitingItems = db.Queue
                            .Include(q => q.Post)
                            .Include(q => q.Post.MediaItems)
                            .Where(q => q.Status == Entities.Queue.Enums.Status.Queued || q.Status == Entities.Queue.Enums.Status.Failed)
                            .Where(q => q.FailureCount <= maxFailedRetries)
                            .OrderByDescending(q => q.CreatedAt)
                            .ToList();

                        waitingItemsCount = waitingItems.Count();
                    }

                    if (waitingItemsCount == 0)
                    {
                        await Task.Delay(waitAmountShort);
                    }
                    else
                    {
                        foreach (var waitingItem in waitingItems)
                        {
                            var post = waitingItem.Post;
                            var caption = GenerateTelegramCaptionForPost(post);

                            Program.PrintMessage(new Entities.ConsoleMessage
                            {
                                Emoji = "⬆️",
                                Text = $"Posting status: {post.StatusUrl}"
                            });

                            try
                            {
                                if (post.MediaItems.Count() == 0)
                                {
                                    await Clients.Telegram.Client.SendTextMessageAsync(
                                        chatId: Settings.Api_Telegram_Channel,
                                        disableWebPagePreview: true,
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        text: caption
                                    );
                                }
                                else
                                {
                                    var mediaGroup = new List<Telegram.Bot.Types.IAlbumInputMedia>();

                                    foreach (var mediaItem in post.MediaItems)
                                    {
                                        var mediaGroupItemFileUrl = new Telegram.Bot.Types.InputFileUrl(mediaItem.Url);
                                        Telegram.Bot.Types.InputMedia mediaGroupItem = null;

                                        // BUG: Telegram cannot mix Audio and Photo/Video media groups
                                        switch (mediaItem.Type)
                                        {
                                            case Entities.Media.Enums.Type.Audio:
                                                mediaGroupItem = new Telegram.Bot.Types.InputMediaAudio(mediaGroupItemFileUrl);
                                                break;
                                            case Entities.Media.Enums.Type.Photo:
                                                mediaGroupItem = new Telegram.Bot.Types.InputMediaPhoto(mediaGroupItemFileUrl);
                                                break;
                                            case Entities.Media.Enums.Type.Video:
                                                mediaGroupItem = new Telegram.Bot.Types.InputMediaVideo(mediaGroupItemFileUrl);
                                                break;
                                        }

                                        if (post.MediaItems.First() == mediaItem)
                                        {
                                            if (mediaGroupItem != null)
                                            {
                                                mediaGroupItem.Caption = caption;
                                                mediaGroupItem.ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html;
                                            }
                                        }

                                        mediaGroup.Add((Telegram.Bot.Types.IAlbumInputMedia)mediaGroupItem);
                                    }

                                    await Clients.Telegram.Client.SendMediaGroupAsync(
                                        chatId: Settings.Api_Telegram_Channel,
                                        media: mediaGroup
                                    );
                                }

                                waitingItem.Status = Entities.Queue.Enums.Status.Posted;
                            }
                            catch (Exception e)
                            {
                                waitingItem.FailureCount++;
                                waitingItem.FailureReason = e.Message;
                                waitingItem.Status = Entities.Queue.Enums.Status.Failed;

                                throw;
                            }
                            finally
                            {
                                using (var db = new Database())
                                {
                                    var postedQueueItem = db.Queue
                                        .Where(q => q.Id == waitingItem.Id)
                                        .FirstOrDefault();

                                    if (postedQueueItem != null)
                                    {
                                        if (waitingItem.Status == Entities.Queue.Enums.Status.Failed)
                                        {
                                            postedQueueItem.FailureCount = waitingItem.FailureCount;
                                            postedQueueItem.FailureReason = waitingItem.FailureReason;
                                        }
                                        else
                                        {
                                            postedQueueItem.FailureCount = 0;
                                            postedQueueItem.FailureReason = "";
                                        }

                                        postedQueueItem.Status = waitingItem.Status;
                                        postedQueueItem.UpdatedAt = DateTime.UtcNow;

                                        await db.SaveChangesAsync();
                                    }
                                }
                            }

                            await Task.Delay(waitAmountLong);
                        }

                        await Task.Delay(waitAmountShort);
                    }
                }
                catch (Exception e)
                {
                    Program.HandleError(e);
                    await Task.Delay(waitAmountShort);
                }
            }
        }

        async static Task<CommandOutput> ExecuteCommand(string command, Entities.User user)
        {
            CommandOutput output;

            var commandArray = command.Trim().Split(" ");
            var commandInput = new CommandInput {
                Arguments = commandArray.Skip(1).ToArray(),
                Trigger = commandArray.First(),
                User = user
            };

            if(commandInput.User.Id == null)
            {
                commandInput.User = await mstg.Data.Users.GetUser(user);
            }

            if (!commandInput.Trigger.StartsWith("/"))
                commandInput.Trigger = $"/{commandInput.Trigger}";

            switch (commandInput.Trigger)
            {
                case "/cat":
                case "/catpls":
                    output = await Commands.Cat(commandInput);
                    break;
                case "/info":
                case "/status":
                    output = await Commands.Status(commandInput);
                    break;
                default:
                    output = new CommandOutput
                    {
                        FailureReason = "Unknown Command",
                        Success = false
                    };
                    break;
            }

            return output;
        }

        static string GenerateFullAccountName(string accountName, string instanceDomain)
        {
            string fullAccountName = accountName;

            if (!accountName.Contains("@"))
            {
                fullAccountName = $"{accountName}@{instanceDomain}";
            }

            fullAccountName = $"@{fullAccountName}";

            return fullAccountName;
        }

        static string GenerateTelegramCaptionForPost(Entities.Post post)
        {
            string caption = "";

            post = HandleTwitterRelays(post);

            string accountProfileUrl = post.AccountProfileUrl;
            string accountName = post.AccountName;
            string instance = post.Instance;
            string statusContent = Utilities.SanitizeMastodonContent(post.StatusContent);
            string statusUrl = post.StatusUrl;

            string emojiPrefix = "?";
            bool isMedia = false;

            if (post.MediaItems.Count() == 0)
            {
                emojiPrefix = "⌨️";
            }
            else
            {
                isMedia = true;

                switch (post.MediaItems[0].Type)
                {
                    case Entities.Media.Enums.Type.Audio:
                        emojiPrefix = "🔊";
                        break;
                    case Entities.Media.Enums.Type.Photo:
                        emojiPrefix = "📷";
                        break;
                    case Entities.Media.Enums.Type.Video:
                        emojiPrefix = "📹";
                        break;
                }
            }

            if (statusContent != String.Empty)
            {
                caption += isMedia ? $"<i>{statusContent}</i>" : $"{statusContent}";
                caption += $"{Environment.NewLine}—{Environment.NewLine}";
            }

            caption += $@"<a href=""{statusUrl}"">{emojiPrefix}</a> | <a href=""{accountProfileUrl}"">{accountName}</a>";

            return caption;
        }

        static Entities.Post HandleTwitterRelays(Entities.Post post)
        {
            List<string> twitterRelays = new List<string>()
            {
                "bird.makeup"
            };

            var username = Utilities.ParseMastodonUsername(post.AccountName);

            if (twitterRelays.Any(x => username.Domain.Contains(x)))
            {
                string relay = username.Domain;

                post.AccountName = $"@{username.Account}@twitter.com";
                post.AccountProfileUrl = $"https://twitter.com/{username.Account}";

                switch (username.Domain)
                {
                    case "bird.makeup":
                        post.StatusUrl = $"https://twitter.com/{username.Account}/status/{post.StatusUrl.Split('/').Last()}";
                        break;
                }
            }

            return post;
        }
    }
}