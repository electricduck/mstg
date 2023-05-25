using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace mstg
{
    class Clients
    {
        public class Mastodon
        {
            public static Mastonet.MastodonClient Client { get; set; }
            public static Mastonet.Entities.Account Me { get; set; }

            public static async Task Connect(string instance, string token)
            {
                var authClient = new Mastonet.AuthenticationClient(instance);
                var client = new Mastonet.MastodonClient(instance, token);
                
                Clients.Mastodon.Client = client;
                Clients.Mastodon.Me = await Clients.Mastodon.Client.GetCurrentUser();
            }
        }

        public class Telegram
        {
            public static ITelegramBotClient Client { get; set; }

            public static async Task Connect(string token)
            {
                var client = new TelegramBotClient(token);
                await client.TestApiAsync();
                Clients.Telegram.Client = client;
            }
        }

        public async static Task Setup(string mastodonInstance, string mastodonToken, string telegramToken)
        {
            Program.PrintMessage("Connecting to Mastodon...", Entities.ConsoleMessage.Enums.Type.Info);
            await Clients.Mastodon.Connect(mastodonInstance, mastodonToken);

            if (Clients.Mastodon.Client == null)
            {
                throw new Exception("Unable to create Mastodon client");
            }

            Program.PrintMessage("Connecting to Telegram...", Entities.ConsoleMessage.Enums.Type.Info);
            await Clients.Telegram.Connect(telegramToken);

            if (Clients.Telegram.Client != null)
            {
                await Clients.Telegram.Client.TestApiAsync();
            }
            else
            {
                throw new Exception("Unable to create Telegram client");
            }
        }
    }
}