using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UnityEngine.SceneManagement;


public class MinesweeperGameManager : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform boardContainer;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float spacing = 0.1f;

    // Camera control parameters
    [SerializeField] private Camera gameCamera;
    [SerializeField] private float[] difficultyMinZoom = { 3f, 3f, 35f, 3f };  // Min zoom per difficulty
    [SerializeField] private float[] difficultyMaxZoom = { 10f, 15f, 18f, 20f }; // Max zoom per difficulty
    [SerializeField] private float[] difficultyStartZoom = { 5f, 7f, 9f, 11f };  // Starting zoom per difficulty
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float mouseZoomSpeed = 0.5f;
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private float mousePanSpeed = 10f;
    private const float PAN_THRESHOLD = 3f;

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


    //UI Stuff
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI mineCounterText;
 
    private float elapsedTime = 0f; // Tracks time since game started
    private bool isAnimating = false; // Prevents clicking during animations

    [SerializeField] private GameObject togglePanel;
    [SerializeField] private CustomToggle customToggle;
    private bool isFlagMode = false;

    public GameWinManager popup;

    [SerializeField] private GameObject restartPanel;
    [SerializeField] private DifficultySelectorTMP difficultySelector;
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private Button hintButton;

    [SerializeField] private GameObject errorPopup;              // A panel for error messages
    [SerializeField] private TextMeshProUGUI errorPopupText;

    void Start()
    {
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }
        gameManager = GameManager.Instance;
        int difficulty = gameManager != null ? gameManager.currentDifficulty : 0;
        difficulty = Mathf.Clamp(difficulty, 0, 3);
        
        SetupGame(difficulty);

        // Check if the player has seen the instructions before.
        if (PlayerPrefs.GetInt("HasSeenInstructions", 0) == 0)
        {
            // Show the instructions panel
            instructionsPanel.SetActive(true);
        }
        else
        {
            instructionsPanel.SetActive(false);
        }

        if (customToggle != null)
        {
            customToggle.OnToggleChanged += ToggleFlagMode;
        }

        
    }

    public void OnContinueButtonClicked()
    {
        instructionsPanel.SetActive(false);
        PlayerPrefs.SetInt("HasSeenInstructions", 1); // Mark that instructions have been seen
    }
    void Update()
    {
        if (currentState == GameState.Playing && !firstClick)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }


        if (Input.touchSupported && Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMouseInput();
        }

        if (isBouncing)
        {
            gameCamera.transform.position = Vector3.Lerp(
                gameCamera.transform.position,
                targetPosition,
                Time.deltaTime * bounceSpeed
            );
            if (Vector3.Distance(gameCamera.transform.position, targetPosition) < 0.01f)
            {
                isBouncing = false;
            }
        }
        else
        {
            EnforceCameraBounds();
        }
    }

    private void HandleMouseInput()
    {
        if (currentState == GameState.GameOver || currentState == GameState.Win) return;

        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0)
        {
            float newZoom = gameCamera.orthographicSize - scrollDelta * mouseZoomSpeed * 10;
            gameCamera.orthographicSize = Mathf.Clamp(newZoom, minZoom, maxZoom);
            CalculateCameraBounds();
        }

        if (Input.GetMouseButtonDown(2))
        {
            mousePanStart = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            isMousePanning = true;
        }
        else if (Input.GetMouseButton(2))
        {
            if (isMousePanning)
            {
                Vector3 direction = mousePanStart - gameCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector3 newPosition = gameCamera.transform.position + direction * mousePanSpeed * Time.deltaTime;
                newPosition = LimitWithinBoundsWithOvershoot(newPosition);
                gameCamera.transform.position = newPosition;
                mousePanStart = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButtonUp(2))
        {
            isMousePanning = false;
            EnforceCameraBounds();
        }

        if (Input.GetMouseButtonDown(1))
        {
            mousePanStart = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            isMousePanning = true;
        }
        else if (Input.GetMouseButton(1))
        {
            if (isMousePanning)
            {
                Vector3 direction = mousePanStart - gameCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector3 newPosition = gameCamera.transform.position + direction * mousePanSpeed * Time.deltaTime;
                newPosition = LimitWithinBoundsWithOvershoot(newPosition);
                gameCamera.transform.position = newPosition;
                mousePanStart = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isMousePanning = false;
            EnforceCameraBounds();
        }
    }

    public bool IsFirstClick()
    {
        return firstClick;
    }

    private void HandleTouchInput()
    {
        if (currentState == GameState.GameOver || currentState == GameState.Win) return;

        if (Input.touchCount == 2)
        {
            // [Zoom logic remains the same...]
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);
            if (touchZero.phase == TouchPhase.Began || touchOne.phase == TouchPhase.Began)
            {
                touchZoomStart = touchZero.position - touchOne.position;
                startZoom = gameCamera.orthographicSize;
                isZooming = true;
                isPanning = false;  // Ensure panning is off when zooming starts
            }
            else if (touchZero.phase == TouchPhase.Moved || touchOne.phase == TouchPhase.Moved)
            {
                Vector2 touchZoomCurrent = touchZero.position - touchOne.position;
                float zoomDelta = touchZoomStart.magnitude / touchZoomCurrent.magnitude;
                float newZoom = Mathf.Clamp(startZoom * zoomDelta, minZoom, maxZoom);
                gameCamera.orthographicSize = newZoom;
                CalculateCameraBounds();
            }
            else if (touchZero.phase == TouchPhase.Ended || touchOne.phase == TouchPhase.Ended)
            {
                EnforceCameraBounds();
                isZooming = false;
            }
        }
        else if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // If we were zooming and a new touch begins, reset zooming
            if (isZooming && touch.phase == TouchPhase.Began)
            {
                isZooming = false;
            }

            if (touch.phase == TouchPhase.Began)
            {
                // Record the start position for potential panning
                touchStart = gameCamera.ScreenToWorldPoint(touch.position);
                // Do not set isPanning here; wait to see if movement exceeds threshold
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                // Calculate the drag distance from the initial touch position
                float dragDistance = (touch.position - touch.rawPosition).magnitude;
                // If the movement exceeds the threshold, consider it a pan
                if (!isPanning && dragDistance > PAN_THRESHOLD)
                {
                    isPanning = true;
                }
                if (isPanning)
                {
                    // Move the camera based on the touch movement
                    Vector3 currentTouchWorldPos = gameCamera.ScreenToWorldPoint(touch.position);
                    Vector3 direction = touchStart - currentTouchWorldPos;
                    Vector3 newPosition = gameCamera.transform.position + direction * panSpeed * Time.deltaTime;
                    newPosition = LimitWithinBoundsWithOvershoot(newPosition);
                    gameCamera.transform.position = newPosition;

                    // Update the touch start for the next frame
                    touchStart = currentTouchWorldPos;
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                // If the user did not pan (i.e. drag distance below threshold), 
                // the tile's own touch logic will handle the tap.
                isPanning = false;
                EnforceCameraBounds();
            }
        }
        else
        {
            isPanning = false;
            isZooming = false;
        }
    }


    private void AnimateCameraToTile(int x, int y)
    {
        // Get the target tile position (keep the camera's z-position)
        Vector3 tilePos = grid[x, y].transform.position;
        tilePos.z = gameCamera.transform.position.z;

        // Define the target zoom level: half of the max zoom
        float targetZoom = maxZoom / 1.5f;

        // Animate camera position and zoom (duration is 1.0 seconds, adjust as needed)
        gameCamera.transform.DOMove(tilePos, 1.0f).SetEase(Ease.OutQuad);
        gameCamera.DOOrthoSize(targetZoom, 1.0f).SetEase(Ease.OutQuad);
    }
    private void SetupGame(int difficulty)
    {
        width = difficultyWidths[difficulty];
        height = difficultyHeights[difficulty];
        mineCount = Mathf.FloorToInt(width * height * difficultyMineDensities[difficulty]);

        elapsedTime = 0f;
        minZoom = 3f;
       
        hintButton.interactable = false;
        hintButton.gameObject.SetActive(true);

        CreateBoard();
        ResetCameraView(difficulty);
        CalculateCameraBounds();
        UpdateMineCounterUI();

        currentState = GameState.Playing;
        firstClick = true;
        togglePanel.SetActive(true);
        restartPanel.SetActive(false);
    }


    private void CalculateCameraBounds()
    {
        float boardWidth = width * (tileSize + spacing) - spacing;
        float boardHeight = height * (tileSize + spacing) - spacing;
        float horizontalBound = boardWidth / 2;
        float verticalBound = boardHeight / 2;

        minBoundary = new Vector2(-horizontalBound, -verticalBound);
        maxBoundary = new Vector2(horizontalBound, verticalBound);
    }

    private Vector3 LimitWithinBoundsWithOvershoot(Vector3 position)
    {
        float x = Mathf.Clamp(position.x, minBoundary.x - maxOvershoot, maxBoundary.x + maxOvershoot);
        float y = Mathf.Clamp(position.y, minBoundary.y - maxOvershoot, maxBoundary.y + maxOvershoot);
        return new Vector3(x, y, position.z);
    }

    private void EnforceCameraBounds()
    {
        Vector3 currentPosition = gameCamera.transform.position;
        bool outOfBounds = false;

        if (currentPosition.x < minBoundary.x || currentPosition.x > maxBoundary.x ||
            currentPosition.y < minBoundary.y || currentPosition.y > maxBoundary.y)
        {
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
            // Calculate the board dimensions
            float boardWidth = width * (tileSize + spacing) - spacing;
            float boardHeight = height * (tileSize + spacing) - spacing;

            // Center the camera on the board
            gameCamera.transform.position = new Vector3(0, 0, gameCamera.transform.position.z);

            // Calculate the orthographic size required so that:
            // - Horizontally, the board plus 15% extra space is visible
            // - Vertically, the board fits exactly
            float horizontalRequired = (boardWidth * 1.50f) / (2 * gameCamera.aspect);
            float verticalRequired = boardHeight / 2;
            float dynamicMaxZoom = Mathf.Max(horizontalRequired, verticalRequired);

            // Set max zoom and use it as the starting zoom level.
            maxZoom = dynamicMaxZoom;
            gameCamera.orthographicSize = maxZoom;

            CalculateCameraBounds();
        }
    }


    private void CreateBoard()
    {
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

        grid = new MineTile[width, height];

        float boardWidth = width * (tileSize + spacing) - spacing;
        float boardHeight = height * (tileSize + spacing) - spacing;
        Vector3 boardOffset = new Vector3(-boardWidth / 2 + (tileSize / 2), -boardHeight / 2, 0);
        boardContainer.localPosition = Vector3.zero;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(
                    x * (tileSize + spacing) + boardOffset.x,
                    y * (tileSize + spacing) + boardOffset.y,
                    0
                );
                GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, boardContainer);
                tileObj.name = $"Tile_{x}_{y}";
                tileObj.transform.localScale = new Vector3(tileSize, tileSize, 1);

                MineTile tile = tileObj.GetComponent<MineTile>();
                if (tile == null)
                {
                    tile = tileObj.AddComponent<MineTile>();
                }

                tile.Setup(x, y, this);
                grid[x, y] = tile;
            }
        }

        flaggedCount = 0;
        revealedCount = 0;
    }

    public async void OnTileClicked(int x, int y)
    {
        if (currentState != GameState.Playing || IsPanning() || isAnimating) return;

        if (firstClick)
        {
            firstClick = false;
            hintButton.interactable = true;
            AnimateCameraToTile(x, y);
            difficultySelector.currentIndex = GameManager.Instance.currentDifficulty;
            await GenerateMinesTatham(x, y);
            
            
        }

        if (!grid[x, y].IsFlagged() && !grid[x, y].IsRevealed())
        {
            if (grid[x, y].GetAdjacentMines() == 0 && !grid[x, y].IsMine())
            {
                Debug.Log($"[DEBUG] Calling FloodFillWithAnimation at ({x}, {y}) after mines are placed.");
                isAnimating = true;
                FloodFillWithAnimation(x, y);
            }
            else
            {
                
                grid[x, y].Reveal();
                if (!grid[x, y].IsMine())
                {
                    revealedCount++;
                    CheckWinCondition();
                }
            }
        }
    }


    private async Task GenerateMinesTatham(int safeX, int safeY)
    {
        Debug.Log($"[DEBUG] Starting async mine generation for ({safeX}, {safeY})");

        minePositions.Clear();
        const int MAX_ATTEMPTS = 500;
        System.Random rng = new System.Random();
        TathamPuzzle finalPuzzle = null;
        int attempts = 0;
        float startTime = Time.realtimeSinceStartup;

        while (attempts < MAX_ATTEMPTS)
        {
            TathamPuzzle puzzle = await TathamPuzzleGenerator.GeneratePuzzleParallelAsync(width, height, mineCount, safeX, safeY);
            attempts++;
            if (puzzle != null)
            {
                finalPuzzle = puzzle;
                break;
            }
        }

        float duration = Time.realtimeSinceStartup - startTime;
        Debug.Log($"[DEBUG] Puzzle generation took {duration:F2} seconds over {attempts} attempts.");

        if (finalPuzzle == null)
        {
            Debug.LogWarning("[DEBUG] No valid puzzle found, using fallback.");
            finalPuzzle = new TathamPuzzle(width, height, mineCount);
        }

        Debug.Log($"[DEBUG] Using final puzzle. Safe Cell ({safeX}, {safeY}) Clue: {finalPuzzle.clues[safeX, safeY]}");

        // Assign mines to the grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isMine = false;
                int clue = 0;
                if (finalPuzzle.mines != null && x < finalPuzzle.width && y < finalPuzzle.height)
                {
                    isMine = finalPuzzle.mines[x, y];
                    clue = finalPuzzle.clues[x, y];
                }
                grid[x, y].SetMine(isMine);
                if (clue < 0) clue = 0;
                grid[x, y].SetAdjacentMines(clue);

                if (isMine)
                {
                    minePositions.Add(new Vector2Int(x, y));
                }
            }
        }

        Debug.Log($"[DEBUG] Board setup complete.");
    }

    public void FloodFillWithAnimation(int startX, int startY)
    {
        Debug.Log($"[DEBUG] FloodFillWithAnimation started at ({startX}, {startY})");

        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        List<MineTile> tilesToReveal = new List<MineTile>();

        toCheck.Enqueue(new Vector2Int(startX, startY));
        visited.Add(new Vector2Int(startX, startY));

        while (toCheck.Count > 0)
        {
            Vector2Int pos = toCheck.Dequeue();
            int x = pos.x;
            int y = pos.y;
            if (!IsValidCoord(x, y)) continue;

            MineTile tile = grid[x, y];
            if (tile.IsRevealed() || tile.IsFlagged()) continue;

            tilesToReveal.Add(tile);

            if (tile.GetAdjacentMines() == 0)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        int ny = y + dy;
                        Vector2Int newPos = new Vector2Int(nx, ny);
                        if (IsValidCoord(nx, ny) && !visited.Contains(newPos))
                        {
                            toCheck.Enqueue(newPos);
                            visited.Add(newPos);
                        }
                    }
                }
            }
        }

        if (tilesToReveal.Count > 0)
        {
            List<Transform> tileTransforms = new List<Transform>();
            List<System.Action> revealActions = new List<System.Action>();

            foreach (MineTile tile in tilesToReveal)
            {
                tileTransforms.Add(tile.transform);
                revealActions.Add(() => {
                    if (!tile.IsMine())
                    {
                        revealedCount++;
                    }
                    tile.RevealWithoutAnimation();
                });
            }

            TileAnimationManager.Instance.AnimateTileSequence(
                tileTransforms,
                revealActions,
                () => {
                    isAnimating = false;
                    CheckWinCondition();
                }
            );
        }
        else
        {
            isAnimating = false;
        }
    }



    private bool IsValidCoord(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private void CheckWinCondition()
    {
        if (revealedCount == (width * height - mineCount))
        {
            currentState = GameState.Win;
            
            togglePanel.SetActive(false);
            Debug.Log("Game Won!");
            RevealRemainingTiles();
            gameCamera.DOOrthoSize(maxZoom, 1.5f).SetEase(Ease.OutQuad);
            CenterCameraOnBoard();
            if (popup != null)
            {
                Debug.Log("Popup Found");
                popup.ShowPopup(gameManager.currentDifficulty, elapsedTime);
            }
            hintButton.gameObject.SetActive(false);

        }
    }

    public void OnMineRevealed()
    {
        togglePanel.SetActive(false);
        currentState = GameState.GameOver;
        RevealAllMines();
        gameCamera.DOOrthoSize(maxZoom, 1.5f).SetEase(Ease.OutQuad);
        CenterCameraOnBoard();
        HighlightIncorrectFlags();
        restartPanel.SetActive(true);
        hintButton.gameObject.SetActive(false);
        Debug.Log("Game Over!");
    }

    private void CenterCameraOnBoard()
    {
        Vector3 boardCenter = new Vector3(0, 0, gameCamera.transform.position.z);
        gameCamera.transform.DOMove(boardCenter, 1.5f).SetEase(Ease.OutQuad);
    }



    private void RevealAllMines()
    {
        foreach (var mp in minePositions)
        {
            MineTile t = grid[mp.x, mp.y];
            if (!t.IsRevealed())
            {
                t.Reveal();
            }
        }

        
    }

    private void RevealRemainingTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Only reveal if the tile isn't already revealed and isn't flagged.
                if (!grid[x, y].IsRevealed() && !grid[x, y].IsFlagged())
                {
                    grid[x, y].ForceReveal();
                }
            }
        }
    }

    public void OnTileFlagged(bool isFlagged)
    {
        flaggedCount += isFlagged ? 1 : -1;
        UpdateMineCounterUI();
    }

    public bool IsGameOver()
    {
        return currentState == GameState.GameOver || currentState == GameState.Win;
    }

    public bool IsPanning()
    {
        return isPanning || isMousePanning;
    }

    public int GetRemainingFlags()
    {
        return mineCount - flaggedCount;
    }

    public void RestartGame()
    {
        int difficulty = gameManager != null ? gameManager.currentDifficulty : 0;
        difficulty = Mathf.Clamp(difficulty, 0, 3);
        SetupGame(difficulty);
    }

   

    public bool IsValidCoordPublic(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public MineTile GetTileAt(int x, int y)
    {
        return grid[x, y];
    }

    public void IncrementRevealedCount()
    {
        revealedCount++;
        CheckWinCondition();
    }

    private void ToggleFlagMode(bool isOn)
    {
        isFlagMode = isOn;
    }

    public bool IsFlagMode()
    {
        return isFlagMode;
    }

    #region UI

    private void ShowErrorPopup(string message)
    {
        if (errorPopup != null && errorPopupText != null)
        {
            errorPopupText.text = message;
            errorPopup.SetActive(true);
            // Optionally, auto-hide after 3 seconds:
            Invoke(nameof(HideErrorPopup), 3f);
        }
    }

    /// <summary>
    /// Hides the error popup.
    /// </summary>
    public void HideErrorPopup()
    {
        if (errorPopup != null)
        {
            errorPopup.SetActive(false);
        }
    }


    public void LoadMainMenu()
    {
        gameManager.currentDifficulty = 0;
        popup.ClosePopup();
        SceneManager.LoadScene("HomePage");
    }

    private void HighlightIncorrectFlags()
    {
        foreach (MineTile tile in grid)
        {
            if (tile.IsFlagged() && !tile.IsMine()) // If the tile is flagged but NOT a mine
            {
                // Change flag to red "X"
                tile.SetIncorrectFlag();

                // Shake animation
                tile.transform.DOShakePosition(2.0f, 0.3f, 10, 90, false, true);
            }
        }
    }

    private void UpdateTimerUI()
    {
        int hours = Mathf.FloorToInt(elapsedTime / 3600);
        int minutes = Mathf.FloorToInt((elapsedTime % 3600) / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    private void UpdateMineCounterUI()
    {
        int remainingMines = mineCount - flaggedCount;
        mineCounterText.text = $"{remainingMines}";
    }

    public bool IsAnimationInProgress()
    {
        return isAnimating;
    }

    #endregion

    #region Hint


    public void OnHintButtonClicked()
    {
        if (currentState != GameState.Playing || firstClick || isAnimating)
        {
            Debug.Log("Hint not available right now.");
            return;
        }

        bool hintExecuted = PerformAdvancedHintMove();
        if (!hintExecuted)
        {
            // Inform the user that no forced move was found, likely due to incorrect flag placements.
            ShowErrorPopup("Move could not be deduced.\n\nIt appears some flags might be placed incorrectly.");
        }
    }


    private bool PerformAdvancedHintMove()
    {
        
        List<Constraint> constraints = new List<Constraint>();
        HashSet<Vector2Int> unknownSet = new HashSet<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                MineTile tile = grid[x, y];
                if (tile.IsRevealed() && !tile.IsMine())
                {
                    int clue = tile.GetAdjacentMines();
                    int flaggedCount = 0;
                    List<Vector2Int> unknownNeighbors = new List<Vector2Int>();

                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;
                            int nx = x + dx;
                            int ny = y + dy;
                            if (!IsValidCoord(nx, ny))
                                continue;

                            MineTile neighbor = grid[nx, ny];
                            if (neighbor.IsFlagged())
                            {
                                flaggedCount++;
                            }
                            else if (!neighbor.IsRevealed())
                            {
                                Vector2Int pos = new Vector2Int(nx, ny);
                                unknownNeighbors.Add(pos);
                            }
                        }
                    }

                    if (unknownNeighbors.Count > 0)
                    {
                        int minesNeeded = clue - flaggedCount;
                        
                        if (minesNeeded < 0)
                            continue;

                        constraints.Add(new Constraint(unknownNeighbors, minesNeeded));
                        foreach (var pos in unknownNeighbors)
                        {
                            unknownSet.Add(pos);
                        }
                    }
                }
            }
        }

        
        if (unknownSet.Count == 0)
            return false;

        
        List<Vector2Int> unknownCells = new List<Vector2Int>(unknownSet);
        Dictionary<Vector2Int, int> unknownIndices = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < unknownCells.Count; i++)
        {
            unknownIndices[unknownCells[i]] = i;
        }

       
        List<bool[]> validSolutions = new List<bool[]>();
        bool[] assignment = new bool[unknownCells.Count]; 
        BacktrackAll(0, unknownCells, unknownIndices, assignment, constraints, validSolutions);

        if (validSolutions.Count == 0)
            return false;


        for (int i = 0; i < unknownCells.Count; i++)
        {
            bool alwaysMine = true;
            bool alwaysSafe = true;
            foreach (var sol in validSolutions)
            {
                if (!sol[i])
                    alwaysMine = false;
                if (sol[i])
                    alwaysSafe = false;
            }

            if (alwaysMine || alwaysSafe)
            {
                Vector2Int hintPos = unknownCells[i];
                MineTile hintTile = grid[hintPos.x, hintPos.y];

                // Verify the deduced move against the actual board.
                if (alwaysMine)
                {
                    // The deduction forces this cell to be a mine.
                    // If it isn’t actually a mine, then the flags must be off.
                    if (!hintTile.IsMine())
                    {
                        ShowErrorPopup("Move could not be deduced.\n\nIt appears some flags might be placed incorrectly.");
                        return false;
                    }
                    else
                    {
                        // Animate and execute flagging.
                        hintTile.transform.DOShakePosition(2.0f, 0.3f, 10, 90, false, true);
                        hintTile.ToggleFlag();
                        return true;
                    }
                }
                else if (alwaysSafe)
                {
                    // The deduction forces this cell to be safe.
                    // If it is actually a mine, then the flags are wrong.
                    if (hintTile.IsMine())
                    {
                        ShowErrorPopup("Move could not be deduced.\n\nIt appears some flags might be placed incorrectly.");
                        return false;
                    }
                    else
                    {
                        // Animate and execute the reveal.
                        hintTile.transform.DOShakePosition(2.0f, 0.3f, 10, 90, false, true);
                        OnTileClicked(hintPos.x, hintPos.y);
                        return true;
                    }
                }
            }
        }

        return false;
    }


    private void BacktrackAll(int index,
                              List<Vector2Int> unknownCells,
                              Dictionary<Vector2Int, int> unknownIndices,
                              bool[] assignment,
                              List<Constraint> constraints,
                              List<bool[]> validSolutions)
    {
        if (index == unknownCells.Count)
        {
            if (CheckAllConstraints(unknownCells, unknownIndices, assignment, constraints))
            {
                bool[] sol = new bool[assignment.Length];
                assignment.CopyTo(sol, 0);
                validSolutions.Add(sol);
            }
            return;
        }

       
        assignment[index] = true;
        if (CheckPartial(index, unknownCells, unknownIndices, assignment, constraints))
            BacktrackAll(index + 1, unknownCells, unknownIndices, assignment, constraints, validSolutions);

        
        assignment[index] = false;
        if (CheckPartial(index, unknownCells, unknownIndices, assignment, constraints))
            BacktrackAll(index + 1, unknownCells, unknownIndices, assignment, constraints, validSolutions);
    }


    private bool CheckPartial(int assignedCount,
                              List<Vector2Int> unknownCells,
                              Dictionary<Vector2Int, int> unknownIndices,
                              bool[] assignment,
                              List<Constraint> constraints)
    {
        foreach (var c in constraints)
        {
            int mineSoFar = 0;
            int unassigned = 0;
            foreach (var cell in c.cells)
            {
                int idx;
                if (unknownIndices.TryGetValue(cell, out idx))
                {
                    if (idx < assignedCount)
                    {
                        if (assignment[idx])
                            mineSoFar++;
                    }
                    else
                    {
                        unassigned++;
                    }
                }
            }
            if (mineSoFar > c.minesNeeded)
                return false;
            if (mineSoFar + unassigned < c.minesNeeded)
                return false;
        }
        return true;
    }


    private bool CheckAllConstraints(List<Vector2Int> unknownCells,
                                     Dictionary<Vector2Int, int> unknownIndices,
                                     bool[] assignment,
                                     List<Constraint> constraints)
    {
        foreach (var c in constraints)
        {
            int mineCount = 0;
            foreach (var cell in c.cells)
            {
                int idx;
                if (unknownIndices.TryGetValue(cell, out idx))
                {
                    if (assignment[idx])
                        mineCount++;
                }
            }
            if (mineCount != c.minesNeeded)
                return false;
        }
        return true;
    }

    #endregion

    private class Constraint
    {
        public List<Vector2Int> cells;
        public int minesNeeded;
        public Constraint(List<Vector2Int> cells, int minesNeeded)
        {
            this.cells = cells;
            this.minesNeeded = minesNeeded;
        }
    }
}



