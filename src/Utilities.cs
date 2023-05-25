using Microsoft.DotNet.PlatformAbstractions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace mstg
{
    public class Utilities
    {
        public static int GenerateRandomNumber(int minValue, int maxValue)
        {
            return Program.Random.Next(minValue, maxValue);
        }

        public async static Task<string> GetOperatingSystem()
        {
            string operatingSystem = RuntimeEnvironment.OperatingSystem;
            string operatingSystemVersion = RuntimeEnvironment.OperatingSystemVersion;
            string operatingSystemPretty = "";

            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Linux)
            {
                string osReleaseFilePath = "/usr/lib/os-release";

                if (File.Exists(osReleaseFilePath))
                {
                    string[] osReleaseFile = await File.ReadAllLinesAsync(osReleaseFilePath);

                    foreach(var osReleaseFileLine in osReleaseFile)
                    {
                        if(osReleaseFileLine.StartsWith("PRETTY_NAME"))
                        {
                            operatingSystemPretty = osReleaseFileLine
                                .Replace("PRETTY_NAME=", "")
                                .Replace("\"", "")
                                .Trim();
                        }
                    }
                }
            }

            if(operatingSystemPretty == String.Empty)
            {
                operatingSystemPretty = $"{operatingSystem} {operatingSystemVersion}";
            }

            return operatingSystemPretty;
        }

        public static string GetVersion()
        {
            var version = "?";
            var attribute = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            if(attribute?.InformationalVersion != null)
            {
                version = attribute.InformationalVersion;
            }

            return version;
        }

        public static Entities.MastodonUsername ParseMastodonUsername(string username)
        {
            Regex regex = new Regex(@"@?\b([A-Z0-9._%+-]+)@([A-Z0-9.-]+\.[A-Z]{2,})\b",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            MatchCollection matches = regex.Matches(username);

            return new Entities.MastodonUsername
            {
                Account = matches[0].Groups[1].Value,
                Domain = matches[0].Groups[2].Value
            };
        }

        public static string SanitizeMastodonContent(string content, bool noHtml = false)
        {
            string[] permittedHtmlTags = new string[] { "a", "b", "i" };
            string permittedHtmlTagsRegex = "";

            if (!noHtml)
            {
                foreach (var permittedHtmlTag in permittedHtmlTags)
                {
                    permittedHtmlTagsRegex += $"(?!{permittedHtmlTag})(?!/{permittedHtmlTag})";
                }
            }

            string htmlTagsRegex = $"<{permittedHtmlTagsRegex}.*?>";

            content = content
                .Replace("</p><p>", $"{Environment.NewLine}{Environment.NewLine}")
                .Replace("<br />", $"{Environment.NewLine}")
                .Replace("<em>", "<i>")
                .Replace("</em>", "</i>")
                .Replace("<hr />", "—")
                .Replace("<li>", " • ")
                .Replace("<strong>", "<b>")
                .Replace("</strong>", "</b>");

            content = Regex.Replace(content, htmlTagsRegex, String.Empty); // remove all remaining HTML tags (apart from allowed ones)
            content = Regex.Replace(content, ":[^\\s]+:", "□"); // replace custom emojis with unicode box error

            return content;
        }
    }
}