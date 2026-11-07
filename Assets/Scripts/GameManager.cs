
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Players")]
    public Transform player1;
    public Transform player2;

    public Tile player1Tile;
    public Tile player2Tile;

    public Tile finishTile;

    [Header("UI")]
    public TMP_Text diceText;
    public TMP_Text turnText;
    [Header("Turn Panel")]
    public Image turnPanelImage;
    [Header("Turn Panel Animation")]
    public Color player1PanelColor = new Color(0.05f, 0.12f, 0.3f, 0.85f);
    public Color player2PanelColor = new Color(0.05f, 0.25f, 0.12f, 0.85f);

    [Min(0.05f)]
    public float panelTransitionDuration = 0.4f;

    private Coroutine panelColorCoroutine;
    private int lastPanelPlayer = -1;
    [Header("Turn Indicator Colors")]
    public Color player1TurnColor = new Color(0.2f, 0.6f, 1f);
    public Color player2TurnColor = new Color(0.2f, 0.85f, 0.3f);
    public TMP_Text scoreP1Text;
    public TMP_Text scoreP2Text;
    public TMP_Text winText;

    [Header("Player Inventories")]
    public TMP_Text player1InventoryText;
    public TMP_Text player2InventoryText;

    [Header("Dice Animation")]
    public Image diceImage;
    public Button rollButton;

    public Sprite[] diceFaces = new Sprite[6];

    [Min(0.1f)]
    public float diceAnimationDuration = 1.5f;

    [Header("Wall Ability")]
    public Button wallButton;
    public TMP_Text wallCountText;

    [Min(1)]
    public int wallDurationInTurns = 2;

    [Header("Freeze Ability")]
    public Button freezeButton;
    public TMP_Text freezeCountText;

    [Min(0.1f)]
    public float frozenTurnMessageDuration = 2f;

    [Header("Bonus Generation")]
    public int startingPointBonuses = 4;
    public int startingWallBonuses = 1;
    public int startingFreezeBonuses = 1;

    private int currentPlayer = 1;
    private int remainingMoves = 0;

    private int itemsP1 = 0;
    private int itemsP2 = 0;

    private int wallChargesP1 = 0;
    private int wallChargesP2 = 0;

    private int freezeChargesP1 = 0;
    private int freezeChargesP2 = 0;

    // Bonuses waiting to respawn at the next active turn
    private int pendingBonusRespawns = 0;

    private bool hasRolled = false;
    private bool isRolling = false;
    private bool gameOver = false;

    private bool isWallPlacementMode = false;
    private bool isResolvingFrozenTurn = false;

    // 0 = nobody frozen
    // 1 = Player 1 frozen
    // 2 = Player 2 frozen
    private int frozenPlayer = 0;

    private Tile[] allTiles;

    private Tile player1StartTile;
    private Tile player2StartTile;

    private readonly List<Tile> activeWalls = new List<Tile>();

    private float gridOffset = 1f;

    private int activeBadTilesCount = 2;
    [Header("Player Movement Animation")]
    [Min(0.05f)]
    public float moveDuration = 0.3f;

    private bool isAnimatingMove = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (winText != null)
        {
            winText.text = "";
        }

        if (rollButton != null)
        {
            rollButton.interactable = true;
        }

        if (diceImage != null &&
            diceFaces != null &&
            diceFaces.Length > 0 &&
            diceFaces[0] != null)
        {
            diceImage.sprite = diceFaces[0];
            diceImage.color = Color.white;
            diceImage.rectTransform.localRotation = Quaternion.identity;
        }

        // Find all tiles
        allTiles = Object.FindObjectsByType<Tile>(
            FindObjectsSortMode.None
        );

        if (allTiles == null || allTiles.Length == 0)
        {
            Debug.LogError("GameManager: No tiles found!");
            return;
        }

        // Save starting positions
        player1StartTile = player1Tile;
        player2StartTile = player2Tile;

        // Find finish tile
        if (finishTile != null)
        {
            finishTile.isFinishTile = true;
        }
        else
        {
            foreach (Tile tile in allTiles)
            {
                if (tile.isFinishTile)
                {
                    finishTile = tile;
                    break;
                }
            }
        }

        // Calculate grid spacing
        if (allTiles.Length > 1)
        {
            float minDistance = float.MaxValue;

            Vector3 firstPos = allTiles[0].transform.position;

            for (int i = 1; i < allTiles.Length; i++)
            {
                float distance = Vector3.Distance(
                    firstPos,
                    allTiles[i].transform.position
                );

                if (distance > 0.1f && distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            if (minDistance < float.MaxValue)
            {
                gridOffset = minDistance;
            }
        }

        // Assign tile coordinates
        foreach (Tile tile in allTiles)
        {
            tile.x = Mathf.RoundToInt(
                tile.transform.position.x / gridOffset
            );

            tile.y = Mathf.RoundToInt(
                tile.transform.position.y / gridOffset
            );
        }

        // Position players
        if (player1 != null && player1Tile != null)
        {
            player1.position = player1Tile.transform.position;
        }

        if (player2 != null && player2Tile != null)
        {
            player2.position = player2Tile.transform.position;
        }

        // Generate traps and bonuses
        GenerateRandomBadTiles();
        GenerateRandomItems();

        UpdateUI();

        if (diceText != null)
        {
            diceText.text = "Roll the dice!";
        }
    }

    // =====================================================
    // TILE CLICKING
    // =====================================================

    public void OnTileClicked(Tile tile)
    {
        if (tile == null || gameOver || isResolvingFrozenTurn)
        {
            return;
        }

        if (isWallPlacementMode)
        {
            TryPlaceWall(tile);
            return;
        }

        TryMoveToTile(tile);
    }

    // =====================================================
    // DICE
    // =====================================================

    public void RollDice()
    {
        if (hasRolled ||
            isRolling ||
            gameOver ||
            isResolvingFrozenTurn ||
            isWallPlacementMode)
        {
            return;
        }

        hasRolled = true;
        isRolling = true;

        remainingMoves = 0;

        if (rollButton != null)
        {
            rollButton.interactable = false;
        }

        if (diceText != null)
        {
            diceText.text = "Rolling...";
        }

        StartCoroutine(AnimateDiceRoll());
    }

    private IEnumerator AnimateDiceRoll()
    {
        int result = Random.Range(1, 7);

        bool canAnimate =
            diceImage != null &&
            diceFaces != null &&
            diceFaces.Length >= 6;

        if (canAnimate)
        {
            for (int i = 0; i < 6; i++)
            {
                if (diceFaces[i] == null)
                {
                    canAnimate = false;
                    break;
                }
            }
        }

        if (canAnimate)
        {
            float elapsed = 0f;
            float nextFaceTime = 0f;

            while (elapsed < diceAnimationDuration)
            {
                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / diceAnimationDuration
                );

                float rotationSpeed = Mathf.Lerp(
                    1100f,
                    100f,
                    progress
                );

                diceImage.rectTransform.Rotate(
                    0f,
                    0f,
                    -rotationSpeed * Time.deltaTime
                );

                if (elapsed >= nextFaceTime)
                {
                    int randomFace = Random.Range(0, 6);

                    diceImage.sprite = diceFaces[randomFace];

                    float faceInterval = Mathf.Lerp(
                        0.06f,
                        0.24f,
                        progress * progress
                    );

                    nextFaceTime = elapsed + faceInterval;
                }

                yield return null;
            }

            diceImage.sprite = diceFaces[result - 1];

            diceImage.rectTransform.localRotation =
                Quaternion.identity;
        }
        else
        {
            Debug.LogWarning(
                "Dice Animation: Assign Dice Image and all 6 Dice Faces!"
            );
        }

        remainingMoves = result;
        isRolling = false;

        if (diceText != null)
        {
            diceText.text = "Moves left: " + remainingMoves;
        }

// Show available movement tiles
        RefreshMoveHighlights();

        Debug.Log("Dice result: " + result);
    }

    // =====================================================
    // PLAYER MOVEMENT
    // =====================================================

    public void TryMoveToTile(Tile targetTile)
{
    if (targetTile == null ||
        !hasRolled ||
        isRolling ||
        isAnimatingMove ||
        remainingMoves <= 0 ||
        gameOver ||
        isResolvingFrozenTurn)
    {
        return;
    }

    Tile currentTile =
        currentPlayer == 1 ? player1Tile : player2Tile;

    if (currentTile == null)
    {
        Debug.LogError("Player tile is not assigned!");
        return;
    }

    // Check adjacency
    int distance =
        Mathf.Abs(targetTile.x - currentTile.x) +
        Mathf.Abs(targetTile.y - currentTile.y);

    if (distance != 1)
    {
        Debug.LogWarning("Selected tile is not adjacent.");
        return;
    }

    // Check walls
    if (targetTile.isWall)
    {
        if (diceText != null)
        {
            diceText.text = "BLOCKED! Wall is in the way.";
        }

        return;
    }

    // Check occupied tiles
    if (currentPlayer == 1 && targetTile == player2Tile)
    {
        return;
    }

    if (currentPlayer == 2 && targetTile == player1Tile)
    {
        return;
    }

    Transform activePlayer =
        currentPlayer == 1 ? player1 : player2;

    if (activePlayer == null)
    {
        Debug.LogError("Active player is not assigned!");
        return;
    }

    // Start smooth movement
    isAnimatingMove = true;

    StartCoroutine(SmoothMovePlayer(
        activePlayer,
        targetTile,
        currentPlayer
    ));
}

// =====================================================
// SMOOTH PLAYER MOVEMENT
// =====================================================

private IEnumerator SmoothMovePlayer(
    Transform movingPlayer,
    Tile targetTile,
    int movingPlayerNumber)
{
    if (movingPlayer == null || targetTile == null)
    {
        isAnimatingMove = false;
        RefreshMoveHighlights();
        yield break;
    }

    Vector3 startPosition = movingPlayer.position;
    Vector3 targetPosition = targetTile.transform.position;

    float elapsed = 0f;
    float duration = Mathf.Max(0.05f, moveDuration);

    // Smooth movement animation
    while (elapsed < duration)
    {
        if (movingPlayer == null || targetTile == null)
        {
            isAnimatingMove = false;
            RefreshMoveHighlights();
            yield break;
        }

        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);

        // Smooth acceleration and deceleration
        t = t * t * (3f - 2f * t);

        movingPlayer.position =
            Vector3.Lerp(startPosition, targetPosition, t);

        yield return null;
    }

    if (movingPlayer == null || targetTile == null)
    {
        isAnimatingMove = false;
        RefreshMoveHighlights();
        yield break;
    }

    // Ensure exact final position
    movingPlayer.position = targetPosition;

    // Update player tile
    if (movingPlayerNumber == 1)
    {
        player1Tile = targetTile;
    }
    else
    {
        player2Tile = targetTile;
    }

    remainingMoves--;

    if (diceText != null)
    {
        diceText.text = "Moves left: " + remainingMoves;
    }

    // Apply tile effects after movement
    EvaluateTileEffect(targetTile);

    // Movement animation finished
    isAnimatingMove = false;

    // Update available movement highlights
    RefreshMoveHighlights();

    if (gameOver)
    {
        yield break;
    }

    // End turn when no moves remain
    if (remainingMoves == 0)
    {
        EndTurn();
    }
}

    private void EvaluateTileEffect(Tile tile)
    {
        // Trap
        if (tile.isBadTile && !tile.isRevealed)
        {
            tile.RevealTile();

            if (currentPlayer == 1)
            {
                itemsP1 = Mathf.Max(0, itemsP1 - 2);
            }
            else
            {
                itemsP2 = Mathf.Max(0, itemsP2 - 2);
            }

            Debug.Log("Trap! -2 points.");

            tile.isBadTile = false;

            StartCoroutine(ReplaceBadTile(tile));
        }

        // Bonus
        if (tile.hasItem)
        {
            BonusType collectedBonus = tile.bonusType;

            tile.CollectItem();

            switch (collectedBonus)
            {
                case BonusType.Points:

                    if (currentPlayer == 1)
                    {
                        itemsP1++;
                    }
                    else
                    {
                        itemsP2++;
                    }

                    Debug.Log("Points collected! +1 point.");

                    if (diceText != null)
                    {
                        diceText.text = "Bonus collected! +1 point.";
                    }

                    break;

                case BonusType.Wall:

                    if (currentPlayer == 1)
                    {
                        wallChargesP1++;
                    }
                    else
                    {
                        wallChargesP2++;
                    }

                    Debug.Log("Wall ability collected!");

                    if (diceText != null)
                    {
                        diceText.text = "WALL ABILITY COLLECTED!";
                    }

                    break;

                case BonusType.Freeze:

                    if (currentPlayer == 1)
                    {
                        freezeChargesP1++;
                    }
                    else
                    {
                        freezeChargesP2++;
                    }

                    Debug.Log("Freeze ability collected!");

                    if (diceText != null)
                    {
                        diceText.text = "FREEZE ABILITY COLLECTED!";
                    }

                    break;
            }

            // Queue a bonus to respawn at the next active turn
            pendingBonusRespawns++;

            Debug.Log(
                "Bonus respawn queued. Pending: " +
                pendingBonusRespawns
            );

            UpdateUI();
        }

        // Finish
        if (tile == finishTile || tile.isFinishTile)
        {
            if (currentPlayer == 1 && itemsP1 >= 10)
            {
                if (winText != null)
                {
                    winText.text = "PLAYER 1 WINS!";
                }

                gameOver = true;
            }
            else if (currentPlayer == 2 && itemsP2 >= 10)
            {
                if (winText != null)
                {
                    winText.text = "PLAYER 2 WINS!";
                }

                gameOver = true;
            }
            else
            {
                Debug.Log("You need 10 points to win!");
            }
        }

        if (gameOver)
        {
            if (rollButton != null)
            {
                rollButton.interactable = false;
            }

            if (wallButton != null)
            {
                wallButton.interactable = false;
            }

            if (freezeButton != null)
            {
                freezeButton.interactable = false;
            }
        }

        UpdateUI();
    }

    // =====================================================
    // BONUS GENERATION
    // =====================================================

    private void GenerateRandomItems()
    {
        foreach (Tile tile in allTiles)
        {
            tile.hasItem = false;
            tile.bonusType = BonusType.Points;
            tile.HideItem();
        }

        List<BonusType> bonusTypes = new List<BonusType>();

        for (int i = 0; i < startingPointBonuses; i++)
        {
            bonusTypes.Add(BonusType.Points);
        }

        for (int i = 0; i < startingWallBonuses; i++)
        {
            bonusTypes.Add(BonusType.Wall);
        }

        for (int i = 0; i < startingFreezeBonuses; i++)
        {
            bonusTypes.Add(BonusType.Freeze);
        }

        // Shuffle bonus types
        for (int i = 0; i < bonusTypes.Count; i++)
        {
            int randomIndex = Random.Range(i, bonusTypes.Count);

            BonusType temp = bonusTypes[i];
            bonusTypes[i] = bonusTypes[randomIndex];
            bonusTypes[randomIndex] = temp;
        }

        int spawnedItems = 0;

        foreach (BonusType bonusType in bonusTypes)
        {
            int attempts = 0;
            bool spawned = false;

            while (attempts < 500 && !spawned)
            {
                attempts++;

                Tile randomTile = allTiles[
                    Random.Range(0, allTiles.Length)
                ];

                if (!CanSpawnItem(randomTile))
                {
                    continue;
                }

                randomTile.hasItem = true;
                randomTile.bonusType = bonusType;
                randomTile.ShowItem();

                spawned = true;
                spawnedItems++;
            }
        }

        Debug.Log("Bonuses generated: " + spawnedItems);
    }

    // Spawn all queued bonuses when a new active turn begins
    private void SpawnPendingBonusItems()
    {
        if (pendingBonusRespawns <= 0)
        {
            return;
        }

        int bonusesToSpawn = pendingBonusRespawns;

        for (int i = 0; i < bonusesToSpawn; i++)
        {
            if (SpawnRandomBonus())
            {
                pendingBonusRespawns--;
            }
            else
            {
                Debug.LogWarning(
                    "No available tile for bonus respawn. " +
                    "The bonus will retry next turn."
                );

                break;
            }
        }

        UpdateUI();
    }

    private bool SpawnRandomBonus()
    {
        List<Tile> availableTiles = new List<Tile>();

        foreach (Tile tile in allTiles)
        {
            if (CanSpawnItem(tile))
            {
                availableTiles.Add(tile);
            }
        }

        if (availableTiles.Count == 0)
        {
            return false;
        }

        Tile randomTile = availableTiles[
            Random.Range(0, availableTiles.Count)
        ];

        // Randomly select Points, Wall, or Freeze
        int roll = Random.Range(0, 100);

        BonusType randomBonus;

        if (roll < 80)
            randomBonus = BonusType.Points;
        else if (roll < 92)
            randomBonus = BonusType.Wall;
        else
            randomBonus = BonusType.Freeze;
        
        randomTile.hasItem = true;
        randomTile.bonusType = randomBonus;
        randomTile.ShowItem();

        Debug.Log(
            "New " + randomBonus +
            " bonus spawned on " + randomTile.name
        );

        return true;
    }

    private bool CanSpawnItem(Tile tile)
    {
        if (tile == null)
        {
            return false;
        }

        if (tile == finishTile || tile.isFinishTile)
        {
            return false;
        }

        if (tile == player1StartTile || tile == player2StartTile)
        {
            return false;
        }

        if (tile == player1Tile || tile == player2Tile)
        {
            return false;
        }

        if (tile.isBadTile || tile.isRevealed)
        {
            return false;
        }

        if (tile.hasItem || tile.isWall)
        {
            return false;
        }

        return true;
    }

    // =====================================================
    // WALL ABILITY
    // =====================================================

    public void ActivateWallPlacement()
    {
        if (gameOver || isResolvingFrozenTurn || isRolling)
        {
            return;
        }

        int charges = currentPlayer == 1
            ? wallChargesP1
            : wallChargesP2;

        if (charges <= 0)
        {
            Debug.Log("You do not have any wall charges.");
            return;
        }

        isWallPlacementMode = !isWallPlacementMode;

        if (isWallPlacementMode)
        {
            if (diceText != null)
            {
                diceText.text = "WALL MODE: Select a tile to block.";
            }

            Debug.Log("Wall placement mode activated.");
        }
        else
        {
            if (diceText != null)
            {
                diceText.text = hasRolled
                    ? "Moves left: " + remainingMoves
                    : "Roll the dice!";
            }

            Debug.Log("Wall placement mode cancelled.");
        }

        UpdateUI();
    }

    private void TryPlaceWall(Tile tile)
    {
        if (!isWallPlacementMode || tile == null)
        {
            return;
        }

        // Cannot place a wall on a player
        if (tile == player1Tile || tile == player2Tile)
        {
            Debug.Log("Cannot place a wall on a player.");
            return;
        }

        // Cannot place a wall on the finish tile
        if (tile == finishTile || tile.isFinishTile)
        {
            Debug.Log("Cannot place a wall on the finish tile.");
            return;
        }

        // Cannot place a wall on bonuses
        if (tile.hasItem)
        {
            Debug.Log("Cannot place a wall on a bonus.");
            return;
        }

        // Cannot place a wall on traps
        if (tile.isBadTile)
        {
            Debug.Log("Cannot place a wall on a trap.");
            return;
        }

        // Cannot place a wall on another wall
        if (tile.isWall)
        {
            Debug.Log("There is already a wall here.");
            return;
        }

        int charges = currentPlayer == 1
            ? wallChargesP1
            : wallChargesP2;

        if (charges <= 0)
        {
            isWallPlacementMode = false;
            UpdateUI();
            return;
        }

        // Consume wall charge
        if (currentPlayer == 1)
        {
            wallChargesP1--;
        }
        else
        {
            wallChargesP2--;
        }

        // Place wall
        tile.PlaceWall(wallDurationInTurns);

        if (!activeWalls.Contains(tile))
        {
            activeWalls.Add(tile);
        }

        isWallPlacementMode = false;

        if (diceText != null)
        {
            diceText.text = "WALL PLACED! Moves left: " + remainingMoves;
        }

        Debug.Log(
            "Player " + currentPlayer +
            " placed a wall. Duration: " +
            wallDurationInTurns + " completed turns."
        );

        UpdateUI();
    }

    private void TickWalls()
    {
        for (int i = activeWalls.Count - 1; i >= 0; i--)
        {
            Tile wall = activeWalls[i];

            if (wall == null)
            {
                activeWalls.RemoveAt(i);
                continue;
            }

            wall.wallTurnsRemaining--;

            if (wall.wallTurnsRemaining <= 0)
            {
                wall.RemoveWall();
                activeWalls.RemoveAt(i);

                Debug.Log("A wall has expired.");
            }
        }
    }

    // =====================================================
    // FREEZE ABILITY
    // =====================================================

    public void UseFreezeAbility()
    {
        if (gameOver || isResolvingFrozenTurn || isRolling)
        {
            return;
        }

        int charges = currentPlayer == 1
            ? freezeChargesP1
            : freezeChargesP2;

        if (charges <= 0)
        {
            Debug.Log("You do not have any freeze charges.");
            return;
        }

        int opponent = currentPlayer == 1 ? 2 : 1;

        // Do not overwrite an existing freeze
        if (frozenPlayer == opponent)
        {
            Debug.Log("The opponent is already frozen.");
            return;
        }

        // Consume freeze charge
        if (currentPlayer == 1)
        {
            freezeChargesP1--;
        }
        else
        {
            freezeChargesP2--;
        }

        frozenPlayer = opponent;

        if (diceText != null)
        {
            diceText.text = "PLAYER " + opponent + " FROZEN!";
        }

        Debug.Log(
            "Player " + currentPlayer +
            " froze Player " + opponent + "."
        );

        UpdateUI();
    }

    // =====================================================
    // TURN MANAGEMENT
    // =====================================================

    private void EndTurn()
    {
        if (gameOver)
        {
            return;
        }

        // Complete the current player's turn.
        // Walls lose one turn of duration.
        TickWalls();

        currentPlayer = currentPlayer == 1 ? 2 : 1;

        hasRolled = false;
        remainingMoves = 0;
        isWallPlacementMode = false;

        // If this player is frozen, skip their turn first.
        if (frozenPlayer == currentPlayer)
        {
            StartCoroutine(SkipFrozenPlayerTurn());
            return;
        }

        // This is the next active player's turn.
        SpawnPendingBonusItems();

        if (diceText != null)
        {
            diceText.text = "Roll the dice!";
        }

        if (rollButton != null)
        {
            rollButton.interactable = true;
        }

        UpdateUI();
    }

    private IEnumerator SkipFrozenPlayerTurn()
    {
        isResolvingFrozenTurn = true;

        int skippedPlayer = currentPlayer;
        int nextPlayer = skippedPlayer == 1 ? 2 : 1;

        // Remove the freeze effect
        frozenPlayer = 0;

        // Disable buttons during the skipped turn message
        if (rollButton != null)
        {
            rollButton.interactable = false;
        }

        if (wallButton != null)
        {
            wallButton.interactable = false;
        }

        if (freezeButton != null)
        {
            freezeButton.interactable = false;
        }

        if (diceText != null)
        {
            diceText.text =
                "PLAYER " + skippedPlayer +
                " IS FROZEN!\nTURN SKIPPED!";
        }

        if (turnText != null)
        {
            turnText.text =
                "PLAYER " + skippedPlayer +
                " FROZEN - TURN SKIPPED!\nPLAYER " +
                nextPlayer + " GOES AGAIN!";

            turnText.color = nextPlayer == 1
                ? player1TurnColor
                : player2TurnColor;
        }
        UpdateTurnPanelColor(nextPlayer);

        Debug.Log("Player " + skippedPlayer + "'s turn is skipped.");

        yield return new WaitForSeconds(frozenTurnMessageDuration);

        // Complete the skipped turn
        TickWalls();

        // Return the turn to the player who used Freeze
        currentPlayer = nextPlayer;

        hasRolled = false;
        remainingMoves = 0;
        isWallPlacementMode = false;

        isResolvingFrozenTurn = false;

        // Respawn pending bonuses at the next active player's turn
        SpawnPendingBonusItems();

        if (diceText != null)
        {
            diceText.text = "Roll the dice!";
        }

        if (rollButton != null)
        {
            rollButton.interactable = true;
        }

        UpdateUI();

        Debug.Log("Player " + currentPlayer + " gets another turn.");
    }

    // =====================================================
    // TRAP GENERATION
    // =====================================================

    private void GenerateRandomBadTiles()
    {
        foreach (Tile tile in allTiles)
        {
            tile.HideBadTile();
        }

        int spawnedBadTiles = 0;
        int attempts = 0;

        while (
            spawnedBadTiles < activeBadTilesCount &&
            attempts < 500
        )
        {
            attempts++;

            Tile randomTile = allTiles[
                Random.Range(0, allTiles.Length)
            ];

            if (!CanSpawnBadTile(randomTile))
            {
                continue;
            }

            randomTile.SetBadTileHidden();

            spawnedBadTiles++;
        }

        Debug.Log("Traps generated: " + spawnedBadTiles);
    }

    private void SpawnNewBadTile()
    {
        int attempts = 0;

        while (attempts < 500)
        {
            attempts++;

            Tile randomTile = allTiles[
                Random.Range(0, allTiles.Length)
            ];

            if (!CanSpawnBadTile(randomTile))
            {
                continue;
            }

            randomTile.SetBadTileHidden();

            Debug.Log("New hidden trap: " + randomTile.name);

            return;
        }

        Debug.LogWarning("Could not find a place for a new trap.");
    }

    private bool CanSpawnBadTile(Tile tile)
    {
        if (tile == null)
        {
            return false;
        }

        if (tile == finishTile || tile.isFinishTile)
        {
            return false;
        }

        if (tile == player1StartTile || tile == player2StartTile)
        {
            return false;
        }

        if (tile == player1Tile || tile == player2Tile)
        {
            return false;
        }

        if (tile.hasItem || tile.isWall)
        {
            return false;
        }

        if (tile.isBadTile || tile.isRevealed)
        {
            return false;
        }

        return true;
    }

    private IEnumerator ReplaceBadTile(Tile oldTile)
    {
        yield return new WaitForSeconds(0.6f);

        if (oldTile != null)
        {
            oldTile.HideBadTile();
        }

        if (!gameOver)
        {
            SpawnNewBadTile();
        }
    }

    // =====================================================
    // UI
    // =====================================================

    private void UpdateUI()
    {
        if (turnText != null && !isResolvingFrozenTurn)
        {
            turnText.text = "PLAYER " + currentPlayer + " TURN";

            turnText.color = currentPlayer == 1
                ? player1TurnColor
                : player2TurnColor;
        }
        if (!isResolvingFrozenTurn)
        {
            UpdateTurnPanelColor(currentPlayer);
        }
        if (player1InventoryText != null)
        {
            player1InventoryText.text =
                "PLAYER 1\n" +
                "Points: " + itemsP1 + "/10\n" +
                "Walls: " + wallChargesP1 + "\n" +
                "Freeze: " + freezeChargesP1;
        }

        // Player 2 inventory
        if (player2InventoryText != null)
        {
            player2InventoryText.text =
                "PLAYER 2\n" +
                "Points: " + itemsP2 + "/10\n" +
                "Walls: " + wallChargesP2 + "\n" +
                "Freeze: " + freezeChargesP2;
        }

        // Active player's wall count
        if (wallCountText != null)
        {
            wallCountText.text =
                currentPlayer == 1
                    ? "Walls: " + wallChargesP1
                    : "Walls: " + wallChargesP2;
        }

        // Active player's freeze count
        if (freezeCountText != null)
        {
            freezeCountText.text =
                currentPlayer == 1
                    ? "Freeze: " + freezeChargesP1
                    : "Freeze: " + freezeChargesP2;
        }

        if (wallButton != null)
        {
            int wallCharges = currentPlayer == 1
                ? wallChargesP1
                : wallChargesP2;

            wallButton.interactable =
                wallCharges > 0 &&
                !gameOver &&
                !isResolvingFrozenTurn &&
                !isRolling;

            TMP_Text buttonText =
                wallButton.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                buttonText.text = isWallPlacementMode
                    ? "Cancel Wall"
                    : "Place Wall";
            }
        }

        if (freezeButton != null)
        {
            int freezeCharges = currentPlayer == 1
                ? freezeChargesP1
                : freezeChargesP2;

            freezeButton.interactable =
                freezeCharges > 0 &&
                !gameOver &&
                !isResolvingFrozenTurn &&
                !isRolling;

            TMP_Text buttonText =
                freezeButton.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                buttonText.text = "Freeze";
            }
        }
        RefreshMoveHighlights();
    }
    // =====================================================
// RESTART GAME
// =====================================================

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
    // =====================================================
// RETURN TO MAIN MENU
// =====================================================

    [Header("Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    public void ReturnToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            mainMenuSceneName
        );
    }
    // =====================================================
// TURN PANEL COLOR ANIMATION
// =====================================================

    private void UpdateTurnPanelColor(int playerNumber)
    {
        if (turnPanelImage == null)
            return;

        if (lastPanelPlayer == playerNumber)
            return;

        lastPanelPlayer = playerNumber;

        Color targetColor = playerNumber == 1
            ? player1PanelColor
            : player2PanelColor;

        if (panelColorCoroutine != null)
        {
            StopCoroutine(panelColorCoroutine);
        }

        panelColorCoroutine = StartCoroutine(
            AnimatePanelColor(targetColor)
        );
    }

    private IEnumerator AnimatePanelColor(Color targetColor)
    {
        Color startColor = turnPanelImage.color;

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, panelTransitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth transition
            t = Mathf.SmoothStep(0f, 1f, t);

            turnPanelImage.color =
                Color.Lerp(startColor, targetColor, t);

            yield return null;
        }

        turnPanelImage.color = targetColor;
        panelColorCoroutine = null;
    }
    // =====================================================
// AVAILABLE MOVEMENT HIGHLIGHTS
// =====================================================

    private void RefreshMoveHighlights()
    {
        Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);

        // First, remove all existing highlights
        foreach (Tile tile in allTiles)
        {
            tile.SetMoveHighlight(false, Color.white);
        }

        // Check whether the player can move
        if (!hasRolled ||
            isRolling ||
            isAnimatingMove ||
            isWallPlacementMode ||
            remainingMoves <= 0 ||
            gameOver ||
            isResolvingFrozenTurn)
        {
            return;
        }

        // Get current player position
        Tile currentTile = currentPlayer == 1
            ? player1Tile
            : player2Tile;

        // Get opponent position
        Tile opponentTile = currentPlayer == 1
            ? player2Tile
            : player1Tile;

        if (currentTile == null)
            return;

        // Choose highlight color
        Color highlightColor = currentPlayer == 1
            ? player1TurnColor
            : player2TurnColor;

        foreach (Tile tile in allTiles)
        {
            if (tile == null)
                continue;

            // Check adjacency
            int distance =
                Mathf.Abs(tile.x - currentTile.x) +
                Mathf.Abs(tile.y - currentTile.y);

            bool isAdjacent = distance == 1;

            // Check movement restrictions
            bool isAvailable =
                isAdjacent &&
                !tile.isWall &&
                tile != opponentTile;

            if (isAvailable)
            {
                tile.SetMoveHighlight(true, highlightColor);
            }
        }
    }
}
