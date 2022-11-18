using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static QuotesBot.CommandHandler;

namespace QuotesBot;

public class Program
{
    public static Task Main(string[] Args) => new Program().MainAsync();

    private DiscordSocketClient? _Client;

    private async Task MainAsync()
    {
        // ReSharper disable once UnusedVariable
        Update Update = new Update();
        await Task.Delay(6000);
        
        if (File.Exists("Update.ps1")) File.Delete("Update.ps1");

        _Client = new DiscordSocketClient();

        _Client.Log += Log;
        _Client.MessageReceived += OnMessageReceived;
            
        if (!File.Exists("./Bot.json"))
        {
            Console.WriteLine("Please Insert Your Bots Token Here:");
            var GetToken = Console.ReadLine();
            var TokenJson = new JObject
            {
                ["Token"] = GetToken
            };
            await File.WriteAllTextAsync("./Bot.json", TokenJson.ToString());
        }
            
        Directory.CreateDirectory("./GuildQuotes");
        Console.Clear();
            
            
        var ClientInfo = await File.ReadAllTextAsync("./Bot.json");
        var ClientInfoJson = JObject.Parse(ClientInfo);
        var Token = ClientInfoJson.GetValue("Token")!.ToString();
        await _Client.LoginAsync(TokenType.Bot, Token);
        await _Client.StartAsync();
        // ReSharper disable once UnusedVariable
        CommandHandler Handler = new CommandHandler(_Client);
        _Client.Ready += OnStartup;
        await Task.Delay(-1);
    }

    private static async Task OnMessageReceived(SocketMessage SocketMessage)
    {
        if (SocketMessage.Author.IsBot) return;
        var Channel = SocketMessage.Channel;
        var QuoteFiles = Directory.GetFiles("./GuildQuotes");
        foreach (string QuoteFile in QuoteFiles)
        {
            var Json = await File.ReadAllTextAsync(QuoteFile);
            var QuoteFileData = JsonConvert.DeserializeObject<JsonQuoteData>(Json)!;

            if (QuoteFileData.ChannelId == Channel.Id)
            {
                var GetMessages = Channel.GetMessageAsync(SocketMessage.Id);
                var GetMessage = await GetMessages;
                string Message = GetMessage.Content.Trim();
                Regex BaseDoubleRegex = new Regex("^\"(?:[^\"]|\"\")*\"\\s*-\\s*[A-Za-z\\s0-9]+",
                    RegexOptions.IgnoreCase);
                Regex DoubleQuoteRegex = new Regex("^\"(?:[^\"]|\"\")*\"\\s-\\s[A-Za-z\\s0-9]+",
                    RegexOptions.IgnoreCase);

                Regex BaseSingleRegex =
                    new Regex("^'(?:[^']|'')*'\\s*-\\s*[A-Za-z\\s0-9]+", RegexOptions.IgnoreCase);
                Regex SingleQuoteRegex =
                    new Regex("^'(?:[^']|'')*'\\s-\\s[A-Za-z\\s0-9]+", RegexOptions.IgnoreCase);
                if (BaseDoubleRegex.IsMatch(Message, 0) || BaseSingleRegex.IsMatch(Message, 0))
                {
                    if (!(DoubleQuoteRegex.IsMatch(Message, 0) || SingleQuoteRegex.IsMatch(Message, 0)))
                    {
                        int ToDashLength = Message.IndexOf("-", StringComparison.Ordinal);
                        string Quote = Message[..ToDashLength].Trim();
                        string Author = Message[(ToDashLength + 1)..].Trim();

                        Regex CheckQuote = new Regex("^\"\\s+(?:[^\"]|\"\")*\\s+\"");
                        if (CheckQuote.IsMatch(Quote, 0))
                        {
                            Quote = Quote.Replace("\"", "").Trim();
                            Quote = $"\"{Quote}\"";
                        }

                        QuoteFileData.Quotes.Add($"{Quote} - {Author}");
                        await File.WriteAllTextAsync(QuoteFile, JsonConvert.SerializeObject(QuoteFileData));
                        continue;
                    }

                    QuoteFileData.Quotes.Add(Message);
                    await File.WriteAllTextAsync(QuoteFile, JsonConvert.SerializeObject(QuoteFileData));
                }
            }
        }
    }

    private static Task Log(LogMessage Msg)
    {
        Console.WriteLine(Msg.ToString());
        return Task.CompletedTask;
    }
        
    private async Task OnStartup()
    {
        var QuoteFiles = Directory.GetFiles("./GuildQuotes");
        foreach (string QuoteFile in QuoteFiles)
        {
            var Json = await File.ReadAllTextAsync(QuoteFile);
            var QuoteFileData = JsonConvert.DeserializeObject<JsonQuoteData>(Json)!;
            var Channel = _Client!.GetChannel(QuoteFileData.ChannelId) as SocketTextChannel;
            if (Channel.GetChannelType() is not ChannelType.Text || Channel is null) continue;
            var Messages = await Channel.GetMessagesAsync().FlattenAsync();
            foreach (var IMessage in Messages)
            {
                if (IMessage.Author.IsBot) return;
                string Message = IMessage.Content.Trim();
                        
                Regex BaseDoubleRegex = new Regex("^\"(?:[^\"]|\"\")*\"\\s*-\\s*[A-Za-z\\s0-9]+",
                    RegexOptions.IgnoreCase);
                Regex DoubleQuoteRegex = new Regex("^\"(?:[^\"]|\"\")*\"\\s-\\s[A-Za-z\\s0-9]+",
                    RegexOptions.IgnoreCase);

                Regex BaseSingleRegex =
                    new Regex("^'(?:[^']|'')*'\\s*-\\s*[A-Za-z\\s0-9]+", RegexOptions.IgnoreCase);
                Regex SingleQuoteRegex =
                    new Regex("^'(?:[^']|'')*'\\s-\\s[A-Za-z\\s0-9]+", RegexOptions.IgnoreCase);
                if (BaseDoubleRegex.IsMatch(Message, 0) || BaseSingleRegex.IsMatch(Message, 0))
                {
                    if (!(DoubleQuoteRegex.IsMatch(Message, 0) || SingleQuoteRegex.IsMatch(Message, 0)))
                    {
                        int ToDashLength = Message.IndexOf("-", StringComparison.Ordinal);
                        string Quote = Message[..ToDashLength].Trim();
                        string Author = Message[(ToDashLength + 1)..].Trim();

                        Regex CheckQuote = new Regex("^\"\\s+(?:[^\"]|\"\")*\\s+\"");
                        if (CheckQuote.IsMatch(Quote, 0))
                        {
                            Quote = Quote.Replace("\"", "").Trim();
                            Quote = $"\"{Quote}\"";
                        }

                        QuoteFileData.Quotes.Add($"{Quote} - {Author}");
                        await File.WriteAllTextAsync(QuoteFile, JsonConvert.SerializeObject(QuoteFileData));
                        continue;
                    }

                    QuoteFileData.Quotes.Add(Message);
                    await File.WriteAllTextAsync(QuoteFile, JsonConvert.SerializeObject(QuoteFileData));
                }
            }
        }
    }
}
