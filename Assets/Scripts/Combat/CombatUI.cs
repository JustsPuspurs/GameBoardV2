using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUI : MonoBehaviour
{
    [Header("Combat System")]
    [SerializeField] private CombatSystem combatSystem;
    [Header("Characters")]
    [SerializeField] private CombatUnit warrior;
    [SerializeField] private CombatUnit skeleton;

    [Header("Health UI")]
    [SerializeField] private TMP_Text warriorHPText;
    [SerializeField] private TMP_Text skeletonHPText;

    [Header("Combat Result UI")]
    [SerializeField] private TMP_Text combatResultText;

    [Header("Combat Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button endTurnButton;

    [Header("Result Colors")]
    [SerializeField] private Color victoryColor = Color.yellow;
    [SerializeField] private Color defeatColor = Color.red;

    private int lastWarriorHP = -1;
    private int lastSkeletonHP = -1;

    private bool combatFinished = false;

    private void Start()
    {
        if (combatSystem == null ||
            warrior == null ||
            skeleton == null ||
            warriorHPText == null ||
            skeletonHPText == null ||
            combatResultText == null ||
            attackButton == null ||
            endTurnButton == null)
        {
            Debug.LogError("CombatUI: Missing references!");
            enabled = false;
            return;
        }

        combatFinished = false;

        combatResultText.gameObject.SetActive(false);

        attackButton.interactable = true;
        endTurnButton.interactable = true;

        UpdateHealthUI();
        CheckCombatResult();
    }

    private void Update()
    {
        if (warrior.CurrentHealth != lastWarriorHP ||
            skeleton.CurrentHealth != lastSkeletonHP)
        {
            UpdateHealthUI();
        }

        if (!combatFinished)
        {
            CheckCombatResult();
        }

        if (!combatFinished)
        {
            attackButton.interactable =
                combatSystem.CanWarriorAttack;
        }
    }

    // ==============================
    // HEALTH UI
    // ==============================

    private void UpdateHealthUI()
    {
        lastWarriorHP = warrior.CurrentHealth;
        lastSkeletonHP = skeleton.CurrentHealth;

        warriorHPText.text =
            $"WARRIOR HP: {lastWarriorHP}/{warrior.maxHealth}";

        skeletonHPText.text =
            $"SKELETON HP: {lastSkeletonHP}/{skeleton.maxHealth}";
    }

    // ==============================
    // COMBAT RESULT
    // ==============================

    private void CheckCombatResult()
    {
        if (warrior.IsDead)
        {
            ShowCombatResult("DEFEAT!", defeatColor);
        }
        else if (skeleton.IsDead)
        {
            ShowCombatResult("VICTORY!", victoryColor);
        }
    }

    private void ShowCombatResult(string result, Color color)
    {
        combatFinished = true;

        combatResultText.text = result;
        combatResultText.color = color;

        combatResultText.gameObject.SetActive(true);

        attackButton.interactable = false;
        endTurnButton.interactable = false;
    }
}
