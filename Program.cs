using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json.Serialization;

// step 0: we need to get the gateway URL from Discord
const string discordApiUrl = "https://discord.com/api/v10";

using var discordRestClient = new HttpClient()
{
  BaseAddress = new Uri(discordApiUrl),
};

var webSocketUrl = string.Empty;

var gatewayResponse = await discordRestClient.GetAsync(new Uri("gateway", UriKind.Relative));

if (gatewayResponse.IsSuccessStatusCode)
{
  Console.WriteLine("Gateway response received successfully.");
  var gatewayResponseData = await gatewayResponse.Content.ReadFromJsonAsync<GatewayDto>();
  if (gatewayResponseData is not null)
  {
    webSocketUrl = gatewayResponseData.Url;
    Console.WriteLine($"WebSocket URL: {webSocketUrl}");
  }
  else
  {
    Console.WriteLine("Failed to deserialize gateway response.");
  }
}
else
{
  Console.WriteLine($"Failed to get gateway response: {gatewayResponse.StatusCode}");
}

// step 1: we need to connect to the WebSocket URL
var discordWebSocketClient = new ClientWebSocket();

await discordWebSocketClient.ConnectAsync(new Uri(webSocketUrl), CancellationToken.None);

// step 2: start listening for the hello op code
while (discordWebSocketClient.State is not WebSocketState.Closed)
{
  var buffer = new byte[1024];
  var receiveResult = await discordWebSocketClient.ReceiveAsync(buffer, CancellationToken.None);
  
  // TODO: Make sure we receive the hello op code
  // if we don't receive it we should close the connection
  // if we do receive it we need to extract the heartbeat interval
  // and start sending heartbeats
  if (receiveResult.MessageType == WebSocketMessageType.Close)
  {
    await discordWebSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
    Console.WriteLine("WebSocket connection closed.");
  }
  else
  {
    var message = System.Text.Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
    Console.WriteLine($"Received message: {message}");
  }
}

internal record GatewayDto(
  [property: JsonPropertyName("url")] string Url
);