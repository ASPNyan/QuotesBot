using System.Text;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using Discord;
using Discord.Net;
using Newtonsoft.Json;

namespace QuotesBot;

public class CommandHandler
{
    private readonly DiscordSocketClient _Client;
    
    public CommandHandler(DiscordSocketClient Client)
    {
        this._Client = Client;
        Client.Ready += Client_Ready;

        Client.SlashCommandExecuted += SlashCommandHandler;
    }
    
    private async Task SlashCommandHandler(SocketSlashCommand Command)
    {
        switch (Command.Data.Name)
        {
            case "register":
                await Register(Command);
                break;
            case "reset":
                await Reset(Command);
                break;
            // ReSharper disable once StringLiteralTypo
            case "totextfile":
                await ToTextFile(Command);
                break;
            case "github":
                await GitHub(Command);
                break;
        }
    }

    private async Task Client_Ready()
    {
        var Commands = CommandHandler.Commands();

        try
        {
            foreach (SlashCommandBuilder? Command in Commands)
            { 
                await _Client.Rest.CreateGlobalCommand(Command?.Build());   
            }
        }
        catch (HttpException Exception)
        {
            var Json = JsonConvert.SerializeObject(Exception.Errors, Formatting.Indented);
            Console.WriteLine(Json);
        }
    }

    private static IEnumerable<SlashCommandBuilder?> Commands()
    {
        var Commands = new List<SlashCommandBuilder?>
        {
            new SlashCommandBuilder()
                .WithName("register")
                .WithDescription("Register your Guild's and its Quotes Channel")
                .AddOption("channel",
                    ApplicationCommandOptionType.Channel,
                    "The Channel where the Quotes will be sent",
                    true
                ),
            new SlashCommandBuilder()
                .WithName("reset")
                .WithDescription("Reset Guild Settings and Quotes to null"),
            new SlashCommandBuilder()
                // ReSharper disable once StringLiteralTypo
                .WithName("totextfile")
                .WithDescription("Send All Quotes as a Text File")
                .AddOption("ephemeral",
                    ApplicationCommandOptionType.Boolean,
                    "Whether the File is Visible to Others or Not",
                    false
                ),
            new SlashCommandBuilder()
                .WithName("github")
                .WithDescription("Sends an embed containing this bot's GitHub page."),
        };

        return Commands;
    }

    private Task GitHub(SocketSlashCommand Command)
    {
        EmbedAuthorBuilder Author = new EmbedAuthorBuilder()
            .WithName("ASPNyan")
            .WithUrl("https://github.com/ASPNyan")
            .WithIconUrl("https://avatars.githubusercontent.com/u/85216339");
        Embed Embed = new EmbedBuilder()
            .WithAuthor(Author)
            .WithColor(Color.Teal)
            .WithTitle("[QuotesBot GitHub](https://github.com/ASPNyan/QuotesBot)")
            .WithDescription(
                "[QuotesBot](https://github.com/ASPNyan/QuotesBot) by GitHub user [ASPNyan](https://github.com/ASPNyan)"
                )
            .Build();

        Command.RespondAsync(embed: Embed, ephemeral: true);
        return Task.CompletedTask;
    }

    private async Task ToTextFile(SocketSlashCommand Command)
    {
        var Guild = _Client.GetGuild(Command.GuildId!.Value);
        var Ephemeral = Command.Data.Options?.FirstOrDefault()?.Value?.ToString() == "True";
        await Command.DeferAsync(Ephemeral);
        var GuildFiles = Directory.GetFiles("./GuildQuotes");
        Regex FileName = new Regex("[0-9]+", RegexOptions.IgnoreCase);

        foreach (string GuildFile in GuildFiles)
        {
            string JsonFileName = GuildFile.Split("/").Last().Split("\\").Last().Split(".").First();
            if (!FileName.IsMatch(JsonFileName)) continue;
            if (JsonFileName == Guild.Id.ToString())
            {
                string JsonFileData = await File.ReadAllTextAsync($"./GuildQuotes/{JsonFileName}.json");
                JsonQuoteData? JsonFile = JsonConvert.DeserializeObject<JsonQuoteData>(JsonFileData);
                var QuotesString = new StringBuilder();
                if (JsonFile?.Quotes != null)
                    foreach (string Quote in JsonFile.Quotes)
                    {
                        QuotesString.Append(Quote + "\n");
                    }
                await File.WriteAllTextAsync($"./GuildQuotes/{JsonFileName}.txt", QuotesString.ToString());
                await Command.FollowupWithFileAsync($"./GuildQuotes/{JsonFileName}.txt", fileName: $"{Guild.Name} Quotes.txt",  ephemeral: Ephemeral);
                File.Delete($"./GuildQuotes/{JsonFileName}.txt");
                return;
            }
        }
    }

    private async Task Reset(SocketInteraction Command)
    {
        var Guild = _Client.GetGuild(Command.GuildId!.Value);
        
        if (File.Exists($"./GuildQuotes/{Guild.Id}.json"))
        {
            JsonQuoteData EmptyData = new(0);
            await File.WriteAllTextAsync($"./GuildQuotes/{Guild.Id}.json", JsonConvert.SerializeObject(EmptyData, Formatting.Indented));
            
            Embed SuccessEmbed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle("Guild Reset!")
                .WithDescription("Guild Settings and Quotes have been reset")
                .Build();

            await Command.RespondAsync(embed: SuccessEmbed, ephemeral: true);
            return;
        }

        Embed FailEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Reset Failed")
            .WithDescription("Guild Settings and Quotes have not been reset")
            .Build();
        await Command.RespondAsync(embed: FailEmbed, ephemeral: true);
    }

    private static async Task Register(SocketSlashCommand CommandData)
    {
        SocketTextChannel? Channel = CommandData.Data.Options.First().Value as SocketTextChannel;
        var Guild = Channel!.Guild;

        bool Success = true;
        if (Channel.GetChannelType() != ChannelType.Text)
        {
            Success = false;
            goto FailSkip;
        }

        List<string>? ExistingQuotes = null;
        if (File.Exists($"./GuildQuotes/{Guild.Id}.json"))
        {
            var Json = await File.ReadAllTextAsync($"./GuildQuotes/{Guild.Id}.json");
            var GuildData = JsonConvert.DeserializeObject<JsonQuoteData>(Json);
            if (GuildData != null && GuildData.ChannelId == Channel.Id)
            {
                await CommandData.RespondAsync("This Channel is already registered as the Quotes Channel", ephemeral: true);
                return;
            }
            if (GuildData != null) ExistingQuotes = GuildData.Quotes;
        }
        else
        {
            File.Create($"./GuildQuotes/{Guild.Id}.json").Close();
        }

        try
        {
             var QuoteDataJson = new JsonQuoteData(Channel.Id);
             var GetMessages = Channel.GetMessagesAsync().ToListAsync();
             var ChannelMessages = await GetMessages;
             if (ExistingQuotes != null) QuoteDataJson.Quotes.AddRange(ExistingQuotes);
             if (ChannelMessages.Count > 0)
             {
                 foreach (var Message in from IMessage in ChannelMessages from MessageData in IMessage select MessageData.Content.Trim())
                 {
                     Regex QuoteRegex = new Regex("\"(?:[^\"]|\"\")*\"\\s+-\\s+[A-Za-z0-9]+", RegexOptions.IgnoreCase);
                     if (QuoteRegex.IsMatch(Message))
                     {
                         QuoteDataJson.Quotes.Add(Message);
                     }
                 }
             }

             await File.WriteAllTextAsync($"./GuildQuotes/{Guild.Id}.json", JsonConvert.SerializeObject(QuoteDataJson, Formatting.Indented));
        }
        catch (Exception Exc)
        {
            Console.WriteLine($"An Error Has Occurred During Guild Registration. Error Log: \n{Exc}");
            Success = false;
        }
        
        FailSkip:
        Embed Embed = new EmbedBuilder()
            .WithTitle("Registered!")
            .WithDescription($"Registered `{Guild.Name}` with <#{Channel.Id}> as Quotes Channel")
            .WithColor(Color.Green)
            .Build();

        if (!Success)
        {
            Embed = new EmbedBuilder()
                .WithTitle("Registration Failed!")
                .WithDescription("An Error Has Occurred During Guild Registration. Please Try Again Later.")
                .WithColor(Color.Red)
                .Build();
        }
            
        await CommandData.RespondAsync(embed: Embed, ephemeral: true);
    }

    public class JsonQuoteData
    {
        public ulong ChannelId { get; }
        public readonly List<string> Quotes = new();

        public JsonQuoteData(ulong ChannelId)
        {
            this.ChannelId = ChannelId;
        }
    }
}