namespace SystemOne.Ollama;

public sealed record NoulQuestion(
    string Instructions,
    NoulCriteria? Criteria = null) : SystemOneQuestion(Instructions);