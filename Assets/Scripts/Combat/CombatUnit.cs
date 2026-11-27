using UnityEngine;

public class CombatUnit : MonoBehaviour
{
    [Header("Character Information")]
    public string unitName = "Unit";

    [Header("Combat Stats")]
    [Min(1)] public int maxHealth = 20;
    [Range(0, DungeonHud.MaxStat)] public int strength = 3;
    [Range(0, DungeonHud.MaxStat)] public int shield = 2;
    [Range(1, DungeonHud.MaxStat)] public int agility = 2;

    [Header("Current Status")]
    [SerializeField] private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int Agility => Mathf.Clamp(agility, 1, DungeonHud.MaxStat);
    public int Strength => Mathf.Clamp(strength, 0, DungeonHud.MaxStat);
    public int Armor => Mathf.Clamp(shield, 0, DungeonHud.MaxStat);
    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        damage = Mathf.Max(0, damage);

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"{unitName} takes {damage} damage!");
        Debug.Log($"{unitName} HP: {currentHealth}/{maxHealth}");

        if (IsDead)
        {
            Die();
        }
    }
    public void Respawn()
    {
        currentHealth = maxHealth;

        Debug.Log($"{unitName} has respawned!");
        Debug.Log($"{unitName} HP: {currentHealth}/{maxHealth}");
    }

    private void Die()
    {
        Debug.Log($"{unitName} has been defeated!");
    }
}
