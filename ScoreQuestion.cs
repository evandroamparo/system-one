namespace SystemOne.Ollama;

public sealed record ScoreQuestion(
    string Instructions,
    IReadOnlyList<string> Criteria) : SystemOneQuestion(Instructions);