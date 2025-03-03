using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MineTile : MonoBehaviour
{
    [SerializeField] private Sprite[] numberSprites; // 0-8 number sprites
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite mineSprite;
    [SerializeField] private Sprite flagSprite;
    [SerializeField] private Sprite incorrectFlagSprite; 


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

        if (isHolding)
        {
            isHolding = false;

            float dragDistance = Vector2.Distance(touchStartPosition, Input.mousePosition);
            if (holdTime < HOLD_THRESHOLD && dragDistance < DRAG_THRESHOLD)
            {
                // If in Flag Mode, place/remove a flag
                if (gameManager.IsFlagMode())
                {
                    ToggleFlag();
                }
                else
                {
                    // If clicking a flagged tile in Reveal Mode, remove the flag instead of revealing
                    if (isFlagged)
                    {
                        ToggleFlag();
                    }
                    else if (!gameManager.IsPanning())
                    {
                        gameManager.OnTileClicked(x, y);
                    }
                }
            }
        }

        holdTime = 0f;
    }



    void OnMouseExit()
    {
        isHolding = false;
        holdTime = 0f;
    }

    public void ToggleFlag()
    {
        if (isRevealed || gameManager.IsGameOver()) return;

        // Prevent flagging before the first click
        if (gameManager.IsFirstClick())
        {
            this.transform.DOShakePosition(0.8f, 0.3f, 10, 90, false, true);
            Debug.Log("Cannot flag before first click!");
            return;
        }

        // If trying to place a flag but all flags are used up, do nothing
        if (!isFlagged && gameManager.GetRemainingFlags() <= 0)
        {
            this.transform.DOShakePosition(0.8f, 0.3f, 10, 90, false, true);
            Debug.Log("No flags remaining!");
            return;
        }

        isFlagged = !isFlagged;
        spriteRenderer.sprite = isFlagged ? flagSprite : defaultSprite;
        gameManager.OnTileFlagged(isFlagged);

        // Debug log whether the flag is correct
        if (isFlagged)
        {
            Debug.Log($"Flag placed at ({x}, {y}) - Correct: {isMine}");
        }
    }

    public void RevealWithoutAnimation()
    {
        if (isRevealed || isFlagged) return;

        isRevealed = true;

        if (isMine)
        {
            spriteRenderer.sprite = mineSprite;
            if (!gameManager.IsGameOver())
            {
                gameManager.OnMineRevealed();
            }
        }
        else
        {
            // Use the correct number sprite based on adjacent mines
            if (adjacentMines >= 0 && adjacentMines < numberSprites.Length)
            {
                spriteRenderer.sprite = numberSprites[adjacentMines];
            }
        }
    }

    // Modify the existing Reveal method to use animations
    public void Reveal()
    {
        if (isRevealed || isFlagged) return;

        // Use animation manager to handle the reveal animation
        TileAnimationManager.Instance.AnimateTileReveal(
            transform,
            () => RevealWithoutAnimation(),
            null
        );
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

    public void SetIncorrectFlag()
    {
        spriteRenderer.sprite = incorrectFlagSprite; // Change flag to red "X"
    }

}