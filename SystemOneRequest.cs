namespace SystemOne.Ollama;

public sealed record SystemOneRequest<TState>(
    string Model,
    TState State,
    IReadOnlyDictionary<string, SystemOneQuestion> Questions);