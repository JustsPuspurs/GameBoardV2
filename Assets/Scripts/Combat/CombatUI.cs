using UnityEngine;
using TMPro;

public class CombatUI : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private CombatUnit warrior;
    [SerializeField] private CombatUnit skeleton;

    [Header("Health UI")]
    [SerializeField] private TMP_Text warriorHPText;
    [SerializeField] private TMP_Text skeletonHPText;

    private int lastWarriorHP = -1;
    private int lastSkeletonHP = -1;

    private void Start()
    {
        if (warrior == null || skeleton == null ||
        warriorHPText == null || skeletonHPText == null)
        {
            Debug.LogError("CombatUI: Missing references!");
            enabled = false;
            return;
        }

        UpdateHealthUI();
    }

    private void Update()
    {
        if (warrior.CurrentHealth != lastWarriorHP ||
            skeleton.CurrentHealth != lastSkeletonHP)
        {
            UpdateHealthUI();
        }
    }

    private void UpdateHealthUI()
    {
        lastWarriorHP = warrior.CurrentHealth;
        lastSkeletonHP = skeleton.CurrentHealth;

        warriorHPText.text =
            $"WARRIOR HP: {lastWarriorHP}/{warrior.maxHealth}";

        skeletonHPText.text =
            $"SKELETON HP: {lastSkeletonHP}/{skeleton.maxHealth}";
    }
}
