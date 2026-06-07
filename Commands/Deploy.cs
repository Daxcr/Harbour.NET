using Discord;
using Discord.Interactions;
using System.Diagnostics;
using System.Xml.Linq;

namespace Commands;

public class DeployCommand : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("deploy", "Deploy bot")]
    public async Task Deploy(
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
                    await FollowupAsync($"A .NET process is already using PID `{runningProcess.Id}`. Use `/kill [bot ID]` to kill it.", ephemeral: true);
                    return;
                }
            } catch { }
        }

        if (Directory.GetFiles($"bin/Bots/{BotID}/bin", "*.exe", SearchOption.TopDirectoryOnly).Length != 1)
        {
            await FollowupAsync($"Multiple or no Windows runtimes detected. Only one is expected.", ephemeral: true);
            return;
        }

        await FollowupAsync("Deploying...");

        string exe = Directory.GetFiles($"{Directory.GetCurrentDirectory()}/bin/Bots/{BotID}/bin", "*.exe", SearchOption.TopDirectoryOnly)[0];

        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{Path.ChangeExtension(exe, ".dll")}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(exe),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Environment =
                {
                    ["TOKEN"] = File.ReadAllText($"bin/Bots/{BotID}/token.txt"),
                }
            }
        };

        process.OutputDataReceived += (sender, msg) => { if (msg.Data != null) Console.WriteLine(msg.Data); };
        process.ErrorDataReceived += (sender, msg) => { if (msg.Data != null) Console.Error.WriteLine(msg.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        int ID = process.Id;

        File.WriteAllBytes($"bin/Bots/{BotID}/PID.txt", BitConverter.GetBytes(ID));

        await ModifyOriginalResponseAsync(message => message.Content = $"`{BotID}` has been deployed! Its proccess ID is `{ID}`.");
    }
}