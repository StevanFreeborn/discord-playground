using System.Net.WebSockets;

internal interface IWebSocket : IDisposable
{
  WebSocketState State { get; }

  Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
  Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
}

internal class WebSocket : IWebSocket
{
  private readonly ClientWebSocket _clientWebSocket;

  public WebSocketState State => _clientWebSocket.State;

  public WebSocket()
  {
    _clientWebSocket = new ClientWebSocket();
  }

  public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
  {
    await _clientWebSocket.ConnectAsync(uri, cancellationToken);
  }

  public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
  {
    return await _clientWebSocket.ReceiveAsync(buffer, cancellationToken);
  }

  public void Dispose()
  {
    _clientWebSocket.Dispose();
  }
}