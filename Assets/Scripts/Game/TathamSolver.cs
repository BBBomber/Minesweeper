
using System.Collections.Generic;

// This solver ensures that any puzzle for which it returns 'true' can be solved with zero guessing.
//
// It merges all adjacency constraints into one global system, then does backtracking with partial constraint
// checks, plus standard adjacency expansions. It also includes a "safe expansion" heuristic if it detects
// large unknown regions that can be trivially expanded.
public static class TathamSolver
{
    public static bool SolvePuzzleNoGuess(TathamPuzzle puzzle, int startX, int startY)
    {
        SolverState state = new SolverState(puzzle);

        // Reveal the start cell and do BFS expansion for zero clues
        state.RevealCell(startX, startY);
        if (puzzle.clues[startX, startY] == 0)
        {
            state.ExpandZeros(startX, startY);
        }

        // Repeatedly apply local adjacency logic and global constraint logic
        // If big unknown lumps remain, do a safe expansion heuristic
        bool progress;
        do
        {
            progress = false;
            progress |= state.ApplyLocalDeductions();
            progress |= state.ApplyGlobalConstraints();

            // Additional safe expansions for any zero we've newly revealed:
            progress |= state.ExpandAllCurrentZeros();
        }
        while (progress);

        // If we have revealed all safe squares, puzzle requires no guess
        return state.AllSafeSquaresRevealed();
    }

    private class SolverState
    {
        private TathamPuzzle puzzle;
        // 0=unknown, 1=revealed, 2=flagged
        private int[,] cellState;
        private int revealedSafeCount;

        // We track which cells are newly revealed zeroes to do expansions
        private Queue<(int, int)> zeroQueue;

        public SolverState(TathamPuzzle puzzle)
        {
            this.puzzle = puzzle;
            cellState = new int[puzzle.width, puzzle.height];
            zeroQueue = new Queue<(int, int)>();
        }

        public bool IsUnknown(int x, int y) => (cellState[x, y] == 0);
        public bool IsRevealed(int x, int y) => (cellState[x, y] == 1);
        public bool IsFlagged(int x, int y) => (cellState[x, y] == 2);

        public void RevealCell(int x, int y)
        {
            if (!puzzle.InRange(x, y)) return;
            if (cellState[x, y] != 0) return;
            cellState[x, y] = 1;
            if (!puzzle.mines[x, y])
            {
                revealedSafeCount++;
                if (puzzle.clues[x, y] == 0)
                {
                    zeroQueue.Enqueue((x, y));
                }
            }
        }

        public void FlagCell(int x, int y)
        {
            if (cellState[x, y] == 0)
            {
                cellState[x, y] = 2;
            }
        }

        public void ExpandZeros(int sx, int sy)
        {
            if (puzzle.clues[sx, sy] != 0) return;
            Queue<(int, int)> q = new Queue<(int, int)>();
            q.Enqueue((sx, sy));
            while (q.Count > 0)
            {
                var (cx, cy) = q.Dequeue();
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (puzzle.InRange(nx, ny) && IsUnknown(nx, ny))
                        {
                            RevealCell(nx, ny);
                            if (puzzle.clues[nx, ny] == 0)
                            {
                                q.Enqueue((nx, ny));
                            }
                        }
                    }
                }
            }
        }

        // Expand any newly revealed zero in zeroQueue
        public bool ExpandAllCurrentZeros()
        {
            bool changed = false;
            while (zeroQueue.Count > 0)
            {
                var (zx, zy) = zeroQueue.Dequeue();
                // BFS expansion
                Queue<(int, int)> q = new Queue<(int, int)>();
                q.Enqueue((zx, zy));
                while (q.Count > 0)
                {
                    var (cx, cy) = q.Dequeue();
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = cx + dx;
                            int ny = cy + dy;
                            if (puzzle.InRange(nx, ny) && IsUnknown(nx, ny))
                            {
                                changed = true;
                                RevealCell(nx, ny);
                                if (puzzle.clues[nx, ny] == 0)
                                {
                                    q.Enqueue((nx, ny));
                                }
                            }
                        }
                    }
                }
            }
            return changed;
        }

        // Basic adjacency logic
        public bool ApplyLocalDeductions()
        {
            bool changed = false;
            for (int x = 0; x < puzzle.width; x++)
            {
                for (int y = 0; y < puzzle.height; y++)
                {
                    if (!IsRevealed(x, y)) continue;
                    int clue = puzzle.clues[x, y];
                    if (clue < 0) continue; // mine

                    int flaggedCount = 0;
                    int unknownCount = 0;
                    List<(int, int)> unknownList = new List<(int, int)>();

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx;
                            int ny = y + dy;
                            if (puzzle.InRange(nx, ny))
                            {
                                if (IsFlagged(nx, ny)) flaggedCount++;
                                else if (IsUnknown(nx, ny))
                                {
                                    unknownCount++;
                                    unknownList.Add((nx, ny));
                                }
                            }
                        }
                    }

                    if (flaggedCount == clue && unknownCount > 0)
                    {
                        // Reveal all unknown as safe
                        foreach (var (ux, uy) in unknownList)
                        {
                            RevealCell(ux, uy);
                        }
                        changed = true;
                    }
                    else if (flaggedCount + unknownCount == clue && unknownCount > 0)
                    {
                        // Flag all unknown
                        foreach (var (ux, uy) in unknownList)
                        {
                            FlagCell(ux, uy);
                        }
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                // Some cells might have become zeros
                changed |= ExpandAllCurrentZeros();
            }
            return changed;
        }

        // Collects all adjacency constraints at once, backtracks over all unknown squares in a single pass
        public bool ApplyGlobalConstraints()
        {
            // Gather constraints
            List<Constraint> constraints = new List<Constraint>();
            HashSet<(int, int)> unknownSet = new HashSet<(int, int)>();

            for (int x = 0; x < puzzle.width; x++)
            {
                for (int y = 0; y < puzzle.height; y++)
                {
                    if (!IsRevealed(x, y)) continue;
                    int clue = puzzle.clues[x, y];
                    if (clue < 0) continue;

                    int flaggedCount = 0;
                    List<(int, int)> relevantUnknowns = new List<(int, int)>();
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx;
                            int ny = y + dy;
                            if (puzzle.InRange(nx, ny))
                            {
                                if (IsFlagged(nx, ny)) flaggedCount++;
                                else if (IsUnknown(nx, ny))
                                {
                                    relevantUnknowns.Add((nx, ny));
                                    unknownSet.Add((nx, ny));
                                }
                            }
                        }
                    }

                    int needed = clue - flaggedCount;
                    if (relevantUnknowns.Count > 0 && needed >= 0)
                    {
                        constraints.Add(new Constraint(relevantUnknowns, needed));
                    }
                }
            }

            if (unknownSet.Count == 0 || constraints.Count == 0)
            {
                return false;
            }

            // Collect unknown cells in a single array
            var unknownArr = new List<(int, int)>(unknownSet).ToArray();
            bool[] assignment = new bool[unknownArr.Length];
            List<bool[]> validSolutions = new List<bool[]>();

            // Full puzzle backtracking with partial checks
            BacktrackAll(0, unknownArr, assignment, constraints, validSolutions);

            if (validSolutions.Count == 0)
            {
                // Contradiction => puzzle not solvable. Tatham typically discards this puzzle in generation
                return false;
            }

            bool changed = false;

            // For each unknown cell, if it's always mine in valid solutions => flag
            // if it's always safe => reveal
            for (int i = 0; i < unknownArr.Length; i++)
            {
                bool allMine = true;
                bool allSafe = true;
                foreach (var sol in validSolutions)
                {
                    if (!sol[i]) allMine = false;
                    if (sol[i]) allSafe = false;
                }

                var (ux, uy) = unknownArr[i];
                if (allMine && !IsFlagged(ux, uy))
                {
                    FlagCell(ux, uy);
                    changed = true;
                }
                else if (allSafe && IsUnknown(ux, uy))
                {
                    RevealCell(ux, uy);
                    changed = true;
                }
            }

            if (changed)
            {
                changed |= ExpandAllCurrentZeros();
            }
            return changed;
        }

        private void BacktrackAll(int index,
                                  (int, int)[] unknownArr,
                                  bool[] assignment,
                                  List<Constraint> constraints,
                                  List<bool[]> validSolutions)
        {
            if (index == unknownArr.Length)
            {
                if (CheckAllConstraints(assignment, unknownArr, constraints))
                {
                    bool[] sol = new bool[assignment.Length];
                    assignment.CopyTo(sol, 0);
                    validSolutions.Add(sol);
                }
                return;
            }

            // Try mine = true
            assignment[index] = true;
            if (CheckPartial(assignment, index, unknownArr, constraints))
            {
                BacktrackAll(index + 1, unknownArr, assignment, constraints, validSolutions);
            }

            // Try safe = false
            assignment[index] = false;
            if (CheckPartial(assignment, index, unknownArr, constraints))
            {
                BacktrackAll(index + 1, unknownArr, assignment, constraints, validSolutions);
            }
        }

        private bool CheckPartial(bool[] assignment,
                                  int assignedIndex,
                                  (int, int)[] unknownArr,
                                  List<Constraint> constraints)
        {
            for (int cIndex = 0; cIndex < constraints.Count; cIndex++)
            {
                var c = constraints[cIndex];
                int mineSoFar = 0;
                int unassigned = 0;
                for (int i = 0; i < c.cells.Count; i++)
                {
                    var cellPos = c.cells[i];
                    int idx = IndexOf(unknownArr, cellPos);
                    if (idx < 0) continue; // might be flagged or revealed
                    if (idx <= assignedIndex)
                    {
                        if (assignment[idx]) mineSoFar++;
                    }
                    else
                    {
                        unassigned++;
                    }
                }
                if (mineSoFar > c.minesNeeded) return false;
                if (mineSoFar + unassigned < c.minesNeeded) return false;
            }
            return true;
        }

        private bool CheckAllConstraints(bool[] assignment,
                                         (int, int)[] unknownArr,
                                         List<Constraint> constraints)
        {
            for (int cIndex = 0; cIndex < constraints.Count; cIndex++)
            {
                var c = constraints[cIndex];
                int mineCount = 0;
                for (int i = 0; i < c.cells.Count; i++)
                {
                    var cellPos = c.cells[i];
                    int idx = IndexOf(unknownArr, cellPos);
                    if (idx >= 0 && assignment[idx])
                    {
                        mineCount++;
                    }
                }
                if (mineCount != c.minesNeeded) return false;
            }
            return true;
        }

        private int IndexOf((int, int)[] arr, (int, int) val)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].Item1 == val.Item1 && arr[i].Item2 == val.Item2)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool AllSafeSquaresRevealed()
        {
            int totalSafe = puzzle.width * puzzle.height - puzzle.mineCount;
            return (revealedSafeCount == totalSafe);
        }

        private class Constraint
        {
            public List<(int, int)> cells;
            public int minesNeeded;

            public Constraint(List<(int, int)> c, int m)
            {
                cells = c;
                minesNeeded = m;
            }
        }
    }
}
