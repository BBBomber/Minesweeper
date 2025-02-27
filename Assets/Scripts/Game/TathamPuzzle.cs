using System.Collections.Generic;

public class TathamPuzzle
{
    public int width;
    public int height;
    public int mineCount;

    // Mine layout: true if there's a mine, false if empty
    public bool[,] mines;

    // Clues: for each cell, how many adjacent mines (-1 if the cell itself is a mine)
    public int[,] clues;

    // Constructor
    public TathamPuzzle(int w, int h, int m)
    {
        width = w;
        height = h;
        mineCount = m;
        mines = new bool[w, h];
        clues = new int[w, h];
    }

    // Checks if coordinates are within puzzle bounds
    public bool InRange(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }
}
