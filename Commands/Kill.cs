using Discord;
using Discord.Interactions;
using System.Diagnostics;
using System.Xml.Linq;

namespace Commands;

public class KillCommand : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("kill", "Kill a bot's process")]
    public async Task Kill(
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
        if (!Directory.Exists($"bin/Bots/{BotID}"))
        {
            await FollowupAsync($"Your bot's home doesn't exist, somehow. Try doing `/pull`.", ephemeral: true);
            return;
        }

        if (File.Exists($"bin/Bots/{BotID}/PID.txt"))
        {
            try 
            {
                Process runningProcess = Process.GetProcessById(BitConverter.ToInt32(File.ReadAllBytes($"bin/Bots/{BotID}/PID.txt")));
                if (runningProcess.ProcessName == "dotnet")
                {
                    await FollowupAsync($"Killing {BotID} (PID: `{runningProcess.Id}`).", ephemeral: true);
                    runningProcess.Kill();
                } else
                {
                    await FollowupAsync($"This bot isn't running.", ephemeral: true);
                }
            } catch
            {
                await FollowupAsync($"This bot isn't running.", ephemeral: true);
            }
        } else
        {
            await FollowupAsync($"This bot isn't running.", ephemeral: true);
        }

        File.WriteAllBytes($"bin/Bots/{BotID}/PID.txt", []);
    }
}