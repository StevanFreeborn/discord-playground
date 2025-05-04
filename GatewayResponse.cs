using System.Text.Json.Serialization;

internal record GatewayResponse(
  [property: JsonPropertyName("url")] string Url
);