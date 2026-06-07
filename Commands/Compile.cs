using Discord;
using Discord.Interactions;
using System.Diagnostics;
using System.Xml.Linq;

namespace Commands;

public class CompileCommand : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("compile", "Compile source")]
    public async Task Compile(
        [Summary("botID", "Bot ID")]
        [Autocomplete(typeof(BotIDAutocompleteHandler))]
        string BotID
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
        if (!Directory.Exists($"bin/Bots/{BotID}/_dl"))
        {
            await FollowupAsync($"Nothing to compile. Run `/pull`.", ephemeral: true);
            return;
        }

        if (Directory.Exists($"bin/Bots/{BotID}/_dl/bin"))
            Directory.Delete($"bin/Bots/{BotID}/_dl/bin", true);
        if (Directory.Exists($"bin/Bots/{BotID}/_dl/obj"))
            Directory.Delete($"bin/Bots/{BotID}/_dl/obj", true);

        if (Directory.GetFiles($"bin/Bots/{BotID}/_dl/", "*.csproj").Length != 1)
        {
            await FollowupAsync($"`.csproj` not found in root, or you have more than one.", ephemeral: true);
            return;
        }

        await FollowupAsync("Building...");

        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish bin/Bots/{BotID}/_dl -o bin/Bots/{BotID}/_dl/bin/preStage --runtime win-x64 -p:PublishSingleFile=false",
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
            await ModifyOriginalResponseAsync(message => message.Content = $"Build failed: `{error}`");
            return;
        }

        await ModifyOriginalResponseAsync(message => message.Content = "Build completed. Use `/stage` then `/deploy` to run.");
    }
}