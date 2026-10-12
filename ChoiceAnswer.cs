namespace SystemOne.Ollama;

public sealed record ChoiceAnswer(
    string Choice,
    IReadOnlyDictionary<string, double> Probabilities,
    double Confidence) : SystemOneAnswer;