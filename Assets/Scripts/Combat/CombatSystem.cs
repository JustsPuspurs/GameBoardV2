using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    [Header("Combat Participants")]
    [SerializeField] private CombatUnit warrior;
    [SerializeField] private CombatUnit skeleton;

    [Header("Character Positions")]
    [SerializeField] private Tile warriorTile;
    [SerializeField] private Tile skeletonTile;

    // ==============================
    // WARRIOR ATTACK
    // ==============================

    public void WarriorAttack()
    {
        Attack(warrior, skeleton);
    }

    [ContextMenu("Test Warrior Attack")]
    private void TestWarriorAttack()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Start the game first!");
            return;
        }

        WarriorAttack();
    }

    // ==============================
    // SKELETON ATTACK
    // ==============================

    public void SkeletonAttack()
    {
        Attack(skeleton, warrior);
    }

    [ContextMenu("Test Skeleton Attack")]
    private void TestSkeletonAttack()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Start the game first!");
            return;
        }

        SkeletonAttack();
    }

    // ==============================
    // COMBAT SYSTEM
    // ==============================

    public void Attack(CombatUnit attacker, CombatUnit defender)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogError("Combat participants are missing!");
            return;
        }

        if (attacker.IsDead || defender.IsDead)
        {
            Debug.LogWarning("A defeated character cannot fight!");
            return;
        }

        // Identify attacker and defender positions

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
            return;
        }

        // Check positions

        if (attackerTile == null || defenderTile == null)
        {
            Debug.LogWarning("Combat tiles are not assigned!");
            return;
        }

        // Calculate distance between characters

        int distance =
            Mathf.Abs(attackerTile.x - defenderTile.x) +
            Mathf.Abs(attackerTile.y - defenderTile.y);

        // Melee attack requires an adjacent tile

        if (distance != 1)
        {
            Debug.LogWarning(
                $"{attacker.unitName} cannot attack! Target is not adjacent."
            );

            return;
        }

        // ==============================
        // DICE ROLL
        // ==============================

        int totalRoll = 0;

        for (int i = 0; i < attacker.strength; i++)
        {
            int diceRoll = Random.Range(1, 7);

            totalRoll += diceRoll;

            Debug.Log($"{attacker.unitName} rolled D6: {diceRoll}");
        }

        // ==============================
        // DAMAGE CALCULATION
        // ==============================

        int damage = Mathf.Max(0, totalRoll - defender.shield);

        Debug.Log($"{attacker.unitName} attacks {defender.unitName}!");

        Debug.Log($"Total roll: {totalRoll}");

        Debug.Log($"Damage after shield: {damage}");

        defender.TakeDamage(damage);
    }
}