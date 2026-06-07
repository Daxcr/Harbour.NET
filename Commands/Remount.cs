using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Xml.Linq;

namespace Commands;

public class RemountCommand : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("remount", "Edit a bot's details")]
    public async Task Remount(
        [Summary("botID", "Bot ID")]
        [Autocomplete(typeof(BotIDAutocompleteHandler))]
        string BotID
    )
    {
        if (Context.User.Id != Admin.ID) return;

        XDocument doc = XDocument.Load("bots.xml");
        XElement? bot = doc.Descendants("Bot").FirstOrDefault(child => child.Attribute("ID")?.Value == BotID);

        if (bot == null)
        {
            await FollowupAsync($"A bot with the ID `{BotID}` doesn't exists.", ephemeral: true);
            return;
        }

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
            .WithTitle($"Remounting {BotID}")
            .WithCustomId($"remount_{BotID}")
            .AddTextInput("URL", "remount_url", placeholder: "https://... .zip   https://github.com/...", value: bot?.Attribute("URL")?.Value)
            .AddTextInput("ID", "remount_id", placeholder: names[Random.Shared.Next(0, names.Length)], value: BotID);
            
        await Context.Interaction.RespondWithModalAsync(modalBuilder.Build());
    }
    public static async Task ModalRecieved(SocketModal modal)
    {
        await modal.DeferAsync(true);

        string? ID = modal.Data.Components.FirstOrDefault(component => component.CustomId == "remount_id")?.Value;
        string? URL = modal.Data.Components.FirstOrDefault(component => component.CustomId == "remount_url")?.Value;
        string OrigID = modal.Data.CustomId.Substring("remount_".Length);

        if (ID == null || URL == null)
        {
            await modal.FollowupAsync("Something went wrong.", ephemeral: true);
            return;
        }

        if (!File.Exists("bots.xml"))
            new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("Bots")).Save("bots.xml");

        XDocument doc = XDocument.Load("bots.xml");
        
        if (doc.Descendants("Bot").Any(child => child.Attribute("ID")?.Value == ID) && OrigID != ID)
        {
            await modal.FollowupAsync($"A bot with the same ID (`{ID}`) already exists. Please use a different ID.", ephemeral: true);
            return;
        }
            
        XElement? bot = doc.Descendants("Bot").FirstOrDefault(child => child.Attribute("ID")?.Value == OrigID);
        if (bot == null)
        {
            await modal.FollowupAsync($"Bot `{OrigID}` not found.", ephemeral: true);
            return;
        }

        bot.Attribute("ID")!.Value = ID;
        bot.Attribute("URL")!.Value = URL;
        doc.Save("bots.xml");

        if (OrigID != ID && Directory.Exists($"bin/Bots/{OrigID}"))
            Directory.Move($"bin/Bots/{OrigID}", $"bin/Bots/{ID}");

        await modal.FollowupAsync($"Bot `{ID}` has been remounted.", ephemeral: true);
    }
}