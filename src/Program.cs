using CommandLine;

namespace mstg
{
    class Program
    {
        public static Random Random { get; set; }

        public class Options
        {
            [Option("config-path", Required = false, Default = "config/")]
            public string ConfigPath { get; set; }
        }

        async static Task Main(string[] args)
        {
            Random = new Random();

            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync<Options>(async o =>
                {
                    try
                    {
                        Console.WriteLine($"\x1b[1;35m               _\x1b[0m");
                        Console.WriteLine($"\x1b[1;35m _ __ ___  ___| |_ __ _\x1b[0m");
                        Console.WriteLine($"\x1b[1;35m| '_ ` _ \\/ __| __/ _` |\x1b[0m");
                        Console.WriteLine($"\x1b[1;35m| | | | | \\__ | || (_| |\x1b[0m");
                        Console.WriteLine($"\x1b[1;35m|_| |_| |_|___/\\__\\__, |\x1b[0m");
                        Console.WriteLine($"\x1b[2;37m==================\x1b[0m\x1b[1;35m|___/\x1b[0m\x1b[2;37m=\x1b[0m{Environment.NewLine}");

                        PrintMessage("Configuring application...", Entities.ConsoleMessage.Enums.Type.Info);
                        Settings.Setup(o.ConfigPath);

                        PrintMessage("Migrating database...", Entities.ConsoleMessage.Enums.Type.Info);
                        Database.Migrate();

                        await Clients.Setup(
                            mastodonInstance: Settings.Api_Mastodon_Instance,
                            mastodonToken: Settings.Api_Mastodon_Token,
                            telegramToken: Settings.Api_Telegram_Token
                        );

                        PrintMessage("Starting server...", Entities.ConsoleMessage.Enums.Type.Info);
                        await Server.Start();
                    }
                    catch (Exception e)
                    {
                        HandleError(e);
                    }
                    finally
                    {
                        PrintMessage("Exiting...", "🛑");
                        Environment.Exit(1);
                    }
                });

            Thread.Sleep(int.MaxValue);
        }

        public static void HandleError(Exception exception)
        {
            var details = $"{exception.StackTrace}";

            if(exception.InnerException != null)
            {
                details = $"{exception.InnerException.Message}{Environment.NewLine}{details}"; 
            }

            PrintMessage(new Entities.ConsoleMessage {
                Details = details,
                Text = $"[{exception.GetType().ToString()}] {exception.Message}",
                Type = Entities.ConsoleMessage.Enums.Type.Error
            });
        }

        public static void PrintMessage(string text, Entities.ConsoleMessage.Enums.Type type = Entities.ConsoleMessage.Enums.Type.Normal)
        {
            PrintMessage(new Entities.ConsoleMessage {
                Text = text,
                Type = type
            });
        }

        public static void PrintMessage(string text, string emoji)
        {
            PrintMessage(new Entities.ConsoleMessage {
                Emoji = emoji,
                Text = text
            });
        }

        public static void PrintMessage(Entities.ConsoleMessage consoleMessage)
        {
            var output = "";
            var prefixColor = "";
            var prefixEmoji = "";
            var prefixText = "";

            switch (consoleMessage.Type)
            {
                case Entities.ConsoleMessage.Enums.Type.Error:
                    prefixColor = "1;31";
                    prefixEmoji = "❌";
                    prefixText = "Error";
                    break;
                case Entities.ConsoleMessage.Enums.Type.Info:
                    prefixEmoji = "ℹ️";
                    break;
                case Entities.ConsoleMessage.Enums.Type.Success:
                    prefixEmoji = "✅";
                    break;
            }

            if(consoleMessage.Emoji != null)
            {
                prefixEmoji = consoleMessage.Emoji;
            }

            if(prefixEmoji != "")
            {
                // BUG: Wrong padding with various emojis (like 🛑)
                var padding = new String(' ', prefixEmoji.Length);
                output += $"{prefixEmoji}{padding}";
            }

            if(prefixText != "")
            {
                output += $"\x1b[{prefixColor}m{prefixText}:\x1b[0m ";
            }

            output += $"\x1b[37m{consoleMessage.Text}\x1b[0m";

            if(consoleMessage.Details != null)
            {
                output += $"\x1b[2;37m{Environment.NewLine}—————{Environment.NewLine}{consoleMessage.Details}{Environment.NewLine}—————\x1b[0m";
            }

            Console.WriteLine(output);
        }
    }

    /*class Program
    {
        string mastodonInstance { get; set; } = "";
        string mastodonToken { get; set; } = "";
        string telegramChannel { get; set; } = "";
        string telegramToken { get; set; } = "";

        static async Task Main(string[] args)
        {
            Program p = new Program();
            await p.Setup();

            //using var cts = new CancellationTokenSource();
            //telegramBot.StartReceiving(HandleUpdateAsync, HandlePollingError, null, cts.Token);

            //Console.WriteLine("Hello World!");
        }

        async Task Setup()
        {
            mastodonInstance = "gearheads.social";
            mastodonToken = "U6zFL-He1VcJNG4zT_CsJ27Iu3NgIF9t-4jZg7gsgOM";
            telegramChannel = "@duckytoots";
            telegramToken = "6293486908:AAGZLlvHpn-XOQ8Tro2IOJt2vGY8c0_ajsY";

            if(telegramChannel.StartsWith("@") != true)
            {
                telegramChannel = $"@{telegramChannel}";
            }

            //var mastodonAuth = new AuthenticationClient(mastodonInstance);
            //var auth = await mastodonAuth.ConnectWithPassword("blah", "blah");

            var mastodonAuth = new Mastonet.Entities.Auth {
                AccessToken = mastodonToken
            };

            var mastodonClient = new MastodonClient(mastodonInstance, mastodonAuth.AccessToken);
            var telegramClient = new TelegramBotClient(telegramToken);

            await telegramClient.TestApiAsync();
            var mastodonInstanceDetails = await mastodonClient.GetInstanceV2();

            var mastodonStreaming = mastodonClient.GetUserStreaming();

            //mastodonStreaming.OnUpdate += HandleMastodonUpdate;
            
            await telegramClient.SendTextMessageAsync(
                chatId: telegramChannel,
                text: "Hello!"
            );

            await mastodonStreaming.Start();
        }

        void HandleMastodonUpdate(object sender, StreamUpdateEventArgs e)
        {
            Console.WriteLine(e.Status.Content);
        }
    }*/
}