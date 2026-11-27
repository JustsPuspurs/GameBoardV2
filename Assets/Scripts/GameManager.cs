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

    [Tooltip("Legacy Player 2 references are no longer used by DR-02. Leave empty or ignore them.")]
    public Transform player2;
    public Tile player2Tile;

    public Tile finishTile;

    [Header("Warrior Movement")]
    [Tooltip("Each Tile has its own Movement Cost (1 or 2), set on the Tile component in the Inspector.")]
    [Min(1)] public int agility = 2;
    [Min(0.05f)] public float moveDuration = 0.3f;
    public Color availableMoveColor = new Color(0.2f, 0.6f, 1f, 1f);

    [Header("UI")]
    public TMP_Text diceText;
    public TMP_Text turnText;
    public TMP_Text player1InventoryText;
    public TMP_Text winText;
    public TMP_Text scoreP1Text;

    [Header("Dice Animation")]
    public Image diceImage;
    [Tooltip("Optional second dice image. If not assigned, the first image still animates and the text displays every die result.")]
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
    [Tooltip("Optional event for an existing monster-turn system. It is not a monster AI script.")]
    public UnityEvent onMonsterTurnStarted;
    [Tooltip("If no monster-turn listener is connected, automatically starts the next Warrior turn after a short delay.")]
    public bool autoCompleteMonsterPhaseWhenNoListener = true;

    [Header("Bonus and Trap Generation")]
    [Min(0)] public int startingPointBonuses = 4;
    [Min(0)] public int activeBadTilesCount = 2;

    [Header("Other Characters / Occupied Tiles")]
    [Tooltip("Optional transforms that occupy tiles and block Warrior movement, such as monsters already present in the scene.")]
    public Transform[] otherCharacters;

    [Header("Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    [SerializeField] private CombatSystem dungeonCombat;

    private DungeonHud dungeonHud;
    private int MovementAgility => dungeonCombat != null ? dungeonCombat.Warrior.Agility : Mathf.Max(1, agility);

    private TurnPhase currentPhase = TurnPhase.HeroTurn;
    private int remainingMoves;
    private int collectedPoints;
    private int pendingBonusRespawns;
    private bool hasRolled;
    private bool isRolling;
    private bool isAnimatingMove;
    private bool gameOver;

    private Tile[] allTiles;
    private Tile warriorStartTile;
    private float gridOffset = 1f;
    private Coroutine panelColorCoroutine;
    private Coroutine monsterPhaseCoroutine;
    private int lastPanelPhase = -1;

    public int RemainingMoves => remainingMoves;
    public TurnPhase CurrentPhase => currentPhase;
    public bool HasRolled => hasRolled;
    public bool IsBusy => isRolling || isAnimatingMove;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one GameManager exists in the scene. The newest one will be used.");
        }
        Instance = this;
    }

    private void Start()
    {
        if (winText != null) winText.text = string.Empty;

        allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.InstanceID);
        if (allTiles == null || allTiles.Length == 0)
        {
            Debug.LogError("GameManager: No Tile objects found in the scene.");
            enabled = false;
            return;
        }

        if (player1 == null)
            Debug.LogError("GameManager: Assign the Warrior to the Player 1 field (the old Player 1 object). ");

        if (player1Tile == null)
            Debug.LogError("GameManager: Assign the Warrior's starting tile to Player 1 Tile.");

        warriorStartTile = player1Tile;

        if (dungeonCombat == null)
        {
            FindFinishTile();
            CalculateGridCoordinates();
        }

        if (player1 != null && player1Tile != null)
            player1.position = player1Tile.transform.position;

        // Player 2 is deliberately not positioned or used. It is a legacy PvP reference.
        if (dungeonCombat == null)
        {
            GenerateRandomBadTiles();
            GenerateRandomItems();
        }
        else
        {
            ConfigureDungeonHud();
        }

        currentPhase = TurnPhase.HeroTurn;
        hasRolled = false;
        remainingMoves = 0;
        UpdateUI();
        SetDiceImagesToDefault();
        UpdateButtons();

        if (diceText != null)
            diceText.text = "Warrior turn - roll the dice.";
    }

    private void ConfigureDungeonHud()
    {
        dungeonHud = GetComponent<DungeonHud>();
        if (dungeonHud == null)
            return;

        CombatUnit warrior = dungeonCombat.Warrior;
        dungeonCombat.AttachHud(dungeonHud);
        dungeonHud.Configure(rollButton, diceText, diceFaces,
            warrior.GetComponent<SpriteRenderer>().sprite, warrior.unitName, EndTurn, dungeonCombat.WarriorAttack);
        UpdateDungeonHud();
    }

    private void Update()
    {
        if (dungeonCombat == null)
            return;

        if (dungeonCombat.IsCombatFinished && currentPhase != TurnPhase.GameOver)
        {
            currentPhase = TurnPhase.GameOver;
            UpdateUI();
        }
        else if (currentPhase == TurnPhase.MonsterTurn && !dungeonCombat.IsMonsterTurn)
        {
            CompleteMonsterTurn();
        }

        UpdateButtons();
        if (dungeonHud != null)
            UpdateDungeonHud();
    }

    private void UpdateDungeonHud()
    {
        CombatUnit warrior = dungeonCombat.Warrior;
        dungeonHud.SetHero(warrior.CurrentHealth, warrior.maxHealth, warrior.Agility, warrior.Armor, warrior.Strength);
        dungeonHud.SetEnemy(dungeonCombat.Opponent, dungeonCombat.HasPendingFight,
            dungeonCombat.IsRollingFight);
        dungeonHud.SetTurn(dungeonCombat.TurnNumber,
            endTurnButton != null && endTurnButton.interactable && hasRolled && !HasAvailableMove(),
            warrior.IsDead);
    }

    private void FindFinishTile()
    {
        if (finishTile != null)
        {
            finishTile.isFinishTile = true;
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

    private void CalculateGridCoordinates()
    {
        if (allTiles.Length > 1)
        {
            float minDistance = float.MaxValue;
            Vector3 firstPosition = allTiles[0].transform.position;

            for (int i = 1; i < allTiles.Length; i++)
            {
                if (allTiles[i] == null) continue;
                float distance = Vector3.Distance(firstPosition, allTiles[i].transform.position);
                if (distance > 0.1f && distance < minDistance)
                    minDistance = distance;
            }

            if (minDistance < float.MaxValue)
                gridOffset = minDistance;
        }

        foreach (Tile tile in allTiles)
        {
            if (tile == null) continue;
            tile.x = Mathf.RoundToInt(tile.transform.position.x / gridOffset);
            tile.y = Mathf.RoundToInt(tile.transform.position.y / gridOffset);
        }
    }

    // -------------------- Tile clicking and movement --------------------

    public void OnTileClicked(Tile tile)
    {
        if (tile == null) return;
        TryMoveToTile(tile);
    }

    public void TryMoveToTile(Tile targetTile)
    {
        if (!CanAttemptMovement()) return;

        if (!IsValidMove(player1Tile, targetTile, out string reason))
        {
            if (!string.IsNullOrEmpty(reason) && diceText != null)
                diceText.text = reason;
            return;
        }

        // Lock movement immediately. A second click cannot start another coroutine.
        isAnimatingMove = true;
        UpdateButtons();
        RefreshMoveHighlights();
        StartCoroutine(SmoothMoveWarrior(targetTile, targetTile.movementCost));
    }

    private bool CanAttemptMovement()
    {
        return !gameOver &&
               currentPhase == TurnPhase.HeroTurn &&
               hasRolled &&
               !isRolling &&
               !isAnimatingMove &&
               remainingMoves > 0 &&
               (dungeonCombat == null || dungeonCombat.CanWarriorMove) &&
               player1 != null &&
               player1Tile != null;
    }

    // This is the single source of truth used by both click validation and highlights.
    private bool HasAvailableMove()
    {
        if (!CanAttemptMovement())
            return false;

        foreach (Tile tile in allTiles)
        {
            if (IsValidMove(player1Tile, tile, out _))
                return true;
        }
        return false;
    }

    private bool IsValidMove(Tile fromTile, Tile targetTile, out string reason)
    {
        reason = string.Empty;

        if (fromTile == null || targetTile == null || targetTile == fromTile)
        {
            reason = "Choose an adjacent tile.";
            return false;
        }

        int manhattanDistance = Mathf.Abs(targetTile.x - fromTile.x) + Mathf.Abs(targetTile.y - fromTile.y);
        if (manhattanDistance != 1)
        {
            reason = "Only four-directional adjacent moves are allowed.";
            return false;
        }

        if (targetTile.isWall)
        {
            reason = "Blocked: this tile is a wall.";
            return false;
        }

        if (targetTile.movementCost < 1 || targetTile.movementCost > 2)
        {
            reason = "Invalid tile movement cost. Set it to 1 or 2.";
            return false;
        }

        if (targetTile.movementCost > remainingMoves)
        {
            reason = "Not enough movement points.";
            return false;
        }

        if (IsOccupiedByOtherCharacter(targetTile))
        {
            reason = "Blocked: another character occupies this tile.";
            return false;
        }

        return true;
    }

    private bool IsOccupiedByOtherCharacter(Tile tile)
    {
        if (dungeonCombat != null)
            return dungeonCombat.IsOccupiedByOpponent(tile);
        if (tile == null || otherCharacters == null) return false;

        foreach (Transform character in otherCharacters)
        {
            if (character == null || character == player1) continue;
            if (Vector3.Distance(character.position, tile.transform.position) < 0.05f)
                return true;
        }

        return false;
    }

    private IEnumerator SmoothMoveWarrior(Tile targetTile, int movementCost)
    {
        if (player1 == null || targetTile == null)
        {
            isAnimatingMove = false;
            UpdateButtons();
            RefreshMoveHighlights();
            yield break;
        }

        Vector3 startPosition = player1.position;
        Vector3 targetPosition = targetTile.transform.position;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, moveDuration);

        while (elapsed < duration)
        {
            if (player1 == null || targetTile == null)
            {
                isAnimatingMove = false;
                UpdateButtons();
                RefreshMoveHighlights();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            player1.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        player1.position = targetPosition;
        player1Tile = targetTile;

        // Spend points only after the move has completed successfully.
        remainingMoves = Mathf.Max(0, remainingMoves - movementCost);
        if (dungeonCombat == null)
        {
            EvaluateTileEffect(targetTile);
            if (diceText != null && !gameOver)
                diceText.text = "Movement points left: " + remainingMoves;
        }
        else
        {
            ApplyDungeonMove(targetTile);
        }

        isAnimatingMove = false;

        UpdateUI();
        UpdateButtons();
        RefreshMoveHighlights();
    }

    private void ApplyDungeonMove(Tile targetTile)
    {
        bool hadArtifact = dungeonCombat.HasArtifact;
        dungeonCombat.NotifyWarriorMoved(targetTile);
        if (!dungeonCombat.CanWarriorMove)
            remainingMoves = 0;

        if (diceText == null)
            return;

        if (dungeonCombat.IsCombatFinished)
            diceText.text = dungeonCombat.HasWon ? "Artifact delivered." : "Restart to try again.";
        else if (dungeonCombat.IsAtLockedExit)
            diceText.text = "Exit locked. Find the artifact.";
        else if (!hadArtifact && dungeonCombat.HasArtifact)
            diceText.text = "Artifact collected. Find the exit.";
        else if (dungeonCombat.HasPendingFight)
            diceText.text = "Enemy nearby. Choose Fight.";
        else if (!dungeonCombat.CanWarriorMove)
            diceText.text = "Combat ended movement for this turn.";
        else
            diceText.text = "Movement points left: " + remainingMoves;
    }

    // -------------------- Dice: Agility x D6 --------------------

    public void RollDice()
    {
        if (gameOver || currentPhase != TurnPhase.HeroTurn || hasRolled || isRolling || isAnimatingMove ||
            (dungeonCombat != null && !dungeonCombat.CanWarriorMove))
            return;

        dungeonHud?.BeginCombatRound();
        hasRolled = true; // Set immediately to prevent repeated button presses.
        isRolling = true;
        remainingMoves = 0;
        UpdateButtons();
        RefreshMoveHighlights();

        if (diceText != null)
            diceText.text = "Rolling " + MovementAgility + "D6...";

        StartCoroutine(AnimateDiceRoll());
    }

    private IEnumerator AnimateDiceRoll()
    {
        int diceCount = MovementAgility;
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

        if (dungeonHud != null)
        {
            yield return dungeonHud.AnimateMovement(results, diceAnimationDuration);
        }
        else if (hasValidFaces && diceImage != null)
        {
            // Animate the first die. If a second image is assigned, animate the second die at the same time.
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
        if (image == null) yield break;

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

    // -------------------- End turn and monster phase hook --------------------

    public void EndTurn()
    {
        if (gameOver || currentPhase != TurnPhase.HeroTurn || isRolling || isAnimatingMove ||
            (dungeonCombat != null && !dungeonCombat.CanEndWarriorTurn))
            return;

        // End Turn is allowed even if the Warrior has no legal move or unused points remain.
        hasRolled = false;
        remainingMoves = 0;
        currentPhase = TurnPhase.MonsterTurn;
        RefreshMoveHighlights();
        UpdateButtons();
        UpdateUI();

        if (turnText != null)
            turnText.text = "Monster turn";
        if (diceText != null)
            diceText.text = "Warrior turn ended.";

        if (dungeonCombat != null)
        {
            dungeonCombat.EndWarriorTurn();
        }
        else if (onMonsterTurnStarted != null && onMonsterTurnStarted.GetPersistentEventCount() > 0)
        {
            onMonsterTurnStarted.Invoke();
        }
        else
        {
            // DR-02 does not create monster AI. This hook allows an existing monster system to be connected later.
            if (autoCompleteMonsterPhaseWhenNoListener)
            {
                if (monsterPhaseCoroutine != null)
                    StopCoroutine(monsterPhaseCoroutine);
                monsterPhaseCoroutine = StartCoroutine(CompleteMonsterPhaseNextFrame());
            }
            else
            {
                Debug.Log("Monster phase is waiting. Call CompleteMonsterTurn() when the existing monster logic finishes.");
            }
        }
    }

    // Call this from the existing DR-01 combat script after a successful melee attack.
    public void EndTurnAfterAttack()
    {
        if (gameOver || currentPhase != TurnPhase.HeroTurn || isRolling || isAnimatingMove)
            return;

        EndTurn();
    }

    private IEnumerator CompleteMonsterPhaseNextFrame()
    {
        yield return null;
        CompleteMonsterTurn();
    }

    // Call this from an existing monster-turn controller when its turn has finished.
    public void CompleteMonsterTurn()
    {
        if (gameOver || currentPhase != TurnPhase.MonsterTurn)
            return;

        currentPhase = TurnPhase.HeroTurn;
        hasRolled = false;
        remainingMoves = 0;
        if (dungeonCombat == null)
            SpawnPendingBonusItems();

        if (diceText != null)
            diceText.text = "Warrior turn - roll the dice.";

        UpdateUI();
        UpdateButtons();
        RefreshMoveHighlights();
    }

    // -------------------- Tile effects, bonuses and traps --------------------

    private void EvaluateTileEffect(Tile tile)
    {
        if (tile == null) return;

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
                if (winText != null) winText.text = "WARRIOR WINS!";
                if (diceText != null) diceText.text = "Victory!";
            }
            else
            {
                Debug.Log("Collect 10 points before reaching the exit.");
            }
        }

        if (gameOver)
        {
            SetAllHighlightsOff();
            UpdateButtons();
        }

        UpdateUI();
    }

    private void GenerateRandomItems()
    {
        foreach (Tile tile in allTiles)
        {
            if (tile == null) continue;
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
            if (!CanSpawnOnTile(candidate)) continue;

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
            if (!SpawnRandomBonus()) break;
            pendingBonusRespawns--;
        }
    }

    private bool SpawnRandomBonus()
    {
        List<Tile> available = new List<Tile>();
        foreach (Tile tile in allTiles)
        {
            if (CanSpawnOnTile(tile)) available.Add(tile);
        }

        if (available.Count == 0) return false;

        Tile selected = available[Random.Range(0, available.Count)];
        selected.hasItem = true;
        selected.bonusType = BonusType.Points;
        selected.ShowItem();
        return true;
    }

    private bool CanSpawnOnTile(Tile tile)
    {
        if (tile == null || tile == finishTile || tile.isFinishTile || tile == warriorStartTile)
            return false;
        if (tile == player1Tile || tile.isWall || tile.isBadTile || tile.isRevealed || tile.hasItem)
            return false;
        return true;
    }

    private void GenerateRandomBadTiles()
    {
        foreach (Tile tile in allTiles)
        {
            if (tile != null) tile.HideBadTile();
        }

        int spawned = 0;
        int attempts = 0;
        while (spawned < activeBadTilesCount && attempts < 1000)
        {
            attempts++;
            Tile candidate = allTiles[Random.Range(0, allTiles.Length)];
            if (!CanSpawnOnTile(candidate)) continue;
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
            if (!CanSpawnOnTile(candidate)) continue;
            candidate.SetBadTileHidden();
            return;
        }
        Debug.LogWarning("Could not find an available tile for a new trap.");
    }

    private IEnumerator ReplaceBadTile(Tile oldTile)
    {
        yield return new WaitForSeconds(0.6f);
        if (oldTile != null) oldTile.HideBadTile();
        if (!gameOver) SpawnNewBadTile();
    }

    // -------------------- UI, highlights and scene controls --------------------

    private void RefreshMoveHighlights()
    {
        if (allTiles == null) return;

        foreach (Tile tile in allTiles)
        {
            if (tile != null)
                tile.SetMoveHighlight(false, Color.white);
        }

        if (!CanAttemptMovement()) return;

        foreach (Tile tile in allTiles)
        {
            if (tile == null) continue;
            if (IsValidMove(player1Tile, tile, out _))
                tile.SetMoveHighlight(true, availableMoveColor);
        }
    }

    private void SetAllHighlightsOff()
    {
        if (allTiles == null) return;
        foreach (Tile tile in allTiles)
        {
            if (tile != null) tile.SetMoveHighlight(false, Color.white);
        }
    }

    private void UpdateUI()
    {
        if (turnText != null)
        {
            switch (currentPhase)
            {
                case TurnPhase.HeroTurn: turnText.text = "WARRIOR TURN"; break;
                case TurnPhase.MonsterTurn: turnText.text = "MONSTER TURN"; break;
                case TurnPhase.GameOver: turnText.text = "GAME OVER"; break;
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
        bool heroCanRoll = !gameOver && currentPhase == TurnPhase.HeroTurn && !hasRolled && !isRolling && !isAnimatingMove;
        bool heroCanEndTurn = !gameOver && currentPhase == TurnPhase.HeroTurn && !isRolling && !isAnimatingMove;

        if (dungeonCombat != null)
        {
            heroCanRoll &= dungeonCombat.CanWarriorMove;
            heroCanEndTurn &= dungeonCombat.CanEndWarriorTurn;
        }

        if (rollButton != null) rollButton.interactable = heroCanRoll;
        if (endTurnButton != null) endTurnButton.interactable = heroCanEndTurn;
    }

    private void SetDiceImagesToDefault()
    {
        if (diceFaces == null || diceFaces.Length == 0 || diceFaces[0] == null) return;
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
        if (turnPanelImage == null) return;
        int phaseId = currentPhase == TurnPhase.HeroTurn ? 0 : currentPhase == TurnPhase.MonsterTurn ? 1 : 2;
        if (lastPanelPhase == phaseId) return;
        lastPanelPhase = phaseId;

        Color target = currentPhase == TurnPhase.MonsterTurn ? monsterPanelColor : heroPanelColor;
        if (panelColorCoroutine != null) StopCoroutine(panelColorCoroutine);
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
