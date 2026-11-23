using System.Collections;
using UnityEngine;

/// <summary>
/// Handles only the Warrior's tile movement. GameManager remains responsible
/// for turn state and the number of movement points.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform warrior;
    [SerializeField] private Tile currentTile;
    [SerializeField] private GameManager gameManager;

    [Header("Movement")]
    [Min(0.05f)] public float moveDuration = 0.3f;
    public Color availableMoveColor = new Color(0.2f, 0.6f, 1f, 1f);

    [Header("Other Characters / Occupied Tiles")]
    [Tooltip("Optional transforms that block movement, such as monsters.")]
    public Transform[] otherCharacters;

    private Tile[] allTiles;
    private bool isAnimatingMove;

    public Tile CurrentTile => currentTile;
    public bool IsAnimatingMove => isAnimatingMove;

    private void Awake()
    {
        if (warrior == null)
            warrior = transform;

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }

    public void Initialize(GameManager manager, Transform warriorTransform, Tile startTile, Tile[] tiles)
    {
        gameManager = manager;
        if (warriorTransform != null)
            warrior = warriorTransform;
        currentTile = startTile;
        allTiles = tiles;

        if (warrior != null && currentTile != null)
            warrior.position = currentTile.transform.position;

        RefreshMoveHighlights();
    }

    public void SetCurrentTile(Tile tile)
    {
        currentTile = tile;
        if (warrior != null && tile != null)
            warrior.position = tile.transform.position;
        RefreshMoveHighlights();
    }

    public void OnTileClicked(Tile tile)
    {
        TryMoveToTile(tile);
    }

    public void TryMoveToTile(Tile targetTile)
    {
        if (gameManager == null || !gameManager.CanWarriorAttemptMovement || isAnimatingMove)
            return;

        if (!IsValidMove(currentTile, targetTile, out string reason))
        {
            if (!string.IsNullOrEmpty(reason))
                gameManager.SetStatusText(reason);
            return;
        }

        isAnimatingMove = true;
        RefreshMoveHighlights();
        StartCoroutine(SmoothMoveWarrior(targetTile, targetTile.movementCost));
    }

    private bool IsValidMove(Tile fromTile, Tile targetTile, out string reason)
    {
        reason = string.Empty;

        if (fromTile == null || targetTile == null || targetTile == fromTile)
        {
            reason = "Choose an adjacent tile.";
            return false;
        }

        int manhattanDistance = Mathf.Abs(targetTile.x - fromTile.x) +
                                Mathf.Abs(targetTile.y - fromTile.y);

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

        if (targetTile.movementCost > gameManager.RemainingMoves)
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
        if (tile == null || otherCharacters == null)
            return false;

        foreach (Transform character in otherCharacters)
        {
            if (character == null || character == warrior)
                continue;

            if (Vector3.Distance(character.position, tile.transform.position) < 0.05f)
                return true;
        }

        return false;
    }

    private IEnumerator SmoothMoveWarrior(Tile targetTile, int movementCost)
    {
        if (warrior == null || targetTile == null || gameManager == null)
        {
            isAnimatingMove = false;
            RefreshMoveHighlights();
            yield break;
        }

        Vector3 startPosition = warrior.position;
        Vector3 targetPosition = targetTile.transform.position;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, moveDuration);

        while (elapsed < duration)
        {
            if (warrior == null || targetTile == null)
            {
                isAnimatingMove = false;
                RefreshMoveHighlights();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            warrior.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        warrior.position = targetPosition;
        currentTile = targetTile;

        // Spend movement only after the animation successfully reaches the target.
        gameManager.SpendMovementPoints(movementCost);
        gameManager.OnWarriorMovedToTile(targetTile);

        isAnimatingMove = false;
        RefreshMoveHighlights();
    }

    public void RefreshMoveHighlights()
    {
        if (allTiles == null)
            return;

        foreach (Tile tile in allTiles)
        {
            if (tile != null)
                tile.SetMoveHighlight(false, Color.white);
        }

        if (gameManager == null || !gameManager.CanWarriorAttemptMovement || isAnimatingMove)
            return;

        foreach (Tile tile in allTiles)
        {
            if (tile == null)
                continue;

            if (IsValidMove(currentTile, tile, out _))
                tile.SetMoveHighlight(true, availableMoveColor);
        }
    }

    public void SetAllHighlightsOff()
    {
        if (allTiles == null)
            return;

        foreach (Tile tile in allTiles)
        {
            if (tile != null)
                tile.SetMoveHighlight(false, Color.white);
        }
    }
}
