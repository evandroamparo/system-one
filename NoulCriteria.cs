using System.Text.Json.Serialization;

namespace SystemOne.Ollama;

public sealed record NoulCriteria(
    [property: JsonPropertyName("false")] string? False,
    [property: JsonPropertyName("true")] string? True);