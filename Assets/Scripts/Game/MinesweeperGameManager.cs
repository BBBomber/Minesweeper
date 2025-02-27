using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinesweeperGameManager : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform boardContainer;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float spacing = 0.1f;

    // Camera control parameters
    [SerializeField] private Camera gameCamera;
    [SerializeField] private float[] difficultyMinZoom = { 3f, 3f, 2.5f, 2f };  // Min zoom per difficulty
    [SerializeField] private float[] difficultyMaxZoom = { 10f, 15f, 18f, 20f }; // Max zoom per difficulty
    [SerializeField] private float[] difficultyStartZoom = { 5f, 7f, 9f, 11f };  // Starting zoom per difficulty
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float mouseZoomSpeed = 0.5f;
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private float mousePanSpeed = 10f;

    // Private camera control variables
    private float minZoom;
    private float maxZoom;

    private enum GameState { Setup, Playing, Win, GameOver };
    private GameState currentState = GameState.Setup;

    private MineTile[,] grid;
    private int width, height;
    private int mineCount;
    private int flaggedCount = 0;
    private int revealedCount = 0;
    private bool firstClick = true;

    // Touch control variables
    private Vector3 touchStart;
    private Vector2 touchZoomStart;
    private float startZoom;
    private bool isPanning = false;
    private bool isZooming = false;

    // Mouse control variables
    private Vector3 mousePanStart;
    private bool isMousePanning = false;

    // Difficulty configurations
    private readonly int[] difficultyWidths = { 8, 10, 12, 16 };
    private readonly int[] difficultyHeights = { 10, 12, 16, 20 };
    private readonly float[] difficultyMineDensities = { 0.12f, 0.15f, 0.18f, 0.22f };

    // Reference to the GameManager singleton
    private GameManager gameManager;

    //mine positions
    private List<Vector2Int> minePositions = new List<Vector2Int>();

    void Start()
    {
        // Get reference to main camera if not assigned
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }

        // Get the difficulty from the GameManager singleton
        gameManager = GameManager.Instance;
        int difficulty = gameManager != null ? gameManager.currentDifficulty : 0;
        difficulty = Mathf.Clamp(difficulty, 0, 3);

        // Setup the game based on difficulty
        SetupGame(difficulty);
    }

    void Update()
    {
        // Check if we should use touch or mouse controls
        if (Input.touchSupported && Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMouseInput();
        }
    }

    private void HandleMouseInput()
    {
        // Skip mouse handling if game is over
        if (currentState == GameState.GameOver || currentState == GameState.Win) return;

        // Mouse wheel zoom
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0)
        {
            float newZoom = gameCamera.orthographicSize - scrollDelta * mouseZoomSpeed * 10;
            gameCamera.orthographicSize = Mathf.Clamp(newZoom, minZoom, maxZoom);
        }

        // Middle mouse button for panning
        if (Input.GetMouseButtonDown(2)) // Middle mouse button
        {
            mousePanStart = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            isMousePanning = true;
        }
        else if (Input.GetMouseButton(2)) // Middle mouse held down
        {
            if (isMousePanning)
            {
                Vector3 direction = mousePanStart - gameCamera.ScreenToWorldPoint(Input.mousePosition);
                gameCamera.transform.position += direction * mousePanSpeed * Time.deltaTime;
                mousePanStart = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButtonUp(2)) // Middle mouse released
        {
            isMousePanning = false;
        }

        // Alternative panning with right mouse button for testing
        if (Input.GetMouseButtonDown(1)) // Right mouse button
        {
            mousePanStart = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            isMousePanning = true;
        }
        else if (Input.GetMouseButton(1)) // Right mouse held down
        {
            if (isMousePanning)
            {
                Vector3 direction = mousePanStart - gameCamera.ScreenToWorldPoint(Input.mousePosition);
                gameCamera.transform.position += direction * mousePanSpeed * Time.deltaTime;
                mousePanStart = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButtonUp(1)) // Right mouse released
        {
            isMousePanning = false;
        }
    }

    private void HandleTouchInput()
    {
        // Skip touch handling if game is over
        if (currentState == GameState.GameOver || currentState == GameState.Win) return;

        // Handle multi-touch for zooming
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Start of multi-touch: store initial positions and zoom
            if (touchZero.phase == TouchPhase.Began || touchOne.phase == TouchPhase.Began)
            {
                touchZoomStart = touchZero.position - touchOne.position;
                startZoom = gameCamera.orthographicSize;
                isZooming = true;
                isPanning = false;
            }
            // Handle zoom during touch
            else if (touchZero.phase == TouchPhase.Moved || touchOne.phase == TouchPhase.Moved)
            {
                Vector2 touchZoomCurrent = touchZero.position - touchOne.position;
                float zoomDelta = touchZoomStart.magnitude / touchZoomCurrent.magnitude;

                // Apply zoom
                gameCamera.orthographicSize = Mathf.Clamp(startZoom * zoomDelta, minZoom, maxZoom);
            }
        }
        // Handle single touch for panning
        else if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // End any ongoing zoom
            if (isZooming && touch.phase == TouchPhase.Began)
            {
                isZooming = false;
            }

            // Start panning on touch begin
            if (touch.phase == TouchPhase.Began)
            {
                touchStart = gameCamera.ScreenToWorldPoint(touch.position);
                isPanning = true;
            }
            // Pan camera when touch moves
            else if (touch.phase == TouchPhase.Moved && isPanning)
            {
                // Only handle as pan if the touch has moved a significant distance
                if (touch.deltaPosition.magnitude > 5)
                {
                    Vector3 direction = touchStart - gameCamera.ScreenToWorldPoint(touch.position);
                    gameCamera.transform.position += direction * panSpeed * Time.deltaTime;

                    // Update start position for smoother movement
                    touchStart = gameCamera.ScreenToWorldPoint(touch.position);
                }
            }
            // End panning when touch ends
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isPanning = false;
            }
        }
        else
        {
            // Reset control flags when no touches
            isPanning = false;
            isZooming = false;
        }
    }

    private void SetupGame(int difficulty)
    {
        // Set dimensions based on difficulty
        width = difficultyWidths[difficulty];
        height = difficultyHeights[difficulty];

        // Calculate mine count based on density
        mineCount = Mathf.FloorToInt(width * height * difficultyMineDensities[difficulty]);

        // Set zoom parameters based on difficulty
        minZoom = difficultyMinZoom[difficulty];
        maxZoom = difficultyMaxZoom[difficulty];

        // Create the board
        CreateBoard();

        // Reset camera position and zoom to fit board with difficulty-specific settings
        ResetCameraView(difficulty);

        currentState = GameState.Playing;
        firstClick = true;
    }

    private void ResetCameraView(int difficulty)
    {
        if (gameCamera != null)
        {
            // Calculate board dimensions
            float boardWidth = width * (tileSize + spacing) - spacing;
            float boardHeight = height * (tileSize + spacing) - spacing;

            // Set camera position to board center
            gameCamera.transform.position = new Vector3(0, 0, gameCamera.transform.position.z);

            // Use the predefined starting zoom value for this difficulty
            float desiredZoom = difficultyStartZoom[difficulty];

            // If auto-calculate zoom is preferred, uncomment this code instead:
            // float calculatedZoom = Mathf.Max(boardWidth, boardHeight) * 0.55f;
            // desiredZoom = calculatedZoom;

            // Apply zoom settings
            gameCamera.orthographicSize = Mathf.Clamp(desiredZoom, minZoom, maxZoom);
        }
    }

    private void CreateBoard()
    {
        // Clear any existing board
        if (boardContainer != null)
        {
            foreach (Transform child in boardContainer)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            boardContainer = new GameObject("BoardContainer").transform;
            boardContainer.SetParent(transform);
            boardContainer.localPosition = Vector3.zero;
        }

        // Initialize the grid
        grid = new MineTile[width, height];

        // Calculate board dimensions
        float boardWidth = width * (tileSize + spacing) - spacing;
        float boardHeight = height * (tileSize + spacing) - spacing;

        // Center the board at (0,0) and offset to the right by half a tile
        Vector3 boardOffset = new Vector3(-boardWidth / 2 + (tileSize / 2), -boardHeight / 2, 0);
        boardContainer.localPosition = Vector3.zero;

        // Create tiles with offset to center the board
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(
                    x * (tileSize + spacing) + boardOffset.x,
                    y * (tileSize + spacing) + boardOffset.y,
                    0
                );

                GameObject tileObject = Instantiate(tilePrefab, position, Quaternion.identity, boardContainer);
                tileObject.name = $"Tile_{x}_{y}";
                tileObject.transform.localScale = new Vector3(tileSize, tileSize, 1);

                MineTile tile = tileObject.GetComponent<MineTile>();
                if (tile == null)
                {
                    tile = tileObject.AddComponent<MineTile>();
                }

                tile.Setup(x, y, this);
                grid[x, y] = tile;
            }
        }

        // Reset game counters
        flaggedCount = 0;
        revealedCount = 0;
    }

    // Called when a tile is clicked
    public void OnTileClicked(int x, int y)
    {
        if (currentState != GameState.Playing || isPanning || isMousePanning) return;

        // Handle first click
        if (firstClick)
        {
            GenerateMines(x, y);
            CalculateAdjacentMines();
            firstClick = false;
        }

        RevealTile(x, y);
        CheckWinCondition();
    }

    private void GenerateMines(int safeX, int safeY)
    {
        minePositions.Clear();
        // 1) Determine which positions are safe around first click
        List<Vector2Int> safeZone = new List<Vector2Int>();
        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                int newX = safeX + xOffset;
                int newY = safeY + yOffset;
                if (IsValidCoordinate(newX, newY))
                {
                    safeZone.Add(new Vector2Int(newX, newY));
                }
            }
        }

        // 2) Build a list of all possible mine positions except the safe zone
        List<Vector2Int> possiblePositions = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!safeZone.Contains(pos))
                {
                    possiblePositions.Add(pos);
                }
            }
        }

        // 3) Shuffle the list
        for (int i = 0; i < possiblePositions.Count; i++)
        {
            int randomIndex = Random.Range(i, possiblePositions.Count);
            Vector2Int temp = possiblePositions[i];
            possiblePositions[i] = possiblePositions[randomIndex];
            possiblePositions[randomIndex] = temp;
        }

        // 4) Place mines
        int minesToPlace = Mathf.Min(mineCount, possiblePositions.Count);
        for (int i = 0; i < minesToPlace; i++)
        {
            Vector2Int pos = possiblePositions[i];
            grid[pos.x, pos.y].SetMine(true);

            // RECORD the mine position in our list (NEW)
            minePositions.Add(pos);
        }
    }

    private void CalculateAdjacentMines()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!grid[x, y].IsMine())
                {
                    int count = CountAdjacentMines(x, y);
                    grid[x, y].SetAdjacentMines(count);
                }
            }
        }
    }

    private int CountAdjacentMines(int x, int y)
    {
        int count = 0;
        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                if (xOffset == 0 && yOffset == 0) continue;

                int newX = x + xOffset;
                int newY = y + yOffset;
                if (IsValidCoordinate(newX, newY) && grid[newX, newY].IsMine())
                {
                    count++;
                }
            }
        }
        return count;
    }

    private void RevealTile(int x, int y)
    {
        if (!IsValidCoordinate(x, y)) return;

        MineTile tile = grid[x, y];
        if (tile.IsRevealed() || tile.IsFlagged()) return;

        tile.Reveal();

        if (!tile.IsMine())
        {
            revealedCount++;
        }
    }

    // Flood fill algorithm to reveal empty connected tiles
    public void FloodFill(int x, int y)
    {
        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                int newX = x + xOffset;
                int newY = y + yOffset;

                if (IsValidCoordinate(newX, newY))
                {
                    MineTile tile = grid[newX, newY];
                    if (!tile.IsRevealed() && !tile.IsFlagged() && !tile.IsMine())
                    {
                        RevealTile(newX, newY);
                    }
                }
            }
        }
    }

    private bool IsValidCoordinate(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private void CheckWinCondition()
    {
        // Win condition: all non-mine tiles are revealed
        if (revealedCount == (width * height - mineCount))
        {
            currentState = GameState.Win;
            Debug.Log("Game Won!");
            // Implement win UI/logic here
        }
    }

    public void OnMineRevealed()
    {
        currentState = GameState.GameOver;
        RevealAllMines();
        Debug.Log("Game Over!");
        // Implement game over UI/logic here
        //restart option, game difficulty selection panel on overlay, or go back to main menu
        //for now go back
        GameManager.Instance.LoadScene("HomePage");
    }

    private void RevealAllMines() //optimize
    {
        // Just loop through known mine positions; no need to scan everything
        foreach (var minePos in minePositions)
        {
            MineTile tile = grid[minePos.x, minePos.y];
            if (!tile.IsRevealed())
            {
                tile.Reveal();
            }
        }
    }

    public void OnTileFlagged(bool isFlagged)
    {
        flaggedCount += isFlagged ? 1 : -1;
        // Update UI for mine counter if needed
    }

    public bool IsGameOver()
    {
        return currentState == GameState.GameOver || currentState == GameState.Win;
    }

    public bool IsPanning()
    {
        return isPanning || isMousePanning;
    }

    public void RestartGame()
    {
        // Get the current difficulty from the GameManager singleton
        int difficulty = gameManager != null ? gameManager.currentDifficulty : 0;
        difficulty = Mathf.Clamp(difficulty, 0, 3);

        SetupGame(difficulty);
    }
}