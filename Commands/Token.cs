using Discord;
using Discord.Interactions;
using System.Xml.Linq;

namespace Commands;

public class TokenCommand : InteractionModuleBase<SocketInteractionContext>
{
    public static string Key = "";
    public static string Bot = "";
    public static string token = "";
    public static long Time = 0;

    [SlashCommand("token", "Generate a short-lived key to transfer token")]
    public async Task Token(
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

        Key = $"{Guid.NewGuid()}".Replace("-", "");
        Bot = BotID;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Time = now.ToUnixTimeSeconds();

        await FollowupAsync($"Your key is `{Key}`. Be quick to enter it into your dashboard, as it expires in 60 seconds.", ephemeral: true);
    }

    public static async Task TokenOk(string Token)
    {
        IUser user = await Program.client!.GetUserAsync(Admin.ID);
        string BotID = Bot;

        Embed embed = new EmbedBuilder
        {
            Title = "Token Verification",
            Description = $"A new request has been filed for a token change.\n**Token:** `{Token.Substring(0, 4)}...`\n**Bot:** `{BotID}`"
        }.Build();

        IUserMessage message = await user.SendMessageAsync(
            embed: embed,
            components: new ComponentBuilder()
                .WithButton("Confirm", $"token_{Key}", ButtonStyle.Success)
                .Build()
        );

        token = Token;

        await Task.Delay(60000);

        await message.ModifyAsync(message =>
        {
            message.Components = new ComponentBuilder().Build();
            message.Content = "Expired";
        });
    }
}