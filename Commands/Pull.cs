using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;

namespace Commands;

public class PullCommand : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("pull", "Pull a bot's binary or git repository")]
    public async Task Pull(
        [Summary("botID", "Bot ID")]
        [Autocomplete(typeof(BotIDAutocompleteHandler))]
        string BotID, IAttachment? ManualUpload = null, bool SkipUrlChecks = false
    )
    {
        if (Context.User.Id != Admin.ID) return;

        await DeferAsync(true);

        XDocument doc = XDocument.Load("bots.xml");
        XElement? bot = doc.Descendants("Bot").FirstOrDefault(child => child.Attribute("ID")?.Value == BotID);

        if (bot == null)
        {
            await FollowupAsync($"A bot with the ID `{BotID}` doesn't exists.", ephemeral: true);
            return;
        }

        await FollowupAsync($"Verifying URL...", ephemeral: true);
        string url = bot.Attribute("URL")!.Value;

        if (ManualUpload != null)
            url = ManualUpload.Url;

        if (await Utils.IsGitRepo(url) || SkipUrlChecks)
        {
            await ModifyOriginalResponseAsync(message => message.Content = "URL is a Git repository. Will pull now.");

            if (!Path.Exists($"bin/Bots/{BotID}/_dl/.git"))
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"clone {url} bin/Bots/{BotID}/_dl",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    await ModifyOriginalResponseAsync(message => message.Content = $"Clone failed: `{error}`");
                    return;
                }

                await ModifyOriginalResponseAsync(message => message.Content = "Repo has been cloned! Run `/compile` to build your code.");
            } else
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"-C bin/Bots/{BotID}/_dl reset --hard HEAD",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    await ModifyOriginalResponseAsync(message => message.Content = $"Pull failed: `{error}`");
                    return;
                }

                await ModifyOriginalResponseAsync(message => message.Content = "Repo has been pulled! Run `/compile` to build your code.");
            }
        } else if (await Utils.IsArchive(url))
        {
            await ModifyOriginalResponseAsync(message => message.Content = "URL is an archive. Will pull now.");

            try
            {
                foreach (string file in Directory.GetFiles($"bin/Bots/{BotID}/_dl/", "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                foreach (string dir in Directory.GetDirectories($"bin/Bots/{BotID}/_dl/"))
                    Directory.Delete(dir, true);

                using HttpClient client = new HttpClient();
                byte[] data = await client.GetByteArrayAsync(url);
                string path = $"bin/Bots/{BotID}/_dl/{Path.GetFileName(new Uri(url).LocalPath)}";
                File.WriteAllBytes(path, data);

                string dest = Path.GetDirectoryName(path)!;
                ZipFile.ExtractToDirectory(path, dest);
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            await ModifyOriginalResponseAsync(message => message.Content = "Archive has been extracted.");
        } else
        {
            if (ManualUpload == null)
                await ModifyOriginalResponseAsync(message => message.Content = "URL is invalid. Must be a Git repo or an archive. If the repo is private, set `skip-url-checks` to `true`.");
            else
                await ModifyOriginalResponseAsync(message => message.Content = "Upload is invalid. Must be an archive.");
            return;
        }
    }
}

public class BotIDAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        if (autocompleteInteraction.User.Id != Admin.ID)
            return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());
        if (!File.Exists("bots.xml"))
            return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());

        XDocument doc = XDocument.Load("bots.xml");

        string userInput = ((string)autocompleteInteraction.Data.Current.Value) ?? "";

        IEnumerable<AutocompleteResult> results = doc.Descendants("Bot")
            .Select(id => id.Attribute("ID")?.Value)
            .Where(id => id != null && id.Contains(userInput, StringComparison.OrdinalIgnoreCase))
            .Select(id => new AutocompleteResult(id, id)).Take(25);

        return AutocompletionResult.FromSuccess(results);
    }
}