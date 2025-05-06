using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class DiscordEventConverter : JsonConverter<DiscordEvent>
{
  public override DiscordEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    using var jsonDocument = JsonDocument.ParseValue(ref reader);
    var rootElement = jsonDocument.RootElement;
    var op = rootElement.GetProperty("op").GetInt32();
    return op switch
    {
      10 => JsonSerializer.Deserialize<HelloDiscordEvent>(rootElement.GetRawText(), options),
      11 => JsonSerializer.Deserialize<HeartbeatAckDiscordEvent>(rootElement.GetRawText(), options),
      0 => DeserializeDispatchEvent(rootElement, options),
      _ => JsonSerializer.Deserialize<DiscordEvent>(rootElement.GetRawText(), options),
    };
  }

  public override void Write(Utf8JsonWriter writer, DiscordEvent value, JsonSerializerOptions options)
  {
    JsonSerializer.Serialize(writer, value, value.GetType(), options);
  }

  private static DiscordEvent? DeserializeDispatchEvent(
    JsonElement rootElement,
    JsonSerializerOptions options
  )
  {
    var type = rootElement.GetProperty("t").GetString();

    return type switch
    {
      "READY" => JsonSerializer.Deserialize<ReadyDiscordEvent>(rootElement.GetRawText(), options),
      _ => JsonSerializer.Deserialize<DiscordEvent>(rootElement.GetRawText(), options),
    };
  }
}

internal record DiscordEvent
{
  [JsonPropertyName("op")]
  public int Op { get; init; }

  [JsonPropertyName("s")]
  public int? Sequence { get; init; }

  [JsonPropertyName("t")]
  public string? Type { get; init; }
}

internal record HeartbeatAckDiscordEvent : DiscordEvent
{
}

internal record HelloDiscordEvent : DiscordEvent
{
  [JsonPropertyName("d")]
  public HelloData Data { get; init; } = new HelloData();
}

internal record HelloData
{
  [JsonPropertyName("heartbeat_interval")]
  public int HeartbeatInterval { get; init; }
}

internal record ReadyDiscordEvent : DiscordEvent
{
  [JsonPropertyName("d")]
  public ReadyData Data { get; init; } = new ReadyData();
}

internal record ReadyData
{
  [JsonPropertyName("v")]
  public int Version { get; init; }

  [JsonPropertyName("session_id")]
  public string SessionId { get; init; } = string.Empty;

  [JsonPropertyName("resume_gateway_url")]
  public string ResumeGatewayUrl { get; init; } = string.Empty;
}


// TODO: We can probably have a base
// class for all events
// then additional base classes
// for events that we receivie vs send
internal record HeartbeatEvent
{
  [JsonPropertyName("op")]
  public int Op { get; } = 1;

  [JsonPropertyName("d")]
  public int? Sequence { get; init; }

  public HeartbeatEvent(int? sequence)
  {
    Sequence = sequence;
  }
}

internal record IdentifyEvent
{
  [JsonPropertyName("op")]
  public int Op { get; } = 2;

  [JsonPropertyName("d")]
  public IdentifyData Data { get; init; } = new IdentifyData();

  public IdentifyEvent(string token, int intents)
  {
    Data = new IdentifyData
    {
      Token = token,
      Intents = intents,
    };
  }
}

internal record IdentifyData
{
  [JsonPropertyName("token")]
  public string Token { get; init; } = string.Empty;

  [JsonPropertyName("properties")]
  public IdentifyProperties Properties { get; init; } = new IdentifyProperties();

  [JsonPropertyName("intents")]
  public int Intents { get; init; }
}

internal record IdentifyProperties
{
  [JsonPropertyName("os")]
  public string Os { get; init; } = Environment.OSVersion.ToString();

  [JsonPropertyName("browser")]
  public string Browser { get; init; } = Assembly.GetExecutingAssembly().GetName().FullName;

  [JsonPropertyName("device")]
  public string Device { get; init; } = Assembly.GetExecutingAssembly().GetName().FullName;
}