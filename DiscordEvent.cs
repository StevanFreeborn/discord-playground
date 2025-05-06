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
      _ => JsonSerializer.Deserialize<DiscordEvent>(rootElement.GetRawText(), options),
    };
  }

  public override void Write(Utf8JsonWriter writer, DiscordEvent value, JsonSerializerOptions options)
  {
    JsonSerializer.Serialize(writer, value, value.GetType(), options);
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