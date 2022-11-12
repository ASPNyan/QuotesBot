using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static QuotesBot.CommandHandler;

namespace QuotesBot
{
    public class Program
    {
        public static Task Main(string[] Args) => new Program().MainAsync();

        private DiscordSocketClient? _Client;

        private async Task MainAsync()
        {
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
            var Channel = SocketMessage.Channel;
            var QuoteFiles = Directory.GetFiles("./GuildQuotes");
            foreach (string QuoteFile in QuoteFiles)
            {
                var Json = await File.ReadAllTextAsync(QuoteFile);
                var QuoteFileData = JsonConvert.DeserializeObject<JsonQuoteData>(Json)!;
                Regex DoubleQuoteRegex = new Regex("\"(?:[^\"]|\"\")*\"\\s+-\\s+[A-Za-z\\s0-9]+", RegexOptions.IgnoreCase);
                Regex SingleQuoteRegex = new Regex("'(?:[^']|'')*'\\s+-\\s+[A-Za-z\\s0-9]+", RegexOptions.IgnoreCase);
                if (QuoteFileData.ChannelId == Channel.Id)
                {
                    var GetMessage = Channel.GetMessageAsync(SocketMessage.Id);
                    var Message = await GetMessage;
                    if (DoubleQuoteRegex.IsMatch(Message.Content.Trim()) || SingleQuoteRegex.IsMatch(Message.Content.Trim()))
                    {
                        QuoteFileData.Quotes.Add(Message.Content.Trim());
                        await File.WriteAllTextAsync(QuoteFile, JsonConvert.SerializeObject(QuoteFileData, Formatting.Indented));
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
                foreach (var Message in Messages)
                {
                    Regex QuoteRegex = new Regex("\"(?:[^\"]|\"\")*\"\\s+-\\s+[A-Za-z0-9]+", RegexOptions.IgnoreCase);
                    if (QuoteRegex.IsMatch(Message.Content.Trim()))
                    {
                        if (!QuoteFileData.Quotes.Contains(Message.Content.Trim()))
                        {
                            QuoteFileData.Quotes.Add(Message.Content.Trim());
                            await File.WriteAllTextAsync(QuoteFile, JsonConvert.SerializeObject(QuoteFileData, Formatting.Indented));
                        }
                    }
                }
            }
        }
    }
}