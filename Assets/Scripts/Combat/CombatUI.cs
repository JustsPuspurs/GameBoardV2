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
    [SerializeField] private Color respawnColor = Color.magenta;

    private int lastWarriorHP = -1;
    private int lastSkeletonHP = -1;

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

        combatResultText.gameObject.SetActive(false);

        UpdateHealthUI();
        UpdateCombatStatus();
        UpdateButtons();
    }

    private void Update()
    {
        if (warrior.CurrentHealth != lastWarriorHP ||
            skeleton.CurrentHealth != lastSkeletonHP)
        {
            UpdateHealthUI();
        }

        UpdateCombatStatus();
        UpdateButtons();
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
    // COMBAT STATUS
    // ==============================

    private void UpdateCombatStatus()
    {
        if (combatSystem.HasWon)
        {
            ShowStatus("VICTORY!", victoryColor);
        }
        else if (warrior.IsDead && skeleton.IsDead)
        {
            ShowStatus("DRAW!", Color.white);
        }
        else if (warrior.IsDead)
        {
            ShowStatus("DEFEAT!", defeatColor);
        }
        else if (skeleton.IsDead)
        {
            ShowStatus(
                "SKELETON RESPAWNING...",
                respawnColor
            );
        }
        else
        {
            combatResultText.gameObject.SetActive(false);
        }
    }

    // ==============================
    // BUTTONS
    // ==============================

    private void UpdateButtons()
    {
        bool gameFinished =
            combatSystem.IsCombatFinished || warrior.IsDead;

        attackButton.interactable =
            !gameFinished &&
            !skeleton.IsDead &&
            combatSystem.CanWarriorAttack;

        endTurnButton.interactable = !gameFinished;
    }

    // ==============================
    // STATUS MESSAGE
    // ==============================

    private void ShowStatus(string message, Color color)
    {
        combatResultText.text = message;
        combatResultText.color = color;

        combatResultText.gameObject.SetActive(true);
    }
}
