using System.Diagnostics;
using System.Text.Json;
using SystemOne.Ollama;

internal static class Program
{
    private static readonly (string Sender, string Text)[] MessageFeed =
    [
        ("Mira", "Good morning! Has anyone tried the new release yet?"),
        ("Devon", "Here are the release notes: https://example.com/release-notes"),
        ("Kai", "You're an idiot. Nobody wants to hear from you."),
        ("Rae", "Limited offer! Visit example.invalid and claim your prize now!"),
        ("Mira", "Could we keep the discussion focused on the bug report?"),
        ("Devon", "Thanks, that fixed it. I appreciate the help."),
    ];

    private static async Task<int> Main()
    {
        string model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "tev1:0.8b";
        string baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434/";

        using HttpClient httpClient = new()
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMinutes(2),
        };

        DecisionClient client = new(httpClient);
        using CancellationTokenSource cancellation = new();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        Console.WriteLine($"Replaying {MessageFeed.Length} chat messages with model '{model}'. Press Ctrl+C to stop.");

        try
        {
            for (int index = 0; index < MessageFeed.Length; index++)
            {
                cancellation.Token.ThrowIfCancellationRequested();

                (string sender, string text) = MessageFeed[index];
                ChatMessage message = new(DateTimeOffset.Now, sender, text);
                SystemOneRequest<ChatMessage> request = new(
                    Model: model,
                    State: message,
                    Questions: BuildQuestions());

                Console.WriteLine();
                Console.WriteLine($"[{message.Timestamp:HH:mm:ss}] {message.Sender}: {message.Text}");

                Stopwatch timer = Stopwatch.StartNew();
                SystemOneResponse response = await client.AskAsync(request, cancellation.Token);
                timer.Stop();

                PrintAssessment(response, timer.ElapsedMilliseconds);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Console.WriteLine("Stopped.");
        }
        catch (HttpRequestException exception)
        {
            Console.Error.WriteLine($"Could not get a System One decision: {exception.Message}");
            Console.Error.WriteLine("Check that Ollama is running and that the selected model has been pulled.");
            return 1;
        }
        catch (JsonException exception)
        {
            Console.Error.WriteLine($"Could not read the System One response: {exception.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        return 0;
    }

    private static Dictionary<string, SystemOneQuestion> BuildQuestions()
    {
        return new Dictionary<string, SystemOneQuestion>
        {
            ["category"] = new ChoiceQuestion(
                Instructions: "Classify this chat message for moderation.",
                Criteria: new Dictionary<string, string?>
                {
                    ["allow"] = "No moderation action is indicated",
                    ["spam"] = "Unsolicited promotion, scams, or repetitive spam",
                    ["harassment"] = "Insults or targeted abusive behavior",
                    ["other"] = "Another kind of message that needs moderator attention",
                }),
            ["policy_violation"] = new NoulQuestion(
                Instructions: "Does this message violate the chat rules?",
                Criteria: new NoulCriteria(
                    False: "The message follows the rules: be respectful, stay on topic, and do not post unsolicited promotions",
                    True: "The message violates one or more of those rules")),
            ["severity"] = new ScoreQuestion(
                Instructions: "Score the severity of any policy violation. Use 0 when there is no violation.",
                Criteria:
                [
                    "No violation",
                    "Minor or ambiguous concern",
                    "Clear but low-impact violation",
                    "Serious violation that should be reviewed",
                    "Severe violation requiring immediate moderator attention",
                ]),
        };
    }

    private static void PrintAssessment(SystemOneResponse response, long elapsedMilliseconds)
    {
        Console.WriteLine($"Model: {response.Model}");

        if (response.Answers.TryGetValue("category", out SystemOneAnswer? categoryAnswer)
            && categoryAnswer is ChoiceAnswer category)
        {
            Console.WriteLine($"Category: {category.Choice} (confidence: {category.Confidence:P1})");
            foreach ((string option, double probability) in category.Probabilities)
            {
                Console.WriteLine($"  {option}: {probability:P1}");
            }
        }

        if (response.Answers.TryGetValue("policy_violation", out SystemOneAnswer? violationAnswer)
            && violationAnswer is NoulAnswer violation)
        {
            Console.WriteLine($"Policy violation probability: {violation.Noul:P1}");
        }

        if (response.Answers.TryGetValue("severity", out SystemOneAnswer? severityAnswer)
            && severityAnswer is ScoreAnswer severity)
        {
            Console.WriteLine($"Severity: {severity.Score:F2} / {severity.Legend.Count - 1} (confidence: {severity.Confidence:P1})");
            foreach ((string level, double probability) in severity.Probabilities)
            {
                string description = severity.Legend.TryGetValue(level, out string? label)
                    ? $" - {label}"
                    : string.Empty;
                Console.WriteLine($"  {level}{description}: {probability:P1}");
            }
        }

        Console.WriteLine($"Decision time: {elapsedMilliseconds} ms");
    }

    private sealed record ChatMessage(DateTimeOffset Timestamp, string Sender, string Text);
}