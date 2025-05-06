using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var config = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json", optional: false)
  .Build();

var section = config.GetSection(nameof(DiscordClientOptions));

var options = new DiscordClientOptions()
{
  ApiUrl = section.GetValue<string>("ApiUrl") ?? string.Empty,
  AppToken = section.GetValue<string>("AppToken") ?? string.Empty,
  Intents = section.GetValue<int>("Intents"),
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