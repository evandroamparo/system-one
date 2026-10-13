using System.Diagnostics;
using System.Globalization;
using SystemOne.Ollama;

internal static class Program
{
    private static readonly int[][] WinningLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6],
    ];

    private static async Task<int> Main()
    {
        string model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "nimble";
        string baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434/";

        using HttpClient httpClient = new()
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMinutes(2),
        };

        DesicionClient client = new(httpClient);
        char[] board = new char[9];
        Array.Fill(board, ' ');
        char currentPlayer = 'X';

        Console.WriteLine("Tic-tac-toe: you are X; System One is O.");

        while (true)
        {
            PrintBoard(board);
            int move;

            if (currentPlayer == 'X')
            {
                move = ReadHumanMove(board);
                if (move < 0)
                {
                    return 0;
                }
            }
            else
            {
                Dictionary<string, string?> criteria = Enumerable.Range(0, board.Length)
                    .Where(index => board[index] == ' ')
                    .ToDictionary(
                        index => (index + 1).ToString(CultureInfo.InvariantCulture),
                        index => (string?)$"Row {index / 3 + 1}, column {index % 3 + 1}",
                        StringComparer.Ordinal);

                SystemOneRequest<TicTacToeState> request = new(
                    Model: model,
                    State: new TicTacToeState(
                        Board:
                        [
                            new string(board, 0, 3).Replace(' ', '-'),
                            new string(board, 3, 3).Replace(' ', '-'),
                            new string(board, 6, 3).Replace(' ', '-'),
                        ],
                        Player: "O"),
                    Questions: new Dictionary<string, SystemOneQuestion>
                    {
                        ["move"] = new ChoiceQuestion(
                            Instructions: "The board rows use X, O, and - for empty. Choose one available square for O. Win if possible; otherwise block X. Return a legal option key.",
                            Criteria: criteria),
                    });

                Stopwatch timer = Stopwatch.StartNew();
                SystemOneResponse response;
                try
                {
                    response = await client.AskAsync(request);
                }
                catch (HttpRequestException exception)
                {
                    Console.Error.WriteLine($"Could not get a System One decision: {exception.Message}");
                    Console.Error.WriteLine("Check that Ollama is running and that the selected model has been pulled.");
                    return 1;
                }

                timer.Stop();

                if (!response.Answers.TryGetValue("move", out SystemOneAnswer? answer)
                    || answer is not ChoiceAnswer choice
                    || !criteria.ContainsKey(choice.Choice)
                    || !int.TryParse(choice.Choice, NumberStyles.None, CultureInfo.InvariantCulture, out int square))
                {
                    Console.Error.WriteLine("System One returned a move that was not one of the available squares.");
                    return 1;
                }

                move = square - 1;
                Console.WriteLine($"System One chose square {square} in {timer.ElapsedMilliseconds} ms (confidence: {choice.Confidence:P1}).");
            }

            board[move] = currentPlayer;

            char? winner = GetWinner(board);
            if (winner is char winningPlayer)
            {
                PrintBoard(board);
                Console.WriteLine(winningPlayer == 'X' ? "You win." : "System One wins.");
                return 0;
            }

            if (Array.TrueForAll(board, square => square != ' '))
            {
                PrintBoard(board);
                Console.WriteLine("Draw.");
                return 0;
            }

            currentPlayer = currentPlayer == 'X' ? 'O' : 'X';
        }
    }

    private static int ReadHumanMove(char[] board)
    {
        while (true)
        {
            Console.Write("Choose an empty square (1-9), or press Enter to quit: ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return -1;
            }

            if (int.TryParse(input, out int square)
                && square is >= 1 and <= 9
                && board[square - 1] == ' ')
            {
                return square - 1;
            }

            Console.WriteLine("That square is not available.");
        }
    }

    private static void PrintBoard(char[] board)
    {
        Console.WriteLine();
        for (int row = 0; row < 3; row++)
        {
            string[] cells = Enumerable.Range(row * 3, 3)
                .Select(index => board[index] == ' ' ? (index + 1).ToString(CultureInfo.InvariantCulture) : board[index].ToString())
                .ToArray();
            Console.WriteLine($" {cells[0]} | {cells[1]} | {cells[2]} ");
            if (row < 2)
            {
                Console.WriteLine("---+---+---");
            }
        }

        Console.WriteLine();
    }

    private static char? GetWinner(char[] board)
    {
        foreach (int[] line in WinningLines)
        {
            char mark = board[line[0]];
            if (mark != ' ' && mark == board[line[1]] && mark == board[line[2]])
            {
                return mark;
            }
        }

        return null;
    }
}

internal sealed record TicTacToeState(string[] Board, string Player);