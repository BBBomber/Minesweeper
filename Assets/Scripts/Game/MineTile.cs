using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MineTile : MonoBehaviour
{
    [SerializeField] private Sprite[] numberSprites; // 0-8 number sprites
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite mineSprite;
    [SerializeField] private Sprite flagSprite;

    private SpriteRenderer spriteRenderer;
    private bool isRevealed = false;
    private bool isFlagged = false;
    private bool isMine = false;
    private int adjacentMines = 0;
    private int x, y; // Grid coordinates
    private float holdTime = 0f;
    private bool isHolding = false;
    private const float HOLD_THRESHOLD = 0.5f; // Hold time to flag
    private Vector2 touchStartPosition;
    private const float DRAG_THRESHOLD = 10f; // Distance in pixels to consider a drag vs a tap

    // Reference to the game manager
    private MinesweeperGameManager gameManager;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        spriteRenderer.sprite = defaultSprite;

        // Add BoxCollider2D for touch input if not already present
        if (GetComponent<BoxCollider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }
    }

    public void Setup(int x, int y, MinesweeperGameManager manager)
    {
        this.x = x;
        this.y = y;
        this.gameManager = manager;
        Reset();
    }

    public void Reset()
    {
        isRevealed = false;
        isFlagged = false;
        isMine = false;
        adjacentMines = 0;
        spriteRenderer.sprite = defaultSprite;
    }

    void Update()
    {
        // Handle hold for flagging
        if (isHolding)
        {
            holdTime += Time.deltaTime;
            if (holdTime >= HOLD_THRESHOLD)
            {
                ToggleFlag();
                isHolding = false;
                holdTime = 0f;
            }
        }
    }

    void OnMouseDown()
    {
        if (isRevealed || gameManager.IsGameOver()) return;

        // Ignore if right or middle mouse button is used (for panning)
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            return;
        }

        // Store the starting touch position for drag detection
        touchStartPosition = Input.mousePosition;
        isHolding = true;
        holdTime = 0f;
    }

    void OnMouseUp()
    {
        if (isRevealed || gameManager.IsGameOver()) return;

        // Ignore if right or middle mouse button was used
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            return;
        }

        if (isHolding)
        {
            isHolding = false;

            // Check if this was a tap or a drag
            float dragDistance = Vector2.Distance(touchStartPosition, Input.mousePosition);

            // If held for less than threshold and not dragged far, it's a normal click (reveal)
            if (holdTime < HOLD_THRESHOLD && dragDistance < DRAG_THRESHOLD)
            {
                if (!isFlagged && !gameManager.IsPanning())
                {
                    gameManager.OnTileClicked(x, y);
                }
            }

            holdTime = 0f;
        }
    }

    void OnMouseExit()
    {
        isHolding = false;
        holdTime = 0f;
    }

    public void ToggleFlag()
    {
        if (isRevealed || gameManager.IsGameOver()) return;

        isFlagged = !isFlagged;
        spriteRenderer.sprite = isFlagged ? flagSprite : defaultSprite;
        gameManager.OnTileFlagged(isFlagged);
    }

    public void Reveal()
    {
        if (isRevealed || isFlagged) return;

        isRevealed = true;

        if (isMine)
        {
            spriteRenderer.sprite = mineSprite;
            gameManager.OnMineRevealed();
        }
        else
        {
            // Use the correct number sprite based on adjacent mines
            if (adjacentMines >= 0 && adjacentMines < numberSprites.Length)
            {
                spriteRenderer.sprite = numberSprites[adjacentMines];
            }

            // If no adjacent mines, flood fill
            if (adjacentMines == 0)
            {
                gameManager.FloodFill(x, y);
            }
        }
    }

    public void SetMine(bool value)
    {
        isMine = value;
    }

    public bool IsMine()
    {
        return isMine;
    }

    public bool IsRevealed()
    {
        return isRevealed;
    }

    public bool IsFlagged()
    {
        return isFlagged;
    }

    public void SetAdjacentMines(int count)
    {
        adjacentMines = count;
    }

    public int GetAdjacentMines()
    {
        return adjacentMines;
    }

    public Vector2Int GetCoordinates()
    {
        return new Vector2Int(x, y);
    }
}