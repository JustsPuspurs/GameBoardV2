using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    [SerializeField] private CombatUnit warrior;
    [SerializeField] private CombatUnit skeleton;
    [SerializeField] private CombatUnit secondSkeleton;
    [SerializeField] private Tile warriorTile;
    [SerializeField] private Tile skeletonTile;
    [SerializeField] private Tile secondSkeletonTile;
    [SerializeField, Min(1)] private int skeletonMoveRange = 3;
    [SerializeField, Min(1)] private int skeletonDetectionRange = 3;
    [SerializeField, Min(1)] private int skeletonRespawnDelay = 4;
    [SerializeField] private GameObject artifact;
    [SerializeField] private Tile artifactTile;
    [SerializeField] private Tile exitTile;

    private const float FightDuration = 3f;
    private static readonly Vector2Int[] Directions =
        { Vector2Int.up, Vector2Int.left, Vector2Int.down, Vector2Int.right };

    private sealed class Enemy
    {
        public readonly CombatUnit Unit;
        public readonly SpriteRenderer Renderer;
        public readonly Color OriginalColor;
        public Tile Tile;
        public int RespawnOnTurn;

        public Enemy(CombatUnit unit, Tile tile)
        {
            Unit = unit;
            Tile = tile;
            Renderer = unit.GetComponent<SpriteRenderer>();
            OriginalColor = Renderer != null ? Renderer.color : Color.white;
            Unit.transform.position = tile.transform.position;
        }
    }

    private readonly List<Enemy> enemies = new();
    private readonly Dictionary<Vector2Int, Tile> board = new();
    private DungeonHud dungeonHud;
    private Enemy pendingEnemy;
    private Enemy displayedEnemy;
    private Enemy foughtDuringWarriorTurn;
    private bool resolving;
    private bool runningEnemyTurn;
    private bool warriorHasFought;

    public int TurnNumber { get; private set; } = 1;
    public bool HasArtifact { get; private set; }
    public bool HasWon { get; private set; }
    public bool IsAtLockedExit => warriorTile == exitTile && !HasArtifact;
    public CombatUnit Warrior => warrior;
    public CombatUnit Opponent => displayedEnemy?.Unit;
    public bool IsMonsterTurn => runningEnemyTurn;
    public bool IsCombatFinished => HasWon || warrior.IsDead;
    public bool HasPendingFight => pendingEnemy != null && !IsCombatFinished;
    public bool IsRollingFight => resolving;
    public bool CanWarriorAttack => HasPendingFight && !resolving;
    public bool CanEndWarriorTurn => !IsCombatFinished && !runningEnemyTurn && !HasPendingFight;
    public bool CanWarriorMove => CanEndWarriorTurn && !warriorHasFought;

    private void Start()
    {
        foreach (Tile tile in FindObjectsByType<Tile>(FindObjectsSortMode.None))
        {
            if (tile.gameObject.scene == gameObject.scene)
                board.Add(Cell(tile), tile);
        }

        if (skeleton != null && skeletonTile != null)
            enemies.Add(new Enemy(skeleton, skeletonTile));
        if (secondSkeleton != null && secondSkeletonTile != null)
            enemies.Add(new Enemy(secondSkeleton, secondSkeletonTile));

        warrior.transform.position = warriorTile.transform.position;
        CheckWarriorTile();
    }

    public void AttachHud(DungeonHud hud) => dungeonHud = hud;

    public bool IsOccupiedByOpponent(Tile tile)
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy.Tile == tile && !enemy.Unit.IsDead)
                return true;
        }
        return false;
    }

    public void NotifyWarriorMoved(Tile newTile)
    {
        if (!CanWarriorMove || newTile == null || newTile.isWall || IsOccupiedByOpponent(newTile))
            return;

        warriorTile = newTile;
        CheckWarriorTile();
    }

    private void CheckWarriorTile()
    {
        if (warriorTile == artifactTile && !HasArtifact)
        {
            HasArtifact = true;
            if (artifact != null)
                artifact.SetActive(false);
        }

        foreach (Enemy enemy in enemies)
        {
            if (!enemy.Unit.IsDead && Distance(enemy.Tile, warriorTile) == 1)
            {
                BeginFight(enemy);
                return;
            }
        }
        CheckVictory();
    }

    private void CheckVictory()
    {
        if (!warrior.IsDead && warriorTile == exitTile && HasArtifact)
            HasWon = true;
    }

    private void BeginFight(Enemy enemy)
    {
        pendingEnemy = enemy;
        displayedEnemy = enemy;
        dungeonHud?.BeginCombatRound();
    }

    public void WarriorAttack()
    {
        if (CanWarriorAttack)
            StartCoroutine(ResolveFight(pendingEnemy));
    }

    private IEnumerator ResolveFight(Enemy enemy)
    {
        resolving = true;
        int[] warriorRoll = Roll(warrior.Strength, out int warriorTotal);
        int[] enemyRoll = Roll(enemy.Unit.Strength, out int enemyTotal);
        if (dungeonHud != null)
            yield return dungeonHud.AnimateFight(warriorRoll, enemyRoll, FightDuration);
        else
            yield return new WaitForSeconds(FightDuration);

        int damageToEnemy = Mathf.Max(0, warriorTotal - enemy.Unit.Armor);
        int damageToWarrior = Mathf.Max(0, enemyTotal - warrior.Armor);
        if (warrior.Agility >= enemy.Unit.Agility)
            enemy.Unit.TakeDamage(damageToEnemy);
        if (warrior.Agility <= enemy.Unit.Agility || !enemy.Unit.IsDead)
            warrior.TakeDamage(damageToWarrior);
        if (warrior.Agility < enemy.Unit.Agility && !warrior.IsDead)
            enemy.Unit.TakeDamage(damageToEnemy);

        if (enemy.Unit.IsDead)
        {
            enemy.RespawnOnTurn = TurnNumber + skeletonRespawnDelay;
            if (enemy.Renderer != null)
                enemy.Renderer.color = Color.gray;
        }

        if (!runningEnemyTurn)
        {
            foughtDuringWarriorTurn = enemy;
            warriorHasFought = true;
            CheckVictory();
        }

        pendingEnemy = null;
        resolving = false;
    }

    private static int[] Roll(int count, out int total)
    {
        var results = new int[count];
        total = 0;
        for (int i = 0; i < count; i++)
        {
            results[i] = Random.Range(1, 7);
            total += results[i];
        }
        return results;
    }

    public void EndWarriorTurn()
    {
        if (CanEndWarriorTurn)
            StartCoroutine(RunEnemyTurn());
    }

    private IEnumerator RunEnemyTurn()
    {
        runningEnemyTurn = true;
        yield return null;
        foreach (Enemy enemy in enemies)
        {
            if (IsCombatFinished)
                break;
            if (enemy.Unit.IsDead || enemy == foughtDuringWarriorTurn)
                continue;

            if (CanSeeWarrior(enemy.Tile))
            {
                for (int i = 0; i < skeletonMoveRange && Distance(enemy.Tile, warriorTile) > 1; i++)
                {
                    Tile next = FindNextTile(enemy.Tile);
                    if (next == null)
                        break;

                    enemy.Tile = next;
                    enemy.Unit.transform.position = next.transform.position;
                    yield return new WaitForSeconds(0.12f);
                }
            }

            if (Distance(enemy.Tile, warriorTile) == 1)
            {
                BeginFight(enemy);
                while (pendingEnemy != null && !IsCombatFinished)
                    yield return null;
            }
        }

        if (!IsCombatFinished)
        {
            TurnNumber++;
            UpdateRespawns();
            warriorHasFought = false;
            foughtDuringWarriorTurn = null;
        }
        runningEnemyTurn = false;
    }

    private void UpdateRespawns()
    {
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.Unit.IsDead)
                continue;

            if (TurnNumber + 1 == enemy.RespawnOnTurn && enemy.Renderer != null)
                enemy.Renderer.color = new Color(0.6f, 0.1f, 1f);

            if (TurnNumber < enemy.RespawnOnTurn || enemy.Tile == warriorTile || IsOccupiedByOpponent(enemy.Tile))
                continue;

            enemy.Unit.Respawn();
            if (enemy.Renderer != null)
                enemy.Renderer.color = enemy.OriginalColor;
        }
    }

    private bool CanSeeWarrior(Tile tile)
    {
        Vector2Int start = Cell(tile);
        Vector2Int delta = Cell(warriorTile) - start;
        int dx = Mathf.Abs(delta.x);
        int dy = Mathf.Abs(delta.y);
        int distance = Mathf.Max(dx, dy);
        if (distance > skeletonDetectionRange || (dx != 0 && dy != 0 && dx != dy))
            return false;

        var step = new Vector2Int(delta.x.CompareTo(0), delta.y.CompareTo(0));
        for (int i = 0; i < distance; i++)
        {
            if (step.x != 0 && step.y != 0 &&
                (BlocksVision(start + new Vector2Int(step.x, 0)) || BlocksVision(start + new Vector2Int(0, step.y))))
                return false;

            start += step;
            if (BlocksVision(start))
                return false;
        }
        return true;
    }

    private bool BlocksVision(Vector2Int cell) => !board.TryGetValue(cell, out Tile tile) || tile.isWall;

    private Tile FindNextTile(Tile start)
    {
        var queue = new Queue<Tile>();
        var firstStep = new Dictionary<Tile, Tile> { [start] = null };
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Tile current = queue.Dequeue();
            if (Distance(current, warriorTile) == 1)
                return firstStep[current];

            foreach (Vector2Int direction in Directions)
            {
                if (!board.TryGetValue(Cell(current) + direction, out Tile next) ||
                    next == warriorTile || next.isWall || firstStep.ContainsKey(next) || IsOccupiedByOpponent(next))
                    continue;

                firstStep[next] = current == start ? next : firstStep[current];
                queue.Enqueue(next);
            }
        }
        return null;
    }

    private static Vector2Int Cell(Tile tile) => new(tile.x, tile.y);

    private static int Distance(Tile a, Tile b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
}
