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

// // step 0: we need to get the gateway URL from Discord
// const string discordApiUrl = "https://discord.com/api/v10";
//
// using var discordRestClient = new HttpClient()
// {
//   BaseAddress = new Uri(discordApiUrl),
// };
//
// var webSocketUrl = string.Empty;
//
// var gatewayResponse = await discordRestClient.GetAsync(new Uri("gateway", UriKind.Relative));
//
// if (gatewayResponse.IsSuccessStatusCode)
// {
//   Console.WriteLine("Gateway response received successfully.");
//   var gatewayResponseData = await gatewayResponse.Content.ReadFromJsonAsync<GatewayResponse>();
//   if (gatewayResponseData is not null)
//   {
//     webSocketUrl = gatewayResponseData.Url;
//     Console.WriteLine($"WebSocket URL: {webSocketUrl}");
//   }
//   else
//   {
//     Console.WriteLine("Failed to deserialize gateway response.");
//   }
// }
// else
// {
//   Console.WriteLine($"Failed to get gateway response: {gatewayResponse.StatusCode}");
// }
//
// // step 1: we need to connect to the WebSocket URL
// var discordWebSocketClient = new ClientWebSocket();
// var timeLastHeartbeatSent = DateTime.MinValue;
// var timeLastHeartbeatAcknowledged = DateTime.MinValue;
//
// await discordWebSocketClient.ConnectAsync(new Uri(webSocketUrl), CancellationToken.None);
//
// // step 2: start listening for the hello op code
// while (discordWebSocketClient.State is not WebSocketState.Closed)
// {
//   var buffer = new byte[1024];
//   var receiveResult = await discordWebSocketClient.ReceiveAsync(buffer, CancellationToken.None);
//
//   // TODO: Make sure we receive the hello op code
//   // if we don't receive it we should close the connection
//   if (receiveResult.MessageType == WebSocketMessageType.Close)
//   {
//     await discordWebSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
//     Console.WriteLine("WebSocket connection closed.");
//   }
//   else
//   {
//     var e = DeserializeDiscordEvent(buffer, receiveResult);
//
//     if (e.Op is not null && e.Op.Value is 10)
//     {
//       Console.WriteLine("Received hello event.");
//
//       _ = Task.Run(async () =>
//       {
//         Console.WriteLine("Starting heartbeat task.");
//
//         while (discordWebSocketClient.State is not WebSocketState.Closed)
//         {
//           var jitter = Random.Shared.Next(0, 1);
//           var interval = e.Data!.Value.GetProperty("heartbeat_interval").GetInt32();
//           await Task.Delay(interval + jitter);
//
//           if (timeLastHeartbeatAcknowledged < timeLastHeartbeatSent)
//           {
//             Console.WriteLine("Heartbeat not acknowledged, closing connection.");
//             await discordWebSocketClient.CloseAsync(WebSocketCloseStatus.ProtocolError, "Heartbeat not acknowledged", CancellationToken.None);
//             break;
//           }
//
//           timeLastHeartbeatSent = await SendHeartbeatAsync(discordWebSocketClient, e);
//
//           Console.WriteLine($"Heartbeat sent at {timeLastHeartbeatSent}.");
//         }
//       });
//     }
//
//     if (e.Op is not null && e.Op.Value is 11)
//     {
//       timeLastHeartbeatAcknowledged = DateTime.UtcNow;
//       Console.WriteLine($"Heartbeat acknowledged at {timeLastHeartbeatAcknowledged}.");
//     }
//
//     if (e.Op is not null && e.Op.Value is 1)
//     {
//       Console.WriteLine("Received heartbeat request.");
//       timeLastHeartbeatSent = await SendHeartbeatAsync(discordWebSocketClient, e);
//     }
//   }
// }
//
// static async Task<DateTime> SendHeartbeatAsync(ClientWebSocket discordWebSocketClient, DiscordEvent e)
// {
//   var heartbeat = new
//   {
//     op = 1,
//     d = e.Sequence
//   };
//
//   var heartbeatMessage = JsonSerializer.Serialize(heartbeat);
//
//   await discordWebSocketClient.SendAsync(
//     new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(heartbeatMessage)),
//     WebSocketMessageType.Text,
//     true,
//     CancellationToken.None
//   );
//
//   return DateTime.UtcNow;
// }
//
// static DiscordEvent DeserializeDiscordEvent(byte[] bufffer, WebSocketReceiveResult result)
// {
//   var message = System.Text.Encoding.UTF8.GetString(bufffer, 0, result.Count);
//   var discordMessage = JsonSerializer.Deserialize<DiscordEvent>(message)
//     ?? throw new DiscordException("Failed to deserialize Discord message.");
//   return discordMessage;
// }
//
// internal record DiscordEvent(
//   [property: JsonPropertyName("op")] int? Op,
//   [property: JsonPropertyName("d")] JsonElement? Data,
//   [property: JsonPropertyName("s")] int? Sequence,
//   [property: JsonPropertyName("t")] string? Type
// );
//
// internal class DiscordException(string message) : Exception(message)
// {
// }