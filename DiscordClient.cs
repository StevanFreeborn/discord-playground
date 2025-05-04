using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;

using Microsoft.Extensions.Logging;

internal interface IDiscordClient : IDisposable
{
  Task ConnectAsync(CancellationToken cancellationToken);
}

internal class DiscordClient : IDiscordClient, IDisposable
{
  private readonly DiscordClientOptions _options;
  private readonly IHttpClientFactory _httpClientFactory;
  private readonly IWebSocketFactory _webSocketFactory;
  private readonly ILogger<DiscordClient> _logger;

  private string _gatewayUrl = string.Empty;
  private IWebSocket? _webSocket;
  private Task? _receiveTask;

  public DiscordClient(
    DiscordClientOptions options,
    IHttpClientFactory httpClientFactory,
    IWebSocketFactory webSocketFactory,
    ILogger<DiscordClient> logger
  )
  {
    _options = options ?? throw new ArgumentNullException(nameof(options));
    _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    _webSocketFactory = webSocketFactory ?? throw new ArgumentNullException(nameof(webSocketFactory));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task ConnectAsync(CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrEmpty(_gatewayUrl))
    {
      _gatewayUrl = await GetGatewayUrlAsync(cancellationToken);
    }

    _webSocket = _webSocketFactory.Create();

    var uri = new Uri(_gatewayUrl);
    await _webSocket.ConnectAsync(uri, cancellationToken);

    _logger.LogInformation("Connected to Discord Gateway at {GatewayUrl}", _gatewayUrl);

    _receiveTask = ReceiveMessagesAsync(cancellationToken);
  }

  private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
  {
    try
    {
      var messageBuffer = new byte[8192];

      while (_webSocket?.State == WebSocketState.Open && cancellationToken.IsCancellationRequested is false)
      {
        // websocket message might be larger than the
        // size of the buffer so we need to loop until
        // we receive the end of the message and
        // write the data we receive on each iteration
        // to the memory stream
        using var memoryStream = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
          result = await _webSocket.ReceiveAsync(new(messageBuffer), cancellationToken);

          if (result.MessageType is WebSocketMessageType.Close)
          {
            _logger.LogInformation("WebSocket closed: {Message}", result.CloseStatusDescription);
            return;
          }

          if (result.MessageType is WebSocketMessageType.Text)
          {
            memoryStream.Write(messageBuffer, 0, result.Count);
          }

        } while (result.EndOfMessage is false);

        memoryStream.Seek(0, SeekOrigin.Begin);

        var e = await JsonSerializer.DeserializeAsync<DiscordEvent>(memoryStream, cancellationToken: cancellationToken);

        if (e is null)
        {
          _logger.LogInformation("Received null event.");
          continue;
        }

        await HandleEventAsync(e, cancellationToken);
      }
    }
    catch (WebSocketException ex)
    {
      _logger.LogError(ex, "WebSocket error: {Message}", ex.Message);
      throw new DiscordClientException("WebSocket error.", ex);
    }
    catch (OperationCanceledException ex)
    {
      _logger.LogInformation(ex, "Receive messages operation canceled: {Message}", ex.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error while receiving messages: {Message}", ex.Message);
      throw new DiscordClientException("Unexpected error while receiving messages.", ex);
    }
  }

  private Task HandleEventAsync(DiscordEvent e, CancellationToken cancellationToken)
  {
    // TODO: Handle different event types
    _logger.LogInformation("Received event: {Event}", e);
    return Task.CompletedTask;
  }

  private async Task<string> GetGatewayUrlAsync(CancellationToken cancellationToken)
  {
    var uri = new Uri("gateway", UriKind.Relative);
    using var httpClient = _httpClientFactory.CreateClient();
    httpClient.BaseAddress = new Uri(_options.ApiUrl);
    var response = await httpClient.GetAsync(uri, cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
      _logger.LogError("Failed to get gateway URL: {StatusCode}", response.StatusCode);
      throw new DiscordClientException("Failed to get gateway URL.");
    }

    var gatewayResponse = await response.Content.ReadFromJsonAsync<GatewayResponse>(cancellationToken);

    if (gatewayResponse is null)
    {
      _logger.LogError("Failed to deserialize gateway response.");
      throw new DiscordClientException("Failed to deserialize gateway response.");
    }

    return gatewayResponse.Url;
  }

  public void Dispose()
  {
    _receiveTask?.Dispose();
    _webSocket?.Dispose();
  }
}