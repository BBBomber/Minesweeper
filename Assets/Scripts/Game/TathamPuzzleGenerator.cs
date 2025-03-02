using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine; // For Debug.Log

public static class TathamPuzzleGenerator
{
    public static TathamPuzzle GeneratePuzzle(int width, int height, int mineCount,
                                               int safeX, int safeY,
                                               System.Random rng)
    {
        //Debug.Log($"[PuzzleGen] Starting puzzle generation. Board: {width}x{height}, Mines: {mineCount}, Safe cell: ({safeX}, {safeY})");

        TathamPuzzle puzzle = new TathamPuzzle(width, height, mineCount);

        // Build list of candidate coordinates (excluding the safe cell)
        List<(int x, int y)> coords = new List<(int x, int y)>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!(x == safeX && y == safeY))
                {
                    coords.Add((x, y));
                }
            }
        }
        //Debug.Log($"[PuzzleGen] Total candidate coordinates (excluding safe cell): {coords.Count}");

        // Shuffle the candidate list.
        for (int i = 0; i < coords.Count; i++)
        {
            int swap = rng.Next(i, coords.Count);
            var temp = coords[i];
            coords[i] = coords[swap];
            coords[swap] = temp;
        }

        // Place mines using the shuffled coordinates.
        int placedMines = 0;
        for (int i = 0; i < mineCount && i < coords.Count; i++)
        {
            var c = coords[i];
            puzzle.mines[c.x, c.y] = true;
            placedMines++;

            // Log every 10 placements for clarity.
            if (i % 10 == 0)
            {
                //Debug.Log($"[PuzzleGen] Placed mine at ({c.x},{c.y}). Total placed so far: {placedMines}");
            }
        }
       // Debug.Log($"[PuzzleGen] Total mines placed: {placedMines}");

        // Compute adjacency clues.
        ComputeClues(puzzle);
        //Debug.Log("[PuzzleGen] Clues computed.");

        // Try solving the puzzle using no-guess criteria.
        bool solvable = TathamSolver.SolvePuzzleNoGuess(puzzle, safeX, safeY);
        if (!solvable)
        {
            //Debug.Log("[PuzzleGen] Puzzle unsolvable with no-guess criteria. Returning null.");
            return null;
        }

        Debug.Log("[PuzzleGen] Puzzle generated and passed the solver successfully.");
        return puzzle;
    }


    // Standard adjacency
    private static void ComputeClues(TathamPuzzle puzzle)
    {
        for (int x = 0; x < puzzle.width; x++)
        {
            for (int y = 0; y < puzzle.height; y++)
            {
                if (puzzle.mines[x, y])
                {
                    puzzle.clues[x, y] = -1;
                }
                else
                {
                    int count = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (puzzle.InRange(nx, ny) && puzzle.mines[nx, ny])
                            {
                                count++;
                            }
                        }
                    }
                    puzzle.clues[x, y] = count;
                }
            }
        }
    }


    public static async Task<TathamPuzzle> GeneratePuzzleParallelAsync(int width, int height, int mineCount, int safeX, int safeY)
    {
        int parallelTasks = Math.Max(1, Environment.ProcessorCount);
        const int MAX_ATTEMPTS = 200; // Attempts per task

        using (CancellationTokenSource cts = new CancellationTokenSource())
        {
            CancellationToken token = cts.Token;
            List<Task<TathamPuzzle>> tasks = new List<Task<TathamPuzzle>>();

            // Launch a task for each available core.
            for (int i = 0; i < parallelTasks; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    System.Random rng = new System.Random();
                    TathamPuzzle puzzle = null;
                    for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        puzzle = GeneratePuzzle(width, height, mineCount, safeX, safeY, rng);
                        if (puzzle != null)
                        {
                            return puzzle;
                        }
                    }
                    return puzzle; // Likely null if no valid puzzle is generated.
                }, token));
            }

            // Wait until any task returns a valid puzzle.
            while (tasks.Count > 0)
            {
                Task<TathamPuzzle> finishedTask = await Task.WhenAny(tasks);
                TathamPuzzle result = null;
                try
                {
                    result = finishedTask.Result;
                }
                catch (OperationCanceledException)
                {
                    // Task was cancelled; ignore and continue.
                }

                if (result != null)
                {
                    // Cancel the remaining tasks.
                    cts.Cancel();
                    return result;
                }
                else
                {
                    // Remove the finished task and continue waiting.
                    tasks.Remove(finishedTask);
                }
            }
            return null;
        }
    }
}
