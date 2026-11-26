using System.Collections.Generic;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    [Header("Combat Participants")]
    [SerializeField] private CombatUnit warrior;
    [SerializeField] private CombatUnit skeleton;

    [Header("Character Positions")]
    [SerializeField] private Tile warriorTile;
    [SerializeField] private Tile skeletonTile;

    [Header("Skeleton Settings")]
    [Min(1)]
    [SerializeField] private int skeletonMoveRange = 3;

    [Min(1)]
    [SerializeField] private int skeletonDetectionRange = 3;
    [Header("Respawn Settings")]
    [Min(1)]
    [SerializeField] private int skeletonRespawnDelay = 4;
    [Header("Level Objective")]
    [SerializeField] private GameObject artifact;
    [SerializeField] private Tile artifactTile;
    [SerializeField] private Tile exitTile;

    private bool hasArtifact = false;
    private bool hasWon = false;

    public bool HasArtifact => hasArtifact;
    public bool HasWon => hasWon;

    private int skeletonDeadTurns = 0;

    private SpriteRenderer skeletonRenderer;
    private Color skeletonOriginalColor;

    private enum CombatTurn
    {
        Warrior,
        Skeleton,
        Finished
    }

    private CombatTurn currentTurn = CombatTurn.Warrior;

    private bool warriorHasAttacked = false;

    public bool CanWarriorAttack =>
        currentTurn == CombatTurn.Warrior &&
        !warriorHasAttacked &&
        warrior != null &&
        skeleton != null &&
        !warrior.IsDead &&
        !skeleton.IsDead &&
        warriorTile != null &&
        skeletonTile != null &&
        GetDistance(warriorTile, skeletonTile) == 1;
    public bool IsCombatFinished =>
        currentTurn == CombatTurn.Finished;

    public bool CanWarriorMove =>
        currentTurn == CombatTurn.Warrior &&
        warrior != null &&
        !warrior.IsDead &&
        !warriorHasAttacked;

    private void Start()
    {
        currentTurn = CombatTurn.Warrior;
        warriorHasAttacked = false;
        skeletonDeadTurns = 0;

        if (skeleton != null)
        {
            skeletonRenderer =
                skeleton.GetComponent<SpriteRenderer>();

            if (skeletonRenderer != null)
            {
                skeletonOriginalColor = skeletonRenderer.color;
            }
        }

        MoveUnitToTile(warrior, warriorTile);
        MoveUnitToTile(skeleton, skeletonTile);

        Debug.Log("Combat system ready!");
        Debug.Log("WARRIOR TURN!");
        CheckArtifactPickup();

        TryStartCombatAutomatically();

        CheckLevelVictory();
    }

    // ==============================
    // WARRIOR ATTACK
    // ==============================

    public void WarriorAttack()
    {
        if (!Application.isPlaying)
            return;

        if (!CanWarriorAttack)
        {
            Debug.LogWarning("Warrior cannot attack now!");
            return;
        }

        ResolveCombatRound();
    }

    // ==============================
    // END WARRIOR TURN
    // ==============================

    public void EndWarriorTurn()
    {
        if (!Application.isPlaying)
            return;

        if (currentTurn != CombatTurn.Warrior)
            return;

        if (warrior == null || skeleton == null)
            return;

        currentTurn = CombatTurn.Skeleton;

        Debug.Log("SKELETON TURN!");

        if (!warriorHasAttacked && !skeleton.IsDead)
        {
            PerformSkeletonTurn();
        }

        if (currentTurn == CombatTurn.Finished)
            return;

        if (warrior.IsDead)
        {
            currentTurn = CombatTurn.Finished;

            Debug.Log("GAME OVER!");
            return;
        }

        currentTurn = CombatTurn.Warrior;
        warriorHasAttacked = false;

        Debug.Log("WARRIOR TURN!");

        UpdateSkeletonRespawn();
    }

    // ==============================
    // SKELETON AI
    // ==============================

    private void PerformSkeletonTurn()
    {
        if (warriorTile == null || skeletonTile == null)
        {
            Debug.LogWarning("Combat tiles are not assigned!");
            return;
        }

        if (skeleton.IsDead || warrior.IsDead)
            return;
        if (!CanSkeletonSeeWarrior())
        {
            Debug.Log("Skeleton cannot see Warrior. Staying idle.");
            return;
        }

        Debug.Log("Skeleton detected Warrior!");

        // Attack immediately if adjacent
        if (GetDistance(skeletonTile, warriorTile) == 1)
        {
            SkeletonAttack();
            return;
        }

        int stepsMoved = 0;

        // Move up to the maximum movement range
        for (int i = 0; i < skeletonMoveRange; i++)
        {
            if (GetDistance(skeletonTile, warriorTile) == 1)
                break;

            Tile nextTile = FindNextSkeletonTile();

            if (nextTile == null)
            {
                Debug.Log("Skeleton cannot find a path!");
                break;
            }
            MoveSkeleton(nextTile);

            stepsMoved++;
        }

        Debug.Log($"Skeleton moved {stepsMoved} tile(s).");

        // Attack after movement if adjacent
        if (GetDistance(skeletonTile, warriorTile) == 1)
        {
            SkeletonAttack();
        }
        else
        {
            Debug.Log("Skeleton is still too far to attack.");
        }
    }

    // ==============================
    // SKELETON MOVEMENT - BFS
    // ==============================
    private bool CanSkeletonSeeWarrior()
{
    if (skeletonTile == null || warriorTile == null)
        return false;

    int dx = Mathf.Abs(warriorTile.x - skeletonTile.x);
    int dy = Mathf.Abs(warriorTile.y - skeletonTile.y);

    if (Mathf.Max(dx, dy) > skeletonDetectionRange)
        return false;

    return HasLineOfSight(skeletonTile, warriorTile);
}

private bool HasLineOfSight(Tile from, Tile to)
{
    Tile[] allTiles = FindObjectsByType<Tile>(
        FindObjectsSortMode.None
    );

    Dictionary<Vector2Int, Tile> tileMap =
        new Dictionary<Vector2Int, Tile>();

    foreach (Tile tile in allTiles)
    {
        if (tile.gameObject.scene != gameObject.scene)
            continue;

        Vector2Int position = new Vector2Int(tile.x, tile.y);

        if (tileMap.ContainsKey(position))
            return false;

        tileMap.Add(position, tile);
    }

    int x = from.x;
    int y = from.y;

    int dx = Mathf.Abs(to.x - from.x);
    int dy = Mathf.Abs(to.y - from.y);

    int stepX = to.x > from.x ? 1 : -1;
    int stepY = to.y > from.y ? 1 : -1;

    int ix = 0;
    int iy = 0;

    while (ix < dx || iy < dy)
    {
        int horizontal = (1 + 2 * ix) * dy;
        int vertical = (1 + 2 * iy) * dx;

        if (horizontal == vertical)
        {
            if (IsVisionBlocked(tileMap, x + stepX, y) ||
                IsVisionBlocked(tileMap, x, y + stepY))
            {
                return false;
            }

            x += stepX;
            y += stepY;
            ix++;
            iy++;
        }
        else if (horizontal < vertical)
        {
            x += stepX;
            ix++;
        }
        else
        {
            y += stepY;
            iy++;
        }

        if (IsVisionBlocked(tileMap, x, y))
            return false;
    }

    return true;
}

private bool IsVisionBlocked(
    Dictionary<Vector2Int, Tile> tileMap,
    int x,
    int y)
{
    if (!tileMap.TryGetValue(
        new Vector2Int(x, y), out Tile tile))
    {
        return true;
    }

    return tile.isWall;
}

    private Tile FindNextSkeletonTile()
    {
        Tile[] allTiles = FindObjectsByType<Tile>(
            FindObjectsSortMode.None
        );

        Dictionary<Vector2Int, Tile> tileMap =
            new Dictionary<Vector2Int, Tile>();

        foreach (Tile tile in allTiles)
        {
            if (tile.gameObject.scene != gameObject.scene)
                continue;

            Vector2Int position =
                new Vector2Int(tile.x, tile.y);

            if (tileMap.ContainsKey(position))
            {
                Debug.LogWarning(
                    $"Duplicate tile coordinates: {position}"
                );

                return null;
            }

            tileMap.Add(position, tile);
        }

        Queue<Tile> queue = new Queue<Tile>();

        HashSet<Tile> visited = new HashSet<Tile>();

        Dictionary<Tile, Tile> previous =
            new Dictionary<Tile, Tile>();

        queue.Enqueue(skeletonTile);
        visited.Add(skeletonTile);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        while (queue.Count > 0)
        {
            Tile currentTile = queue.Dequeue();

            if (GetDistance(currentTile, warriorTile) == 1)
            {
                Tile step = currentTile;

                while (previous.ContainsKey(step) &&
                       previous[step] != skeletonTile)
                {
                    step = previous[step];
                }

                return step;
            }

            foreach (Vector2Int direction in directions)
            {
                Vector2Int nextPosition = new Vector2Int(
                    currentTile.x + direction.x,
                    currentTile.y + direction.y
                );

                if (!tileMap.TryGetValue(
                    nextPosition, out Tile neighbor))
                {
                    continue;
                }

                if (visited.Contains(neighbor))
                    continue;

                if (neighbor.isWall)
                    continue;

                if (neighbor == warriorTile)
                    continue;

                visited.Add(neighbor);
                previous[neighbor] = currentTile;

                queue.Enqueue(neighbor);
            }
        }

        return null;
    }

    private void MoveSkeleton(Tile targetTile)
    {
        if (targetTile == null ||
            targetTile.isWall ||
            targetTile == warriorTile)
        {
            return;
        }

        skeletonTile = targetTile;

        MoveUnitToTile(skeleton, skeletonTile);

        Debug.Log(
            $"Skeleton moved to tile ({targetTile.x}, {targetTile.y})"
        );
    }

    private void MoveUnitToTile(CombatUnit unit, Tile tile)
    {
        if (unit == null || tile == null)
            return;

        Vector3 position = tile.transform.position;

        position.z = unit.transform.position.z;

        unit.transform.position = position;
    }

    private int GetDistance(Tile first, Tile second)
    {
        return Mathf.Abs(first.x - second.x) +
               Mathf.Abs(first.y - second.y);
    }

    // ==============================
    // SKELETON ATTACK
    // ==============================

    public void SkeletonAttack()
{
    if (!Application.isPlaying ||
        currentTurn != CombatTurn.Skeleton)
        return;

    ResolveCombatRound();
}
    private void TryStartCombatAutomatically()
    {
        if (currentTurn == CombatTurn.Finished)
            return;

        if (warrior == null || skeleton == null)
            return;

        if (warrior.IsDead || skeleton.IsDead)
            return;

        if (warriorTile == null || skeletonTile == null)
            return;

        if (warriorHasAttacked)
            return;

        if (GetDistance(warriorTile, skeletonTile) == 1)
        {
            Debug.Log("COMBAT STARTED AUTOMATICALLY!");

            ResolveCombatRound();
        }
    }
    private void CheckLevelVictory()
    {
        if (currentTurn == CombatTurn.Finished)
            return;

        if (warrior == null || warrior.IsDead)
            return;

        if (warriorTile == null || exitTile == null)
            return;

        if (warriorTile != exitTile)
            return;

        if (!hasArtifact)
        {
            Debug.Log("EXIT LOCKED! Find the artifact first.");
            return;
        }

        hasWon = true;
        currentTurn = CombatTurn.Finished;

        Debug.Log("ARTIFACT DELIVERED!");
        Debug.Log("VICTORY! LEVEL COMPLETED!");
    }
    private void CheckArtifactPickup()
    {
        if (hasArtifact)
            return;

        if (warrior == null || warrior.IsDead)
            return;

        if (warriorTile == null || artifactTile == null)
            return;

        if (warriorTile == artifactTile)
        {
            hasArtifact = true;

            if (artifact != null)
            {
                artifact.SetActive(false);
            }

            Debug.Log("ARTIFACT COLLECTED!");
            Debug.Log("Find the exit!");
        }
    }
    public void NotifyWarriorMoved(Tile newTile)
    {
        if (!CanWarriorMove)
        {
            Debug.Log("Warrior movement is blocked!");
            return;
        }

        if (newTile == null || newTile.isWall)
            return;

        if (newTile == skeletonTile &&
            skeleton != null &&
            !skeleton.IsDead)
        {
            return;
        }

        warriorTile = newTile;

        CheckArtifactPickup();

        TryStartCombatAutomatically();

        CheckLevelVictory();
    }
    private void HandleSkeletonDeath()
    {
        skeletonDeadTurns = 0;

        if (skeletonRenderer != null)
        {
            skeletonRenderer.color = Color.gray;
        }

        Debug.Log("Skeleton defeated!");
        Debug.Log("Skeleton will respawn in 4 turns.");
    }

    private void UpdateSkeletonRespawn()
    {
        if (skeleton == null || !skeleton.IsDead)
            return;

        skeletonDeadTurns++;

        Debug.Log(
            $"Skeleton respawn: {skeletonDeadTurns}/{skeletonRespawnDelay}"
        );

        if (skeletonDeadTurns == skeletonRespawnDelay - 1)
        {
            if (skeletonRenderer != null)
            {
                skeletonRenderer.color =
                    new Color(0.6f, 0.1f, 1f);
            }

            Debug.Log("Skeleton eyes are glowing purple!");
        }

        if (skeletonDeadTurns >= skeletonRespawnDelay)
        {
            if (warriorTile == skeletonTile)
            {
                Debug.Log("Respawn delayed: tile is occupied.");
                return;
            }

            skeleton.Respawn();

            skeletonDeadTurns = 0;

            if (skeletonRenderer != null)
            {
                skeletonRenderer.color = skeletonOriginalColor;
            }

            TryStartCombatAutomatically();
        }
    }

private void ResolveCombatRound()
{
    if (warrior == null || skeleton == null)
        return;

    if (warriorTile == null || skeletonTile == null)
        return;

    if (warrior.IsDead || skeleton.IsDead)
        return;

    if (warriorHasAttacked)
        return;

    if (GetDistance(warriorTile, skeletonTile) != 1)
    {
        Debug.Log("Characters are not adjacent!");
        return;
    }

    warriorHasAttacked = true;

    Debug.Log("COMBAT ROUND STARTED!");

    // Equal initiative
    if (warrior.agility == skeleton.agility)
    {
        Debug.Log("SIMULTANEOUS ATTACK!");

        int warriorDamage = CalculateDamage(
            warrior, skeleton
        );

        int skeletonDamage = CalculateDamage(
            skeleton, warrior
        );

        skeleton.TakeDamage(warriorDamage);
        warrior.TakeDamage(skeletonDamage);
    }

    // Warrior attacks first
    else if (warrior.agility > skeleton.agility)
    {
        Debug.Log("Warrior has initiative!");

        skeleton.TakeDamage(
            CalculateDamage(warrior, skeleton)
        );

        if (!skeleton.IsDead)
        {
            warrior.TakeDamage(
                CalculateDamage(skeleton, warrior)
            );
        }
    }

    // Skeleton attacks first
    else
    {
        Debug.Log("Skeleton has initiative!");

        warrior.TakeDamage(
            CalculateDamage(skeleton, warrior)
        );

        if (!warrior.IsDead)
        {
            skeleton.TakeDamage(
                CalculateDamage(warrior, skeleton)
            );
        }
    }

    // Combat result
    if (warrior.IsDead && skeleton.IsDead)
    {
        currentTurn = CombatTurn.Finished;
        Debug.Log("DRAW! Both characters defeated!");
    }
    else if (skeleton.IsDead)
    {
        HandleSkeletonDeath();
    }
    else if (warrior.IsDead)
    {
        currentTurn = CombatTurn.Finished;
        Debug.Log("SKELETON WINS!");
    }
    else
    {
        Debug.Log("Combat round finished!");
    }
}

private int CalculateDamage(
    CombatUnit attacker,
    CombatUnit defender)
{
    int totalRoll = 0;

    for (int i = 0; i < attacker.strength; i++)
    {
        int diceRoll = Random.Range(1, 7);

        totalRoll += diceRoll;

        Debug.Log(
            $"{attacker.unitName} rolled D6: {diceRoll}"
        );
    }

    int damage = Mathf.Max(
        0,
        totalRoll - defender.shield
    );

    Debug.Log(
        $"{attacker.unitName} deals {damage} damage!"
    );

    return damage;
}

    // ==============================
    // ATTACK CALCULATION
    // ==============================

    private bool TryAttack(
        CombatUnit attacker,
        CombatUnit defender)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogError("Combat participants are missing!");
            return false;
        }

        if (attacker.IsDead || defender.IsDead)
        {
            Debug.LogWarning(
                "A defeated character cannot fight!"
            );

            return false;
        }

        Tile attackerTile;
        Tile defenderTile;

        if (attacker == warrior && defender == skeleton)
        {
            attackerTile = warriorTile;
            defenderTile = skeletonTile;
        }
        else if (attacker == skeleton && defender == warrior)
        {
            attackerTile = skeletonTile;
            defenderTile = warriorTile;
        }
        else
        {
            Debug.LogError("Unknown combat participants!");
            return false;
        }

        if (attackerTile == null || defenderTile == null)
        {
            Debug.LogWarning("Combat tiles are not assigned!");
            return false;
        }

        if (GetDistance(attackerTile, defenderTile) != 1)
        {
            Debug.LogWarning(
                $"{attacker.unitName} cannot attack! Target is not adjacent."
            );

            return false;
        }

        int totalRoll = 0;

        for (int i = 0; i < attacker.strength; i++)
        {
            int diceRoll = Random.Range(1, 7);

            totalRoll += diceRoll;

            Debug.Log(
                $"{attacker.unitName} rolled D6: {diceRoll}"
            );
        }

        int damage = Mathf.Max(
            0,
            totalRoll - defender.shield
        );

        Debug.Log(
            $"{attacker.unitName} attacks {defender.unitName}!"
        );

        Debug.Log($"Total roll: {totalRoll}");
        Debug.Log($"Damage after shield: {damage}");

        defender.TakeDamage(damage);

        return true;
    }

    // ==============================
    // TEST COMMANDS
    // ==============================

    [ContextMenu("Test Warrior Attack")]
    private void TestWarriorAttack()
    {
        WarriorAttack();
    }

    [ContextMenu("Test End Warrior Turn")]
    private void TestEndWarriorTurn()
    {
        EndWarriorTurn();
    }
}