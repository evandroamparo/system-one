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
                Dictionary<string, string?> criteria = BuildMoveCriteria(board);
                string boardState = string.Join(" / ",
                    Enumerable.Range(0, 3)
                        .Select(row => new string(board, row * 3, 3).Replace(' ', '-')));

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
                            Instructions:
                                "You are playing Tic-Tac-Toe as O. " +
                                "The board is a 3x3 grid where '-' means empty, 'X' is the human, and 'O' is you. " +
                                "The current board is: " + boardState + ". " +
                                "Your move must be one of the legal options only: " + string.Join(", ", criteria.Keys) + ". " +
                                "Choose the best move using this priority order: " +
                                "1) win immediately if you can, 2) block the human's immediate win if they have one next, " +
                                "3) take the center if available, 4) take a corner, 5) take any remaining legal square. " +
                                "Return only the numeric key of the chosen square, never a sentence or a board. " +
                                "Do not choose a square that is already occupied.",
                            Criteria: criteria),
                    });

                Stopwatch timer = Stopwatch.StartNew();
                SystemOneResponse response;
                try
                {
                    Console.Write("💭");
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
                    int fallbackMove = ChooseBestMove(board, 'O');
                    Console.Error.WriteLine("System One returned a move that was not one of the available squares or did not follow the legal options. Falling back to local tactical move.");
                    Console.WriteLine($"Fallback move: {fallbackMove + 1} in {timer.ElapsedMilliseconds} ms.");
                    move = fallbackMove;
                }
                else
                {
                    move = square - 1;
                    Console.WriteLine($"System One chose square {square} in {timer.ElapsedMilliseconds} ms (confidence: {choice.Confidence:P1}).");
                }
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

    private static Dictionary<string, string?> BuildMoveCriteria(char[] board)
    {
        return Enumerable.Range(0, board.Length)
            .Where(index => board[index] == ' ')
            .ToDictionary(
                index => (index + 1).ToString(CultureInfo.InvariantCulture),
                index => (string?)$"Square {(index + 1)} (row {index / 3 + 1}, column {index % 3 + 1})",
                StringComparer.Ordinal);
    }

    private static int ChooseBestMove(char[] board, char player)
    {
        int[] legalMoves = Enumerable.Range(0, board.Length)
            .Where(index => board[index] == ' ')
            .ToArray();

        foreach (int move in legalMoves)
        {
            board[move] = player;
            if (GetWinner(board) == player)
            {
                board[move] = ' ';
                return move;
            }
            board[move] = ' ';
        }

        char opponent = player == 'X' ? 'O' : 'X';
        foreach (int move in legalMoves)
        {
            board[move] = opponent;
            if (GetWinner(board) == opponent)
            {
                board[move] = ' ';
                return move;
            }
            board[move] = ' ';
        }

        int[] preferred = [4, 0, 2, 6, 8, 1, 3, 5, 7];
        foreach (int move in preferred)
        {
            if (board[move] == ' ')
            {
                return move;
            }
        }

        return legalMoves[0];
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