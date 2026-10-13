namespace SystemOne.Ollama;

public sealed record SystemOneResponse(
    string Model,
    IReadOnlyDictionary<string, SystemOneAnswer> Answers,
    SystemOneUsage Usage);