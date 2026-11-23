using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    [Header("Combat Participants")]
    [SerializeField] private CombatUnit warrior;
    [SerializeField] private CombatUnit skeleton;

    [Header("Character Positions")]
    [SerializeField] private Tile warriorTile;
    [SerializeField] private Tile skeletonTile;

    // Combat turns
    private enum CombatTurn
    {
        Warrior,
        Skeleton,
        Finished
    }

    private CombatTurn currentTurn = CombatTurn.Warrior;

    private bool warriorHasAttacked = false;

    private void Start()
    {
        currentTurn = CombatTurn.Warrior;
        warriorHasAttacked = false;

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

        // Attack only counts if successful
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

        Debug.Log(
            "Warrior attack used! End your turn."
        );
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

        SkeletonAttack();

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

        // Start next Warrior turn
        currentTurn = CombatTurn.Warrior;
        warriorHasAttacked = false;

        Debug.Log("WARRIOR TURN!");
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

        // Check melee distance
        int distance =
            Mathf.Abs(attackerTile.x - defenderTile.x) +
            Mathf.Abs(attackerTile.y - defenderTile.y);

        if (distance != 1)
        {
            Debug.LogWarning(
                $"{attacker.unitName} cannot attack! Target is not adjacent."
            );

            return false;
        }

        // Roll D6 for each Strength point
        int totalRoll = 0;

        for (int i = 0; i < attacker.strength; i++)
        {
            int diceRoll = Random.Range(1, 7);

            totalRoll += diceRoll;

            Debug.Log(
                $"{attacker.unitName} rolled D6: {diceRoll}"
            );
        }

        // Calculate damage
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