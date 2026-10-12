# Ollama System One with .NET 10

This runnable .NET 10 console sample sends typed C# request objects to Ollama's `POST /v1/systemone` endpoint and deserializes the typed response. `DesicionClient` wraps `HttpClient` for the request.

## Prerequisites

- .NET 10 SDK
- Ollama v0.35.0 or later
- The `nimble` model pulled locally

Pull the recommended model:

```powershell
ollama pull nimble
```

The local server listens at `http://localhost:11434` by default. Build and run the sample:

```powershell
dotnet build

```

The program asks three questions about the same support ticket: which team should handle it, whether a refund was explicitly requested, and how urgent it is. You can select another local model or server URL with environment variables:

```powershell
$env:OLLAMA_MODEL = "tev1:4b"
$env:OLLAMA_BASE_URL = "http://localhost:11434/"
dotnet run
```

For Bash:

```bash
OLLAMA_MODEL=tev1:4b OLLAMA_BASE_URL=http://localhost:11434/ dotnet run
```

Local requests do not require an API key. This sample calls the System One endpoint directly.

## What Is System One?

System One is a local API for decision models. Instead of asking a chat model to write a free-form answer, an application supplies:

- `state`: the content to evaluate, such as text or structured JSON data;
- `questions`: one or more named questions, each with a type, instructions, and, where applicable, explicit criteria.

The model scores the allowed outcomes. This fits classification, routing, Boolean checks, and rubric-based evaluation. System One is not a conversation: questions do not receive one another's answers, and the model does not produce free-form explanations. Implement cross-answer consistency checks or explanations in application code when required.

### Question Types

| Type | Purpose | Result |
| --- | --- | --- |
| `choice` | Select among named options with descriptions | Selected option, probabilities for each option, and confidence |
| `noul` | Estimate a yes/no outcome | `noul`, the probability that the outcome is `true`, from 0 to 1 |
| `score` | Evaluate ordered levels from lowest to highest | `score`, the expected value on a zero-based scale, probabilities per level, and confidence |

For `choice`, `criteria` maps stable option keys to descriptions. Include a `none` option when none of the normal options may apply. For `noul`, descriptions of `true` and `false` are optional. For `score`, array order defines the scale: three criteria produce levels 0, 1, and 2. The result is a probability-weighted average, so it can be fractional rather than a whole level.

Probabilities are normalized across the supplied candidates, subject to floating-point precision. `confidence`, returned for `choice` and `score`, describes how concentrated that distribution is. It is not a calibrated probability that the answer is correct, so a high value is not a guarantee.

## Why Use a Decision Model?

Chat models typically generate tokens until they produce a natural-language response, which application code must then interpret. System One models are tuned to score application-defined alternatives and return structured decisions with probabilities. This can simplify classification tasks and makes the accepted outcomes explicit.

The model is still probabilistic and can be wrong. Use clear instructions, distinct criteria, and representative examples. Measure error rates and validate any confidence threshold on your own data. Keep suitable review and controls in place for high-impact decisions, including decisions in security, health, and finance.

## Model Choice

This sample recommends `nimble`, a 9B-parameter decision model. Its Ollama download is listed at approximately 9.3–9.5 GB. Download size is not total inference memory: the runtime, loaded context, and other processes also consume RAM. Nimble is a reasonable local decision-model choice to evaluate with 32 GB, but CPU latency depends on the processor, quantization, and input size; do not expect GPU-level latency.

Nimble specializes in text decisions and is fine-tuned from Qwen3.5-9B. The Ollama tag listing reports a 256K context window, while the model notes say a decision prompt must fit within 8,192 tokens. Because these published values differ, treat 8,192 tokens as the conservative limit, keep state and instructions concise, and verify behavior for the installed tag. Each question is scored with the state and question set in context.

A smaller alternative is `tev1:4b`, listed at approximately 4.4–4.5 GB and also trained for decision tasks. Results depend on the data and task, so benchmark both on your own domain before choosing for quality or latency. `tev1:0.8b` is smaller still, but has lower published average performance. Author-reported benchmarks are not a substitute for your own evaluation.

## Project Structure

- [Program.cs](Program.cs) builds a typed state, combines all three question types, and prints the results.
- [DesicionClient.cs](DesicionClient.cs) wraps `HttpClient`, posts JSON to `v1/systemone`, and deserializes the response.
- Each model type has a dedicated source file, such as [SystemOneRequest.cs](SystemOneRequest.cs), [ChoiceQuestion.cs](ChoiceQuestion.cs), and [ChoiceAnswer.cs](ChoiceAnswer.cs).
- [SystemOne.Ollama.csproj](SystemOne.Ollama.csproj) targets `net10.0`. No additional NuGet packages are required.

The `JsonPolymorphic` and `JsonDerivedType` attributes map the JSON `type` field to concrete C# question and answer types. Responses deserialize as `ChoiceAnswer`, `NoulAnswer`, or `ScoreAnswer` instead of untyped dictionaries. The client accepts a caller-configured `HttpClient`, so larger applications can control its base address, timeout, and lifetime.

## Why `HttpClient` Instead of the OpenAI SDK?

The official OpenAI .NET SDK allows a custom endpoint/base URL and provides typed methods for OpenAI-compatible operations such as chat completions. That does not mean it supports arbitrary routes. System One uses the dedicated `POST /v1/systemone` route, which is not exposed as a typed operation by the OpenAI SDK. This sample therefore uses `HttpClient` and its own typed models rather than pretending a chat-completion call exercises System One.

For Ollama's OpenAI-compatible endpoints, see the [Ollama OpenAI compatibility documentation](https://docs.ollama.com/openai) and the [official OpenAI .NET SDK](https://github.com/openai/openai-dotnet).

## Important Limits

- The System One API requires Ollama v0.35.0 or later; images with Clef models require v0.35.1 or later. This sample uses text and Nimble.
- A request accepts 1 to 64 named questions. Option limits vary by model; follow the limits for the model in use.
- Requests without images are limited to 64 KiB, and input is not automatically truncated. The input must also fit in the loaded model context.
- System One returns a complete JSON response; streaming and typical generation controls are not available on this route.
- This sample does not set `keep_alive`; Ollama uses its configured default.

## References

- [Ollama: System One API](https://docs.ollama.com/api/systemone)
- [Ollama: Decision capability](https://docs.ollama.com/capabilities/decision)
- [Ollama Library: Nimble](https://ollama.com/library/nimble)
- [Ollama Library: Tev1](https://ollama.com/library/tev1)
- [Ollama: OpenAI compatibility](https://docs.ollama.com/openai)
- [OpenAI .NET SDK](https://github.com/openai/openai-dotnet)
