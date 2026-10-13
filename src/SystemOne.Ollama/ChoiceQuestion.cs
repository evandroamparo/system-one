namespace SystemOne.Ollama;

public sealed record ChoiceQuestion(
    string Instructions,
    IReadOnlyDictionary<string, string?> Criteria) : SystemOneQuestion(Instructions);