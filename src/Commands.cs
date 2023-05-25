using System.Diagnostics;
using System.Net;
using Microsoft.DotNet.PlatformAbstractions;
using mstg.Entities;

namespace mstg
{
    public class Commands
    {
        public async static Task<CommandOutput> Cat(Entities.CommandInput input)
        {
            var output = new CommandOutput();

            string height = Utilities.GenerateRandomNumber(250, 1000).ToString();
            string width = Utilities.GenerateRandomNumber(250, 1000).ToString();
            string url = $"https://placekitten.com/{height}/{width}";

            switch (input.User.Service)
            {
                case Entities.User.Enums.Service.Mastodon:
                    output.Text = url;
                    break;
                case Entities.User.Enums.Service.Telegram:
                    output.MediaType = Entities.Media.Enums.Type.Photo;
                    output.MediaUrl = url;
                    break;
            }

            output.Success = true;

            return output;
        }

        public async static Task<CommandOutput> Status(Entities.CommandInput input)
        {
            var output = new CommandOutput();

            var process = Process.GetCurrentProcess();

            var memoryUsage = Convert.ToDecimal(process.WorkingSet64 / 1000000).ToString();
            TimeSpan timeSinceStart = DateTime.Now.ToUniversalTime().Subtract(process.StartTime.ToUniversalTime());

            string environment = $".NET {Environment.Version}";
            string hostname = Dns.GetHostName();
            string memory = $"{memoryUsage}mb";
            string os = await Utilities.GetOperatingSystem();
            string uptime = timeSinceStart.ToString("d'd 'h'h 'm'm 's's'");
            string userId = input.User.Id;
            string userName = input.User.ServiceName;
            string userUsername = input.User.ServiceUsername;
            string version = Utilities.GetVersion();

            int failedPosts = 0;
            int queuedPosts = 0;
            int totalPosts = 0;

            if (input.User.Service == User.Enums.Service.Telegram)
            {
                userUsername = $"<a href=\"tg://user?id={input.User.ServiceId}\">{userUsername}</a>";
            }

            using (var db = new Database())
            {
                var queueTable = db.Queue;

                failedPosts = queueTable
                    .Where(q => q.Status == Entities.Queue.Enums.Status.Failed)
                    .Count();

                queuedPosts = queueTable
                    .Where(q => q.Status == Entities.Queue.Enums.Status.Queued)
                    .Count();

                totalPosts = queueTable
                    .Count();
            }

            string content = @$"<b>mstg</b> | {version}
—
<b>Posts</b>
<b> ↳ Total:</b> {totalPosts}
<b> ↳ Queued:</b> {queuedPosts}
<b> ↳ Failed:</b> {failedPosts}
—
<b>You</b>
<b> ↳ ID:</b> {userId}
<b> ↳ Name:</b> {userName}
<b> ↳ Username:</b> {userUsername}
—
<b>Uptime:</b> {uptime}
<b>Memory:</b> {memory}
<b>Host:</b> {hostname}
<b>OS:</b> {os}";

            output.Success = true;
            output.Text = content;

            return output;
        }

        static string GenerateUi(string content)
        {
            return content;
        }
    }
}