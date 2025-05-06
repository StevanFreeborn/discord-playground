using System.Text.Json;

using Microsoft.Extensions.Logging;

var options = new DiscordClientOptions()
{
  ApiUrl = "https://discord.com/api/v10",
};

var httpClientFactory = new HttpClientFactory();
var webSocketFactory = new WebSocketFactory();
var loggerFactory = LoggerFactory.Create(static builder =>
{
  builder.AddJsonConsole(static o =>
  {
    o.JsonWriterOptions = new JsonWriterOptions
    {
      Indented = true,
      IndentSize = 2,
    };
  });

  builder.SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger<DiscordClient>();

var discordClient = new DiscordClient(
  options,
  httpClientFactory,
  webSocketFactory,
  logger
);

await discordClient.ConnectAsync();

Console.ReadKey();
