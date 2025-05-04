using System.Text.Json;
using System.Text.Json.Serialization;

// TODO: Make polymorphic and represent
// each known op code as strongly typed
// event.

// TODO: Implement a JSON converter
// to handle polymorphic deserialization
// based on the op code.
internal record DiscordEvent(
  [property: JsonPropertyName("op")] int? Op,
  [property: JsonPropertyName("d")] JsonElement? Data,
  [property: JsonPropertyName("s")] int? Sequence,
  [property: JsonPropertyName("t")] string? Type
);
