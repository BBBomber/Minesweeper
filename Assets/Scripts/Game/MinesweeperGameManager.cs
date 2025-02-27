using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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


    // New camera boundary parameters
    [SerializeField] private float bounceSpeed = 5f; // Speed of elastic bounce-back
    [SerializeField] private float maxOvershoot = 2f; // Maximum allowed overshoot beyond bounds

    // Camera boundary variables
    private Vector2 minBoundary;
    private Vector2 maxBoundary;
    private bool isBouncing = false;
    private Vector3 targetPosition;

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

        // Handle elastic bounce-back if needed
        if (isBouncing)
        {
            gameCamera.transform.position = Vector3.Lerp(
                gameCamera.transform.position,
                targetPosition,
                Time.deltaTime * bounceSpeed
            );

            // Stop bouncing when close enough to target
            if (Vector3.Distance(gameCamera.transform.position, targetPosition) < 0.01f)
            {
                isBouncing = false;
            }
        }
        else
        {
            // Check if camera is outside bounds and needs correction
            EnforceCameraBounds();
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

            // Recalculate bounds after zoom change
            CalculateCameraBounds();
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
                Vector3 newPosition = gameCamera.transform.position + direction * mousePanSpeed * Time.deltaTime;

                // Allow limited overshoot beyond bounds
                newPosition = LimitWithinBoundsWithOvershoot(newPosition);
                gameCamera.transform.position = newPosition;

                mousePanStart = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButtonUp(2)) // Middle mouse released
        {
            isMousePanning = false;
            // When released, start the elastic bounce-back if out of bounds
            EnforceCameraBounds();
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
                Vector3 newPosition = gameCamera.transform.position + direction * mousePanSpeed * Time.deltaTime;

                // Allow limited overshoot beyond bounds
                newPosition = LimitWithinBoundsWithOvershoot(newPosition);
                gameCamera.transform.position = newPosition;

                mousePanStart = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButtonUp(1)) // Right mouse released
        {
            isMousePanning = false;
            // When released, start the elastic bounce-back if out of bounds
            EnforceCameraBounds();
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
                float newZoom = Mathf.Clamp(startZoom * zoomDelta, minZoom, maxZoom);
                gameCamera.orthographicSize = newZoom;

                // Recalculate bounds after zoom change
                CalculateCameraBounds();
            }
            else if (touchZero.phase == TouchPhase.Ended || touchOne.phase == TouchPhase.Ended)
            {
                // When zooming ends, check if we need to bounce back
                EnforceCameraBounds();
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
                    Vector3 newPosition = gameCamera.transform.position + direction * panSpeed * Time.deltaTime;

                    // Allow limited overshoot beyond bounds
                    newPosition = LimitWithinBoundsWithOvershoot(newPosition);
                    gameCamera.transform.position = newPosition;

                    // Update start position for smoother movement
                    touchStart = gameCamera.ScreenToWorldPoint(touch.position);
                }
            }
            // End panning when touch ends
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isPanning = false;
                // When released, start the elastic bounce-back if out of bounds
                EnforceCameraBounds();
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
        CalculateCameraBounds();
        currentState = GameState.Playing;
        firstClick = true;
    }
    private void CalculateCameraBounds()
    {
        // Calculate board dimensions
        float boardWidth = width * (tileSize + spacing) - spacing;
        float boardHeight = height * (tileSize + spacing) - spacing;

        // Set boundaries to exactly match the board edges
        float horizontalBound = boardWidth / 2;
        float verticalBound = boardHeight / 2;

        minBoundary = new Vector2(-horizontalBound, -verticalBound);
        maxBoundary = new Vector2(horizontalBound, verticalBound);
    }

    private Vector3 LimitWithinBoundsWithOvershoot(Vector3 position)
    {
        // Allow a limited overshoot beyond the boundaries
        float x = Mathf.Clamp(position.x, minBoundary.x - maxOvershoot, maxBoundary.x + maxOvershoot);
        float y = Mathf.Clamp(position.y, minBoundary.y - maxOvershoot, maxBoundary.y + maxOvershoot);

        return new Vector3(x, y, position.z);
    }

    private void EnforceCameraBounds()
    {
        Vector3 currentPosition = gameCamera.transform.position;
        bool outOfBounds = false;

        // Check if the camera is outside the allowed boundaries
        if (currentPosition.x < minBoundary.x || currentPosition.x > maxBoundary.x ||
            currentPosition.y < minBoundary.y || currentPosition.y > maxBoundary.y)
        {
            // Calculate the target position within bounds
            float targetX = Mathf.Clamp(currentPosition.x, minBoundary.x, maxBoundary.x);
            float targetY = Mathf.Clamp(currentPosition.y, minBoundary.y, maxBoundary.y);

            targetPosition = new Vector3(targetX, targetY, currentPosition.z);
            isBouncing = true;
            outOfBounds = true;
        }

        if (!outOfBounds)
        {
            isBouncing = false;
        }
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
            CalculateCameraBounds();
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
        // Don’t do anything if game is over or panning
        if (currentState != GameState.Playing || IsPanning()) return;

        // If first click, place mines, etc.
        if (firstClick)
        {
            GenerateMines(x, y);
            CalculateAdjacentMines();
            firstClick = false;
        }

        // Instead of tile.Reveal(), just do one BFS call:
        if (!grid[x, y].IsFlagged() && !grid[x, y].IsRevealed())
        {
            FloodFill(x, y);  // The BFS will reveal tile (x,y) and all connected zeros
        }

        CheckWinCondition();
    }

    private void GenerateMines(int safeX, int safeY)
    {
        minePositions.Clear();
        bool validPuzzle = false;
        int attempts = 0;
        const int MAX_ATTEMPTS = 50; // Limit regeneration attempts

        while (!validPuzzle && attempts < MAX_ATTEMPTS)
        {
            attempts++;

            // 1) Determine safe zone around first click
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

            // 2) Clear any existing mines
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y].SetMine(false);
                }
            }

            // 3) Build a list of all possible mine positions except the safe zone
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

            // 4) Shuffle the list
            for (int i = 0; i < possiblePositions.Count; i++)
            {
                int randomIndex = Random.Range(i, possiblePositions.Count);
                Vector2Int temp = possiblePositions[i];
                possiblePositions[i] = possiblePositions[randomIndex];
                possiblePositions[randomIndex] = temp;
            }

            // 5) Place mines
            minePositions.Clear();
            int minesToPlace = Mathf.Min(mineCount, possiblePositions.Count);
            for (int i = 0; i < minesToPlace; i++)
            {
                Vector2Int pos = possiblePositions[i];
                grid[pos.x, pos.y].SetMine(true);
                minePositions.Add(pos);
            }

            // 6) Calculate adjacent mines for all tiles
            CalculateAdjacentMines();

            // 7) Check if the puzzle is solvable
            validPuzzle = IsPuzzleSolvable(safeX, safeY);

            if (validPuzzle)
            {
                Debug.Log("Generated a solvable puzzle in " + attempts + " attempts");
            }
        }

        if (!validPuzzle)
        {
            Debug.LogWarning("Failed to generate a perfectly solvable puzzle after " + MAX_ATTEMPTS + " attempts. Using last generation.");
        }
    }

    private bool IsPuzzleSolvable(int startX, int startY)
    {
        // Create a simulation grid to track the solver's knowledge
        bool[,] revealed = new bool[width, height];
        bool[,] flagged = new bool[width, height];
        bool[,] knownSafe = new bool[width, height];

        // Start by revealing the first cell
        List<Vector2Int> toReveal = new List<Vector2Int>();
        toReveal.Add(new Vector2Int(startX, startY));

        while (toReveal.Count > 0)
        {
            Vector2Int pos = toReveal[0];
            toReveal.RemoveAt(0);

            if (revealed[pos.x, pos.y] || flagged[pos.x, pos.y])
                continue;

            // Reveal this cell
            revealed[pos.x, pos.y] = true;

            // If it's a mine, that's not allowed in our simulation
            if (grid[pos.x, pos.y].IsMine())
                return false;

            // If it's a 0, add all neighbors to reveal queue
            if (grid[pos.x, pos.y].GetAdjacentMines() == 0)
            {
                for (int xOffset = -1; xOffset <= 1; xOffset++)
                {
                    for (int yOffset = -1; yOffset <= 1; yOffset++)
                    {
                        int newX = pos.x + xOffset;
                        int newY = pos.y + yOffset;

                        if (IsValidCoordinate(newX, newY) && !revealed[newX, newY] && !flagged[newX, newY])
                        {
                            toReveal.Add(new Vector2Int(newX, newY));
                            knownSafe[newX, newY] = true;
                        }
                    }
                }
            }

            // After each reveal, find certain moves and continue
            while (true)
            {
                bool progress = false;

                // For each revealed cell, check if we can determine mines around it
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (!revealed[x, y]) continue;

                        int adjacentCount = grid[x, y].GetAdjacentMines();
                        if (adjacentCount == 0) continue;

                        // Count adjacent flags and unrevealed cells
                        int adjacentFlags = 0;
                        List<Vector2Int> unrevealed = new List<Vector2Int>();

                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            for (int yOffset = -1; yOffset <= 1; yOffset++)
                            {
                                if (xOffset == 0 && yOffset == 0) continue;

                                int newX = x + xOffset;
                                int newY = y + yOffset;

                                if (IsValidCoordinate(newX, newY))
                                {
                                    if (flagged[newX, newY])
                                        adjacentFlags++;
                                    else if (!revealed[newX, newY])
                                        unrevealed.Add(new Vector2Int(newX, newY));
                                }
                            }
                        }

                        // If number of flags equals adjacent mines and there are still unrevealed cells,
                        // we can safely reveal all unrevealed
                        if (adjacentFlags == adjacentCount && unrevealed.Count > 0)
                        {
                            foreach (Vector2Int cell in unrevealed)
                            {
                                if (!knownSafe[cell.x, cell.y])
                                {
                                    toReveal.Add(cell);
                                    knownSafe[cell.x, cell.y] = true;
                                    progress = true;
                                }
                            }
                        }

                        // If number of unrevealed equals remaining mines (adjacent mines - flags)
                        // we can flag all unrevealed
                        if (unrevealed.Count == adjacentCount - adjacentFlags)
                        {
                            foreach (Vector2Int cell in unrevealed)
                            {
                                if (!flagged[cell.x, cell.y])
                                {
                                    flagged[cell.x, cell.y] = true;
                                    progress = true;
                                }
                            }
                        }
                    }
                }

                // If no progress was made in this iteration, stop
                if (!progress) break;
            }

            // Check if all non-mine cells can be revealed
            int totalRevealed = 0;
            int totalFlagged = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (revealed[x, y]) totalRevealed++;
                    if (flagged[x, y]) totalFlagged++;
                }
            }

            // If we've revealed all non-mine cells or if there are unrevealed cells that 
            // aren't in the reveal queue and not flagged, we're blocked
            if (totalRevealed == (width * height - mineCount))
            {
                return true; // Puzzle is solved!
            }

            // If no more cells to reveal, but puzzle isn't solved, we must guess
            if (toReveal.Count == 0)
            {
                // Check if any safe moves are available but not in the queue
                bool safeMoveAvailable = false;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (knownSafe[x, y] && !revealed[x, y] && !flagged[x, y])
                        {
                            toReveal.Add(new Vector2Int(x, y));
                            safeMoveAvailable = true;
                        }
                    }
                }

                if (!safeMoveAvailable)
                    return false; // We would have to guess, so the puzzle is not deterministic
            }
        }

        return true;
    }

    // Add this method to your MineTile class to fix flood fill behavior
    /*public void ForceReveal()
    {
        // This method is similar to Reveal() but skips the flag check
        if (isRevealed) return;

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

            // If no adjacent mines, flood fill
            if (adjacentMines == 0)
            {
                gameManager.FloodFill(x, y);
            }
        }
    }*/


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
    public void FloodFill(int startX, int startY)
    {
        // Typical BFS or DFS approach
        Queue<Vector2Int> tilesToCheck = new Queue<Vector2Int>();
        tilesToCheck.Enqueue(new Vector2Int(startX, startY));

        while (tilesToCheck.Count > 0)
        {
            Vector2Int pos = tilesToCheck.Dequeue();
            int x = pos.x;
            int y = pos.y;

            // 1) Make sure in bounds
            if (!IsValidCoordinate(x, y))
                continue;

            MineTile tile = grid[x, y];

            // 2) Skip if flagged or already revealed
            if (tile.IsRevealed() || tile.IsFlagged())
                continue;

            // 3) Reveal this tile
            tile.Reveal();  // This sets isRevealed = true and updates the sprite

            // 4) If it's not a mine AND has 0 neighbors, add neighbors to queue
            if (!tile.IsMine() && tile.GetAdjacentMines() == 0)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // skip itself

                        int nx = x + dx;
                        int ny = y + dy;

                        if (IsValidCoordinate(nx, ny))
                        {
                            tilesToCheck.Enqueue(new Vector2Int(nx, ny));
                        }
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