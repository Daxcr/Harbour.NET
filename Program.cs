using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System.Reflection;
using Commands;

class Program
{
    public static DiscordSocketClient? client;
    public static InteractionService? interactions;
    record TokenRequest(string Token, string Key);

    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapPost("/Harbour.NET/api/token", async (TokenRequest body) =>
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (
                body.Key.Replace(" ", "") == TokenCommand.Key &&
                TokenCommand.Key != "" &&
                now.ToUnixTimeSeconds() < TokenCommand.Time + 60 &&
                now.ToUnixTimeSeconds() > TokenCommand.Time
            )
            {
                _ = TokenCommand.TokenOk(body.Token);
                return Results.Ok();
            } else
            {
                await Task.Delay(5000);
                return Results.Unauthorized();
            }
        });

        _ = app.RunAsync("http://localhost:5068");

        Admin.ID = ulong.Parse(File.ReadAllText("admin.txt"));

        var config = new DiscordSocketConfig
        {
            MessageCacheSize = 0,
            AlwaysDownloadUsers = false,
            GatewayIntents = GatewayIntents.All
        };

        client = new DiscordSocketClient(config);
        interactions = new InteractionService(client);

        await interactions.AddModulesAsync(Assembly.GetEntryAssembly(), null);

        client.Log += LogAsync;

        var token = File.ReadAllText("token.txt");

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        client.Ready += async () =>
        {
            await interactions.RegisterCommandsGloballyAsync();
        };

        client.InteractionCreated += async (interaction) =>
        {
            var context = new SocketInteractionContext(client, interaction);
            await interactions.ExecuteCommandAsync(context, null);
        };
        client.ModalSubmitted += async modal =>
        {
            string customId = modal.Data.CustomId;

            if (customId == "mount")
                await MountCommand.ModalRecieved(modal);

            else if (customId.StartsWith("remount_"))
                await RemountCommand.ModalRecieved(modal);

            else
                await modal.RespondAsync("Something went wrong.", ephemeral: true);
        };
        client.InteractionCreated += async interaction =>
        {
            if (interaction is IComponentInteraction componentInteraction)
            {
                string id = componentInteraction.Data.CustomId;
                
                if (id.StartsWith("token_"))
                {
                    string key = id["token_".Length..];
                    if (TokenCommand.Key == key)
                    {
                        await componentInteraction.DeferAsync(true);
                        File.WriteAllText($"bin/Bots/{TokenCommand.Bot}/token.txt", TokenCommand.token);
                        await componentInteraction.FollowupAsync("Success", ephemeral: true);

                        await componentInteraction.Message.ModifyAsync(message =>
                        {
                            message.Components = new ComponentBuilder().Build();
                            message.Content = "Success";
                        });
                    } else
                    {
                        await componentInteraction.RespondAsync("Expired", ephemeral: true);
                    }
                }
            }
        };

        await Task.Delay(Timeout.Infinite);
    }

    private static Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
}