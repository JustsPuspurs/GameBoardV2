using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    [Header("Combat Participants")]
    [SerializeField] private CombatUnit warrior;
    [SerializeField] private CombatUnit skeleton;

    public void WarriorAttack()
    {
        Attack(warrior, skeleton);
    }

    [ContextMenu("Test Warrior Attack")]
    private void TestWarriorAttack()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Start the game before testing combat.");
            return;
        }

        WarriorAttack();
    }

    public void Attack(CombatUnit attacker, CombatUnit defender)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogError("Combat participants are missing!");
            return;
        }

        if (attacker.IsDead || defender.IsDead)
        {
            Debug.LogWarning("A defeated character cannot participate in combat.");
            return;
        }

        int totalRoll = 0;

        for (int i = 0; i < attacker.strength; i++)
        {
            int diceRoll = Random.Range(1, 7);

            totalRoll += diceRoll;

            Debug.Log($"{attacker.unitName} rolled D6: {diceRoll}");
        }

        int damage = Mathf.Max(0, totalRoll - defender.shield);

        Debug.Log($"{attacker.unitName} attacks {defender.unitName}!");
        Debug.Log($"Total roll: {totalRoll}");
        Debug.Log($"Damage after shield: {damage}");

        defender.TakeDamage(damage);
    }
    public void SkeletonAttack()
    {
        Attack(skeleton, warrior);
    }

    [ContextMenu("Test Skeleton Attack")]
    private void TestSkeletonAttack()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Start the game before testing combat.");
            return;
        }

        SkeletonAttack();
    }
}