using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
  private readonly JsonSerializerOptions _jsonSerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
    Converters =
    {
      new DiscordEventConverter(),
    },
  };

  private string _gatewayUrl = string.Empty;
  private IWebSocket? _webSocket;
  private Task? _receiveTask;
  private DateTime _timeLastHeartbeatSent = DateTime.MinValue;
  private DateTime _timeLastHeartbeatAcknowledged = DateTime.MinValue;
  private CancellationTokenSource? _heartbeatCts;
  private Task? _heartbeatTask;
  private string _sessionId = string.Empty;
  private string _resumeGatewayUrl = string.Empty;

  // TODO: Need to add asynchronous lock to
  // deal with synchronization of shared
  // state between different threads

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
        var bytesWritten = 0;

        do
        {
          result = await _webSocket.ReceiveAsync(new(messageBuffer), cancellationToken);

          if (result.MessageType is WebSocketMessageType.Close)
          {
            // TODO: If close status is 1000 or 1001 we cannot resume.
            // if 1000 or 1001 we should close the connection and reconnect
            // else we should close the connection and attempt to resume

            if (result.CloseStatus is WebSocketCloseStatus.NormalClosure or WebSocketCloseStatus.EndpointUnavailable)
            {
              return;
            }

            return;
          }

          if (result.MessageType is WebSocketMessageType.Text)
          {
            memoryStream.Write(messageBuffer, 0, result.Count);
            bytesWritten += result.Count;
          }

        } while (result.EndOfMessage is false);

        memoryStream.Seek(0, SeekOrigin.Begin);

        var e = await JsonSerializer.DeserializeAsync<DiscordEvent>(
          memoryStream,
          _jsonSerializerOptions,
          cancellationToken
        );

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
      _logger.LogError(ex, "WebSocket error");
      throw new DiscordClientException("WebSocket error.", ex);
    }
    catch (OperationCanceledException ex)
    {
      _logger.LogInformation(ex, "Receive messages operation canceled: {Message}", ex.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error while receiving messages");
      throw new DiscordClientException("Unexpected error while receiving messages.", ex);
    }
  }

  private async Task HandleEventAsync(DiscordEvent e, CancellationToken cancellationToken)
  {
    if (e is HelloDiscordEvent he)
    {
      StartHeartbeat(he, cancellationToken);
      await IdentifyAsync(cancellationToken);
      return;
    }

    if (e is HeartbeatAckDiscordEvent hae)
    {
      _timeLastHeartbeatAcknowledged = DateTime.UtcNow;
      _logger.LogInformation("Heartbeat acknowledged at {Time}", _timeLastHeartbeatAcknowledged);
      return;
    }

    if (e is ReadyDiscordEvent re)
    {
      _sessionId = re.Data.SessionId;
      _resumeGatewayUrl = re.Data.ResumeGatewayUrl;
      _logger.LogInformation("Ready event received. Session ID: {SessionId}", re.Data.SessionId);
      return;
    }
  }

  private async Task IdentifyAsync(CancellationToken cancellationToken)
  {
    var e = new IdentifyEvent(_options.AppToken, _options.Intents);

    await SendJsonAsync(e, cancellationToken);

    _logger.LogInformation("Identify event sent.");
  }

  private void StartHeartbeat(HelloDiscordEvent helloEvent, CancellationToken cancellationToken)
  {
    _heartbeatCts?.Cancel();
    _heartbeatCts?.Dispose();

    _heartbeatCts = new CancellationTokenSource();
    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _heartbeatCts.Token);

    _heartbeatTask = Task.Run(async () =>
    {
      _logger.LogInformation("Starting heartbeat task.");

      while (_webSocket?.State is WebSocketState.Open && linkedCts.Token.IsCancellationRequested is false)
      {
        try
        {

          var jitter = Random.Shared.Next(0, 1);
          await Task.Delay(helloEvent.Data.HeartbeatInterval + jitter);

          if (_timeLastHeartbeatAcknowledged < _timeLastHeartbeatSent)
          {
            if (_webSocket?.State is WebSocketState.Open)
            {
              _logger.LogWarning("Heartbeat not acknowledged. Closing WebSocket.");
              await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Heartbeat not acknowledged", CancellationToken.None);
            }
            break;
          }

          _timeLastHeartbeatSent = await SendHeartbeatAsync(helloEvent.Sequence, linkedCts.Token);

          _logger.LogInformation("Heartbeat sent at {Time}", _timeLastHeartbeatSent);
        }
        catch (OperationCanceledException ex)
        {
          _logger.LogInformation(ex, "Heartbeat task canceled");
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error in heartbeat task: {Message}", ex.Message);
          // TODO: Attempt to reconnect
        }
      }
    }, linkedCts.Token);
  }

  private async Task<DateTime> SendHeartbeatAsync(int? sequence, CancellationToken cancellationToken)
  {
    var heartbeat = new HeartbeatEvent(sequence);
    await SendJsonAsync(heartbeat, cancellationToken);
    return DateTime.UtcNow;
  }

  private async Task SendJsonAsync(object data, CancellationToken cancellationToken)
  {
    if (_webSocket?.State is not WebSocketState.Open)
    {
      _logger.LogWarning("WebSocket is not open. Cannot send message.");
      return;
    }

    try
    {
      var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
      var bytes = Encoding.UTF8.GetBytes(json);
      var buffer = new ArraySegment<byte>(bytes);
      await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
    }
    catch (OperationCanceledException ex)
    {
      _logger.LogInformation(ex, "Send operation canceled");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to send message: {Message}", ex.Message);
      throw new DiscordClientException("Failed to send message.", ex);
    }
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
    _heartbeatCts?.Cancel();
    _heartbeatCts?.Dispose();
    _heartbeatTask?.Dispose();
    _receiveTask?.Dispose();
    _webSocket?.Dispose();
  }
}