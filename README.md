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

The state contains the ticket details:

```json
{
  "CustomerMessage": "I was charged twice. Please refund the extra payment.",
  "ServiceStatus": "The checkout service is returning HTTP 500 errors."
}
```

For the `intent` question, the instructions and criteria are:

```text
Instructions: Which support team should handle this ticket?
Criteria:
  billing: Payments, duplicate charges, and refunds
  technical: Software errors, outages, and integrations
  other: None of the listed teams applies
```

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

After X chooses square 1, the state for O's turn is:

```json
{
  "Board": ["X--", "---", "---"],
  "Player": "O"
}
```

The `move` question includes the current board and only the available squares:

```text
Instructions: You are playing Tic-Tac-Toe as O. The board is a 3x3 grid where...

Criteria:
  2: Square 2 (row 1, column 2)
  3: Square 3 (row 1, column 3)
  4: Square 4 (row 2, column 1)
  5: Square 5 (row 2, column 2)
  6: Square 6 (row 2, column 3)
  7: Square 7 (row 3, column 1)
  8: Square 8 (row 3, column 2)
  9: Square 9 (row 3, column 3)
```

The board, legal-move criteria, and board-specific instructions are rebuilt for each O turn.

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

### Live chat moderation

```powershell
dotnet run --project examples/LiveChatModeration/LiveChatModeration.csproj
```

Replays a short stream of sample chat messages, submitting each message as a separate event. For every message, System One classifies the moderation category (`choice`), estimates the probability of a rules violation (`noul`), and scores its severity (`score`). The example prints the complete assessment and request latency before sending the next event. The feed is local and deterministic; it does not connect to a live chat platform.

Illustrative output:

```text
Replaying 6 chat messages with model 'nimble'. Press Ctrl+C to stop.

[10:15:32.104] Mira: Good morning! Has anyone tried the new release yet?
Model: nimble
Category: allow (confidence: 91.2%)
Policy violation probability: 2.1%
Severity: 0.04 / 4 (confidence: 88.5%)
Decision time: 740 ms
Tokens: 198 in, 42 out
```

Latency and assessments vary by model and environment. Each event receives a complete response; the current client does not stream partial answers. Press Ctrl+C to stop the replay.

## Project structure

- `src/SystemOne.Ollama`: shared HTTP client and models.
- `examples/SupportTicket`: classification using `choice`, `noul`, and `score`.
- `examples/TicTacToe`: choosing among legal moves in a game.
- `examples/LiveChatModeration`: event-by-event chat moderation using all three question types.

## References

- [System One API](https://docs.ollama.com/api/systemone)
- [Decision capability](https://docs.ollama.com/capabilities/decision)
- [Nimble model](https://ollama.com/library/nimble)
- [Tev1 model](https://ollama.com/library/tev1)