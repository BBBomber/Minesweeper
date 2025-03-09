using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SavedGameState
{
    public int difficulty;
    public float elapsedTime;
    public int flaggedCount;
    public int revealedCount;
    public bool firstClick;
    public List<Vector2Int> minePositions;
    public List<TileState> tileStates;
}

[System.Serializable]
public class TileState
{
    public int x;
    public int y;
    public bool isRevealed;
    public bool isFlagged;
    public bool isMine;
    public int adjacentMines;
}