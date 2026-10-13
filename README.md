# System One with .NET

This repository demonstrates how to use **Ollama System One** in .NET applications.

## What is System One?

System One is Ollama's decision API. An application provides a state and questions with explicit criteria; the model evaluates them and returns structured results, such as a choice, a yes/no probability, or a score. Unlike a chat response, the result is designed for applications to consume directly.

## Run the projects

Prerequisites: .NET 10 SDK and Ollama v0.35.0 or later running locally. Pull the models used by the examples:

```powershell
ollama pull nimble
ollama pull tev1:4b
```

From the repository root, build the solution:

```powershell
dotnet build SystemOne.sln
```

Ollama uses `http://localhost:11434/` by default. To use a different model or server URL, set `OLLAMA_MODEL` or `OLLAMA_BASE_URL` before running an example.

## Examples

### Support ticket classification

```powershell
dotnet run --project examples/SupportTicket/SupportTicket.csproj
```

Submits a support ticket and evaluates three aspects: the responsible team (`choice`), whether a refund was explicitly requested (`noul`), and urgency (`score`).

Illustrative output:

```text
Model: nimble
Input tokens: 245
Team: billing (confidence: 82.4%)
  billing: 82.4%
  technical: 12.1%
  other: 5.5%
Probability refund was requested: 98.7%
Urgency score: 1.42 (scale 0-2)
Score confidence: 71.3%
```

### Tic-tac-toe

```powershell
dotnet run --project examples/TicTacToe/TicTacToe.csproj
```

You play as `X`; System One chooses an empty square as `O`. The application provides the legal moves and validates the result locally.

Illustrative output:

```text
Tic-tac-toe: you are X; System One is O.

 1 | 2 | 3
---+---+---
 4 | 5 | 6
---+---+---
 7 | 8 | 9

Choose an empty square (1-9), or press Enter to quit: 1
System One chose square 5 in 1250 ms (confidence: 76.0%).
```

Output varies by model and environment; the values above are examples, not guaranteed results.

## Project structure

- `src/SystemOne.Ollama`: shared HTTP client and models.
- `examples/SupportTicket`: classification using `choice`, `noul`, and `score`.
- `examples/TicTacToe`: choosing among legal moves in a game.

## References

- [System One API](https://docs.ollama.com/api/systemone)
- [Decision capability](https://docs.ollama.com/capabilities/decision)
- [Nimble model](https://ollama.com/library/nimble)
- [Tev1 model](https://ollama.com/library/tev1)