using System.Net.WebSockets;

internal interface IWebSocketFactory
{
  IWebSocket Create();
}

internal class WebSocketFactory : IWebSocketFactory
{
  public IWebSocket Create()
  {
    return new WebSocket();
  }
}