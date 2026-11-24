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
        !warriorHasAttacked;

    private void Start()
    {
        currentTurn = CombatTurn.Warrior;
        warriorHasAttacked = false;

        MoveUnitToTile(warrior, warriorTile);
        MoveUnitToTile(skeleton, skeletonTile);

        Debug.Log("Combat started!");
        Debug.Log("WARRIOR TURN!");
    }

    // ==============================
    // WARRIOR ATTACK
    // ==============================

    public void WarriorAttack()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Start the game first!");
            return;
        }

        if (currentTurn != CombatTurn.Warrior)
        {
            Debug.LogWarning("It is not Warrior's turn!");
            return;
        }

        if (warriorHasAttacked)
        {
            Debug.LogWarning(
                "Warrior has already attacked this turn!"
            );

            return;
        }

        if (!TryAttack(warrior, skeleton))
        {
            return;
        }

        warriorHasAttacked = true;

        if (skeleton.IsDead)
        {
            currentTurn = CombatTurn.Finished;

            Debug.Log("WARRIOR WINS!");
            return;
        }

        Debug.Log("Warrior attack used! End your turn.");
    }

    // ==============================
    // END WARRIOR TURN
    // ==============================

    public void EndWarriorTurn()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Start the game first!");
            return;
        }

        if (currentTurn != CombatTurn.Warrior)
        {
            Debug.LogWarning("Cannot end Warrior turn!");
            return;
        }

        if (warrior == null || skeleton == null)
        {
            Debug.LogError("Combat participants are missing!");
            return;
        }

        currentTurn = CombatTurn.Skeleton;

        Debug.Log("SKELETON TURN!");

        PerformSkeletonTurn();

        if (warrior.IsDead)
        {
            currentTurn = CombatTurn.Finished;

            Debug.Log("SKELETON WINS!");
            return;
        }

        if (skeleton.IsDead)
        {
            currentTurn = CombatTurn.Finished;

            Debug.Log("WARRIOR WINS!");
            return;
        }

        currentTurn = CombatTurn.Warrior;
        warriorHasAttacked = false;

        Debug.Log("WARRIOR TURN!");
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
        {
            return;
        }

        // Attack if Warrior is already adjacent
        if (GetDistance(skeletonTile, warriorTile) == 1)
        {
            SkeletonAttack();
            return;
        }

        // Find the next tile towards Warrior
        Tile nextTile = FindNextSkeletonTile();

        if (nextTile != null)
        {
            MoveSkeleton(nextTile);
        }
        else
        {
            Debug.Log("Skeleton cannot find a path!");
        }

        // Attack after movement if possible
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

            Vector2Int position = new Vector2Int(tile.x, tile.y);

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

            // Find a tile adjacent to Warrior
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
        {
            Debug.LogWarning("It is not Skeleton's turn!");
            return;
        }

        TryAttack(skeleton, warrior);
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