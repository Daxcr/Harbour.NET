using Discord;
using Discord.Interactions;
using System.Diagnostics;
using System.Xml.Linq;

namespace Commands;

public class StageCommand : InteractionModuleBase<SocketInteractionContext>
{
    private void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }
    
    [SlashCommand("stage", "Replace your existing binary with a newly compiled one")]
    public async Task Stage(
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
            await FollowupAsync($"Nothing to stage, and no source. Run `/pull` then `/compile`.", ephemeral: true);
            return;
        }
        if (!Directory.Exists($"bin/Bots/{BotID}/_dl/bin/preStage"))
        {
            await FollowupAsync($"Nothing to stage. Run `/compile`.", ephemeral: true);
            return;
        }

        await FollowupAsync("Staging...");

        string source = $"bin/Bots/{BotID}/_dl/bin/preStage";
        string dest = $"bin/Bots/{BotID}/bin";

        CopyDirectory(source, dest);

        await ModifyOriginalResponseAsync(message => message.Content = $"`{BotID}` has been staged! Run `/deploy` to deploy it.");
    }
}