namespace SystemOne.Ollama;

public sealed record ScoreAnswer(
    double Score,
    IReadOnlyDictionary<string, string> Legend,
    IReadOnlyDictionary<string, double> Probabilities,
    double Confidence) : SystemOneAnswer;