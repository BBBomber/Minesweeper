using System;
using System.Collections.Generic;

public static class TathamPuzzleGenerator
{
    public static TathamPuzzle GeneratePuzzle(int width, int height, int mineCount,
                                              int safeX, int safeY,
                                              System.Random rng)
    {
        TathamPuzzle puzzle = new TathamPuzzle(width, height, mineCount);

        // We'll place mines randomly (except the safe cell). We can also exclude a 3x3 block if we want.
        List<(int x, int y)> coords = new List<(int x, int y)>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Exclude the first-click cell from being a mine
                if (!(x == safeX && y == safeY))
                {
                    coords.Add((x, y));
                }
            }
        }
        // Shuffle
        for (int i = 0; i < coords.Count; i++)
        {
            int swap = rng.Next(i, coords.Count);
            var temp = coords[i];
            coords[i] = coords[swap];
            coords[swap] = temp;
        }
        // place mines
        for (int i = 0; i < mineCount && i < coords.Count; i++)
        {
            var c = coords[i];
            puzzle.mines[c.x, c.y] = true;
        }

        // compute adjacency
        ComputeClues(puzzle);

        // Try solving. If TathamSolver says it's solvable with no guess, we accept
        bool solvable = TathamSolver.SolvePuzzleNoGuess(puzzle, safeX, safeY);
        if (!solvable)
        {
            return null;
        }
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
}
