using SystemOne.Ollama;

string model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "nimble";
string baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434/";

SupportTicket ticket = new(
    CustomerMessage: "I was charged twice. Please refund the extra payment.",
    ServiceStatus: "The checkout service is returning HTTP 500 errors.");

Dictionary<string, SystemOneQuestion> questions = new()
{
    ["intent"] = new ChoiceQuestion(
        Instructions: "Which support team should handle this ticket?",
        Criteria: new Dictionary<string, string?>
        {
            ["billing"] = "Payments, duplicate charges, and refunds",
            ["technical"] = "Software errors, outages, and integrations",
            ["other"] = "None of the listed teams applies",
        }),
    ["refund_requested"] = new NoulQuestion(
        Instructions: "Does the customer explicitly request a refund?",
        Criteria: new NoulCriteria(
            False: "No refund is requested",
            True: "The customer requests a refund")),
    ["urgency"] = new ScoreQuestion(
        Instructions: "How urgent is this request?",
        Criteria:
        [
            "Routine: no immediate service impact",
            "Soon: a customer is inconvenienced",
            "Immediate: a critical service is unavailable",
        ]),
};

SystemOneRequest<SupportTicket> request = new(
    Model: model,
    State: ticket,
    Questions: questions);

using HttpClient httpClient = new()
{
    BaseAddress = new Uri(baseUrl),
    Timeout = TimeSpan.FromMinutes(2),
};

DecisionClient client = new(httpClient);

try
{
    SystemOneResponse result = await client.AskAsync(request);

    Console.WriteLine($"Model: {result.Model}");
    Console.WriteLine($"Input tokens: {result.Usage.InputTokens}");

    if (result.Answers["intent"] is ChoiceAnswer intent)
    {
        Console.WriteLine($"Team: {intent.Choice} (confidence: {intent.Confidence:P1})");
        foreach ((string option, double probability) in intent.Probabilities)
        {
            Console.WriteLine($"  {option}: {probability:P1}");
        }
    }

    if (result.Answers["refund_requested"] is NoulAnswer refund)
    {
        Console.WriteLine($"Probability refund was requested: {refund.Noul:P1}");
    }

    if (result.Answers["urgency"] is ScoreAnswer urgency)
    {
        Console.WriteLine($"Urgency score: {urgency.Score:F2} (scale 0-{urgency.Legend.Count - 1})");
        Console.WriteLine($"Score confidence: {urgency.Confidence:P1}");
        foreach ((string option, double probability) in urgency.Probabilities)
        {
            Console.WriteLine($"  {option}: {probability:P1}");
        }
    }
}
catch (HttpRequestException exception)
{
    Console.Error.WriteLine($"Could not get a System One decision: {exception.Message}");
    Console.Error.WriteLine("Check that Ollama is running and that the selected model has been pulled.");
    return 1;
}

return 0;