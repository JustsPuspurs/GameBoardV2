using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum TurnPhase
    {
        HeroTurn,
        MonsterTurn,
        GameOver
    }

    [Header("Warrior (legacy Player 1 fields kept for scene compatibility)")]
    public Transform player1;
    public Tile player1Tile;

    [Tooltip("Legacy Player 2 references are no longer used by DR-02.")]
    public Transform player2;
    public Tile player2Tile;

    public Tile finishTile;

    [Header("Player Movement")]
    public PlayerMovement playerMovement;
    [Min(1)] public int agility = 2;

    [Header("UI")]
    public TMP_Text diceText;
    public TMP_Text turnText;
    public TMP_Text player1InventoryText;
    public TMP_Text winText;
    public TMP_Text scoreP1Text;

    [Header("Dice Animation")]
    public Image diceImage;
    public Image diceImage2;
    public Button rollButton;
    public Button endTurnButton;
    public Sprite[] diceFaces = new Sprite[6];
    [Min(0.1f)] public float diceAnimationDuration = 1.5f;

    [Header("Turn Panel")]
    public Image turnPanelImage;
    public Color heroPanelColor = new Color(0.05f, 0.12f, 0.3f, 0.85f);
    public Color monsterPanelColor = new Color(0.25f, 0.08f, 0.08f, 0.85f);
    [Min(0.05f)] public float panelTransitionDuration = 0.4f;

    [Header("Monster Turn Integration")]
    public UnityEvent onMonsterTurnStarted;
    public bool autoCompleteMonsterPhaseWhenNoListener = true;

    [Header("Bonus and Trap Generation")]
    [Min(0)] public int startingPointBonuses = 4;
    [Min(0)] public int activeBadTilesCount = 2;

    [Header("Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    private TurnPhase currentPhase = TurnPhase.HeroTurn;
    private int remainingMoves;
    private int collectedPoints;
    private int pendingBonusRespawns;
    private bool hasRolled;
    private bool isRolling;
    private bool gameOver;

    private Tile[] allTiles;
    private Tile warriorStartTile;
    private Coroutine panelColorCoroutine;
    private Coroutine monsterPhaseCoroutine;
    private int lastPanelPhase = -1;

    public int RemainingMoves => remainingMoves;
    public TurnPhase CurrentPhase => currentPhase;
    public bool HasRolled => hasRolled;
    public bool IsBusy => isRolling || (playerMovement != null && playerMovement.IsAnimatingMove);
    public bool CanWarriorAttemptMovement =>
        !gameOver &&
        currentPhase == TurnPhase.HeroTurn &&
        hasRolled &&
        !isRolling &&
        playerMovement != null &&
        !playerMovement.IsAnimatingMove &&
        remainingMoves > 0 &&
        player1 != null &&
        player1Tile != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("More than one GameManager exists in the scene. The newest one will be used.");

        Instance = this;
    }

    private void Start()
    {
        if (winText != null)
            winText.text = string.Empty;

        allTiles = FindObjectsOfType<Tile>();
        if (allTiles == null || allTiles.Length == 0)
        {
            Debug.LogError("GameManager: No Tile objects found in the scene.");
            enabled = false;
            return;
        }
        CalculateGridCoordinates();
        
        if (player1 == null)
            Debug.LogError("GameManager: Assign the Warrior to the Player 1 field.");

        if (player1Tile == null)
            Debug.LogError("GameManager: Assign the Warrior starting tile to Player 1 Tile.");

        if (playerMovement == null && player1 != null)
            playerMovement = player1.GetComponent<PlayerMovement>();

        if (playerMovement == null)
        {
            Debug.LogError("GameManager: Add PlayerMovement to the Warrior and assign it here (or let it auto-find on the Warrior).");
            enabled = false;
            return;
        }

        warriorStartTile = player1Tile;
        FindFinishTile();

        playerMovement.Initialize(this, player1, player1Tile, allTiles);

        GenerateRandomBadTiles();
        GenerateRandomItems();

        currentPhase = TurnPhase.HeroTurn;
        hasRolled = false;
        remainingMoves = 0;

        UpdateUI();
        SetDiceImagesToDefault();
        UpdateButtons();

        if (diceText != null)
            diceText.text = "Warrior turn - roll the dice.";
    }
    private void CalculateGridCoordinates()
    {
        List<float> xPositions = new List<float>();
        List<float> yPositions = new List<float>();

        foreach (Tile tile in allTiles)
        {
            if (tile == null) continue;

            float x = tile.transform.position.x;
            float y = tile.transform.position.y;

            if (!xPositions.Exists(p => Mathf.Abs(p - x) < 0.001f))
                xPositions.Add(x);

            if (!yPositions.Exists(p => Mathf.Abs(p - y) < 0.001f))
                yPositions.Add(y);
        }

        xPositions.Sort();
        yPositions.Sort();

        foreach (Tile tile in allTiles)
        {
            if (tile == null) continue;

            tile.x = GetCoordinateIndex(xPositions, tile.transform.position.x);
            tile.y = GetCoordinateIndex(yPositions, tile.transform.position.y);
        }

        Debug.Log("Tile coordinates initialized successfully.");
    }

    private int GetCoordinateIndex(List<float> positions, float value)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            if (Mathf.Abs(positions[i] - value) < 0.001f)
                return i;
        }

        return -1;
    }

    private float FindGridSpacing(List<float> positions)
    {
        float spacing = float.MaxValue;

        for (int i = 1; i < positions.Count; i++)
        {
            float difference = positions[i] - positions[i - 1];

            if (difference > 0.001f && difference < spacing)
                spacing = difference;
        }

        return spacing == float.MaxValue ? 1f : spacing;
    }
    private void FindFinishTile()
    {
        if (finishTile != null)
        {
            finishTile.isFinishTile = true;
            finishTile.RefreshColor();
            return;
        }

        foreach (Tile tile in allTiles)
        {
            if (tile != null && tile.isFinishTile)
            {
                finishTile = tile;
                return;
            }
        }
    }

    public void OnTileClicked(Tile tile)
    {
        if (playerMovement != null)
            playerMovement.OnTileClicked(tile);
    }

    public void SetStatusText(string message)
    {
        if (diceText != null)
            diceText.text = message;
    }

    public void SpendMovementPoints(int cost)
    {
        remainingMoves = Mathf.Max(0, remainingMoves - Mathf.Max(0, cost));
        UpdateButtons();
    }

    public void OnWarriorMovedToTile(Tile tile)
    {
        if (tile == null)
            return;

        player1Tile = tile;
        EvaluateTileEffect(tile);

        if (diceText != null && !gameOver)
            diceText.text = "Movement points left: " + remainingMoves;

        UpdateUI();
        UpdateButtons();
    }

    public void RollDice()
    {
        if (gameOver || currentPhase != TurnPhase.HeroTurn || hasRolled || isRolling ||
            (playerMovement != null && playerMovement.IsAnimatingMove))
            return;

        hasRolled = true;
        isRolling = true;
        remainingMoves = 0;
        UpdateButtons();
        RefreshMoveHighlights();

        if (diceText != null)
            diceText.text = "Rolling " + Mathf.Max(1, agility) + "D6...";

        StartCoroutine(AnimateDiceRoll());
    }

    private IEnumerator AnimateDiceRoll()
    {
        int diceCount = Mathf.Max(1, agility);
        int[] results = new int[diceCount];
        int total = 0;

        for (int i = 0; i < diceCount; i++)
        {
            results[i] = Random.Range(1, 7);
            total += results[i];
        }

        bool hasValidFaces = diceFaces != null && diceFaces.Length >= 6;
        if (hasValidFaces)
        {
            for (int i = 0; i < 6; i++)
            {
                if (diceFaces[i] == null)
                {
                    hasValidFaces = false;
                    break;
                }
            }
        }

        if (hasValidFaces && diceImage != null)
        {
            Coroutine firstAnimation = StartCoroutine(AnimateSingleDie(diceImage, results[0]));
            Coroutine secondAnimation = null;

            if (diceCount >= 2 && diceImage2 != null)
                secondAnimation = StartCoroutine(AnimateSingleDie(diceImage2, results[1]));

            yield return firstAnimation;
            if (secondAnimation != null)
                yield return secondAnimation;
        }
        else
        {
            Debug.LogWarning("Dice animation skipped. Assign Dice Image and all six Dice Faces in the Inspector.");
            yield return new WaitForSeconds(Mathf.Max(0.1f, diceAnimationDuration));
        }

        remainingMoves = total;
        isRolling = false;

        string resultsText = string.Join(" + ", results);
        if (diceText != null)
            diceText.text = "Roll: " + resultsText + " = " + total + " movement points.";

        UpdateButtons();
        RefreshMoveHighlights();
        Debug.Log("Warrior rolled " + resultsText + " = " + total + " movement points.");
    }

    private IEnumerator AnimateSingleDie(Image image, int finalResult)
    {
        if (image == null)
            yield break;

        float elapsed = 0f;
        float nextFaceTime = 0f;
        float duration = Mathf.Max(0.1f, diceAnimationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float rotationSpeed = Mathf.Lerp(1100f, 100f, progress);
            image.rectTransform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);

            if (elapsed >= nextFaceTime)
            {
                image.sprite = diceFaces[Random.Range(0, 6)];
                float faceInterval = Mathf.Lerp(0.06f, 0.24f, progress * progress);
                nextFaceTime = elapsed + faceInterval;
            }

            yield return null;
        }

        image.sprite = diceFaces[finalResult - 1];
        image.rectTransform.localRotation = Quaternion.identity;
    }

    public void EndTurn()
    {
        if (gameOver || currentPhase != TurnPhase.HeroTurn || isRolling ||
            (playerMovement != null && playerMovement.IsAnimatingMove))
            return;

        hasRolled = false;
        remainingMoves = 0;
        currentPhase = TurnPhase.MonsterTurn;

        RefreshMoveHighlights();
        UpdateButtons();
        UpdateUI();

        if (turnText != null)
            turnText.text = "MONSTER TURN";
        if (diceText != null)
            diceText.text = "Warrior turn ended.";

        if (onMonsterTurnStarted != null && onMonsterTurnStarted.GetPersistentEventCount() > 0)
        {
            onMonsterTurnStarted.Invoke();
        }
        else if (autoCompleteMonsterPhaseWhenNoListener)
        {
            if (monsterPhaseCoroutine != null)
                StopCoroutine(monsterPhaseCoroutine);
            monsterPhaseCoroutine = StartCoroutine(CompleteMonsterPhaseNextFrame());
        }
        else
        {
            Debug.Log("Monster phase is waiting. Call CompleteMonsterTurn() when existing monster logic finishes.");
        }
    }

    public void EndTurnAfterAttack()
    {
        if (gameOver || currentPhase != TurnPhase.HeroTurn || isRolling ||
            (playerMovement != null && playerMovement.IsAnimatingMove))
            return;

        EndTurn();
    }

    private IEnumerator CompleteMonsterPhaseNextFrame()
    {
        yield return null;
        CompleteMonsterTurn();
    }

    public void CompleteMonsterTurn()
    {
        if (gameOver || currentPhase != TurnPhase.MonsterTurn)
            return;

        currentPhase = TurnPhase.HeroTurn;
        hasRolled = false;
        remainingMoves = 0;

        SpawnPendingBonusItems();

        if (diceText != null)
            diceText.text = "Warrior turn - roll the dice.";

        UpdateUI();
        UpdateButtons();
        RefreshMoveHighlights();
    }

    private void EvaluateTileEffect(Tile tile)
    {
        if (tile == null)
            return;

        if (tile.isBadTile && !tile.isRevealed)
        {
            tile.RevealTile();
            collectedPoints = Mathf.Max(0, collectedPoints - 2);
            Debug.Log("Trap! Warrior loses 2 points.");
            tile.isBadTile = false;
            StartCoroutine(ReplaceBadTile(tile));
        }

        if (tile.hasItem)
        {
            tile.CollectItem();
            collectedPoints++;
            pendingBonusRespawns++;

            if (diceText != null)
                diceText.text = "Bonus collected! +1 point. Movement left: " + remainingMoves;

            Debug.Log("Warrior collected a point bonus. Total: " + collectedPoints);
        }

        if (tile == finishTile || tile.isFinishTile)
        {
            if (collectedPoints >= 10)
            {
                gameOver = true;
                currentPhase = TurnPhase.GameOver;
                if (winText != null)
                    winText.text = "WARRIOR WINS!";
                if (diceText != null)
                    diceText.text = "Victory!";
            }
            else
            {
                Debug.Log("Collect 10 points before reaching the exit.");
            }
        }

        if (gameOver && playerMovement != null)
            playerMovement.SetAllHighlightsOff();

        UpdateUI();
    }

    private void GenerateRandomItems()
    {
        foreach (Tile tile in allTiles)
        {
            if (tile == null)
                continue;

            tile.hasItem = false;
            tile.bonusType = BonusType.Points;
            tile.HideItem();
        }

        int spawned = 0;
        int attempts = 0;

        while (spawned < startingPointBonuses && attempts < 1000)
        {
            attempts++;
            Tile candidate = allTiles[Random.Range(0, allTiles.Length)];
            if (!CanSpawnOnTile(candidate))
                continue;

            candidate.hasItem = true;
            candidate.bonusType = BonusType.Points;
            candidate.ShowItem();
            spawned++;
        }

        Debug.Log("Point bonuses generated: " + spawned);
    }

    private void SpawnPendingBonusItems()
    {
        int count = pendingBonusRespawns;
        for (int i = 0; i < count; i++)
        {
            if (!SpawnRandomBonus())
                break;

            pendingBonusRespawns--;
        }
    }

    private bool SpawnRandomBonus()
    {
        List<Tile> available = new List<Tile>();

        foreach (Tile tile in allTiles)
        {
            if (CanSpawnOnTile(tile))
                available.Add(tile);
        }

        if (available.Count == 0)
            return false;

        Tile selected = available[Random.Range(0, available.Count)];
        selected.hasItem = true;
        selected.bonusType = BonusType.Points;
        selected.ShowItem();
        return true;
    }

    private bool CanSpawnOnTile(Tile tile)
    {
        if (tile == null || tile == finishTile || tile.isFinishTile ||
            tile == warriorStartTile || tile == player1Tile)
            return false;

        if (tile.isWall || tile.isBadTile || tile.isRevealed || tile.hasItem)
            return false;

        return true;
    }

    private void GenerateRandomBadTiles()
    {
        foreach (Tile tile in allTiles)
        {
            if (tile != null)
                tile.HideBadTile();
        }

        int spawned = 0;
        int attempts = 0;

        while (spawned < activeBadTilesCount && attempts < 1000)
        {
            attempts++;
            Tile candidate = allTiles[Random.Range(0, allTiles.Length)];
            if (!CanSpawnOnTile(candidate))
                continue;

            candidate.SetBadTileHidden();
            spawned++;
        }

        Debug.Log("Traps generated: " + spawned);
    }

    private void SpawnNewBadTile()
    {
        int attempts = 0;
        while (attempts < 1000)
        {
            attempts++;
            Tile candidate = allTiles[Random.Range(0, allTiles.Length)];
            if (!CanSpawnOnTile(candidate))
                continue;

            candidate.SetBadTileHidden();
            return;
        }

        Debug.LogWarning("Could not find an available tile for a new trap.");
    }

    private IEnumerator ReplaceBadTile(Tile oldTile)
    {
        yield return new WaitForSeconds(0.6f);

        if (oldTile != null)
            oldTile.HideBadTile();

        if (!gameOver)
            SpawnNewBadTile();
    }

    private void RefreshMoveHighlights()
    {
        if (playerMovement != null)
            playerMovement.RefreshMoveHighlights();
    }

    private void UpdateUI()
    {
        if (turnText != null)
        {
            switch (currentPhase)
            {
                case TurnPhase.HeroTurn:
                    turnText.text = "WARRIOR TURN";
                    break;
                case TurnPhase.MonsterTurn:
                    turnText.text = "MONSTER TURN";
                    break;
                case TurnPhase.GameOver:
                    turnText.text = "GAME OVER";
                    break;
            }
        }

        if (player1InventoryText != null)
            player1InventoryText.text = "WARRIOR\nPoints: " + collectedPoints + "/10";

        if (scoreP1Text != null)
            scoreP1Text.text = collectedPoints.ToString();

        UpdateTurnPanelColor();
        UpdateButtons();
        RefreshMoveHighlights();
    }

    private void UpdateButtons()
    {
        bool heroCanRoll = !gameOver &&
                           currentPhase == TurnPhase.HeroTurn &&
                           !hasRolled &&
                           !isRolling &&
                           (playerMovement == null || !playerMovement.IsAnimatingMove);

        bool heroCanEndTurn = !gameOver &&
                              currentPhase == TurnPhase.HeroTurn &&
                              !isRolling &&
                              (playerMovement == null || !playerMovement.IsAnimatingMove);

        if (rollButton != null)
            rollButton.interactable = heroCanRoll;

        if (endTurnButton != null)
            endTurnButton.interactable = heroCanEndTurn;
    }

    private void SetDiceImagesToDefault()
    {
        if (diceFaces == null || diceFaces.Length == 0 || diceFaces[0] == null)
            return;

        if (diceImage != null)
        {
            diceImage.sprite = diceFaces[0];
            diceImage.color = Color.white;
            diceImage.rectTransform.localRotation = Quaternion.identity;
        }

        if (diceImage2 != null)
        {
            diceImage2.sprite = diceFaces[0];
            diceImage2.color = Color.white;
            diceImage2.rectTransform.localRotation = Quaternion.identity;
        }
    }

    private void UpdateTurnPanelColor()
    {
        if (turnPanelImage == null)
            return;

        int phaseId = currentPhase == TurnPhase.HeroTurn ? 0 :
                      currentPhase == TurnPhase.MonsterTurn ? 1 : 2;

        if (lastPanelPhase == phaseId)
            return;

        lastPanelPhase = phaseId;
        Color target = currentPhase == TurnPhase.MonsterTurn ? monsterPanelColor : heroPanelColor;

        if (panelColorCoroutine != null)
            StopCoroutine(panelColorCoroutine);

        panelColorCoroutine = StartCoroutine(AnimatePanelColor(target));
    }

    private IEnumerator AnimatePanelColor(Color targetColor)
    {
        Color startColor = turnPanelImage.color;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, panelTransitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            turnPanelImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        turnPanelImage.color = targetColor;
        panelColorCoroutine = null;
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }
}
