using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Xml.Linq;

namespace Commands;

public class MountCommand : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("mount", "Mount a bot")]
    public async Task Mount()
    {
        if (Context.User.Id != Admin.ID) return;

        string[] names = [
            "johnathan",
            "stinky",
            "fishbot",
            "eviljoe",
            "lesserdax",
            "1440pOledMonitor",
            "yes"
        ];

        ModalBuilder modalBuilder = new ModalBuilder()
            .WithTitle("Mounting")
            .WithCustomId("mount")
            .AddTextInput("URL", "mount_url", placeholder: "https://... .zip   https://github.com/...")
            .AddTextInput("ID", "mount_id", placeholder: names[Random.Shared.Next(0, names.Length)]);
            
        await Context.Interaction.RespondWithModalAsync(modalBuilder.Build());
    }
    public static async Task ModalRecieved(SocketModal modal)
    {
        await modal.DeferAsync(true);

        string? ID = modal.Data.Components.FirstOrDefault(component => component.CustomId == "mount_id")?.Value;
        string? URL = modal.Data.Components.FirstOrDefault(component => component.CustomId == "mount_url")?.Value;
        if (ID == null || URL == null)
        {
            await modal.FollowupAsync("Something went wrong.", ephemeral: true);
            return;
        }

        if (!File.Exists("bots.xml"))
            new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("Bots")).Save("bots.xml");

        XDocument doc = XDocument.Load("bots.xml");
        
        if (doc.Descendants("Bot").Any(child => child.Attribute("ID")?.Value == ID))
        {
            await modal.FollowupAsync($"A bot with the same ID (`{ID}`) already exists. Please use a different ID.", ephemeral: true);
            return;
        }
            
        Directory.CreateDirectory($"bin/Bots/{ID}");
        using (File.Create($"bin/Bots/{ID}/PID.txt")) { }

        XElement bot = new XElement("Bot", new XAttribute("ID", ID), new XAttribute("URL", URL));
        doc.Root!.Add(bot);
        doc.Save("bots.xml");

        await modal.FollowupAsync($"Bot `{ID}` has been set up.", ephemeral: true);
    }
}