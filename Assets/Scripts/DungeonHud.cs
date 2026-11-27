using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class DungeonHud : MonoBehaviour
{
    public const int MaxStat = 6;

    [SerializeField] private string playerName = "Player";
    [SerializeField] private AudioClip diceSound;

    private static readonly Color32 Panel = new(39, 40, 46, 255);
    private static readonly Color32 Well = new(28, 29, 34, 255);
    private static readonly Color32 Edge = new(110, 101, 93, 255);
    private static readonly Color32 Ink = new(242, 234, 221, 255);
    private static readonly Color32 Muted = new(195, 187, 179, 255);
    private static readonly Color32 Gold = new(235, 190, 117, 255);
    private static readonly Color32 Health = new(112, 188, 153, 255);
    private static readonly Color32 Enemy = new(223, 142, 133, 255);
    private static readonly Vector2 TopLeft = new(0, 1);
    private static readonly Vector2 BottomRight = new(1, 0);

    private readonly (Button source, Button proxy)[] actions = new (Button, Button)[4];
    private readonly Image[] movementDice = new Image[MaxStat];
    private Image[] heroDice;
    private Image[] enemyDice;
    private Font font;
    private Sprite[] faces;
    private Canvas canvas;
    private TMP_Text legacyStatus;
    private TMP_Text legacyResult;
    private Button originalRoll;
    private Button roll;
    private Text movementStatus;
    private Text diceCount;
    private Text encounterResult;
    private Text heroStats;
    private Text heroHealth;
    private Text enemyStats;
    private Text enemyHealth;
    private Text heroRollText;
    private Text enemyRollText;
    private Image heroHealthFill;
    private Image enemyHealthFill;
    private RectTransform movementTray;
    private RectTransform heroTray;
    private RectTransform enemyTray;
    private RectTransform encounter;
    private AudioSource audioSource;
    private int movementCount;
    private int[] movementResults;
    private int[] heroResults;
    private int[] enemyResults;
    private bool rolling;
    private bool rollWasReady;

    public void Configure(Button legacyRoll, TMP_Text status, Sprite[] diceFaces, Sprite heroPortrait,
        string heroClass, Sprite enemyPortrait = null, string enemyName = null, Action endTurn = null)
    {
        if (legacyRoll == null || status == null)
            return;

        originalRoll = legacyRoll;
        legacyStatus = status;
        faces = diceFaces;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas oldCanvas = originalRoll.GetComponentInParent<Canvas>();
        Button[] originalButtons = oldCanvas.GetComponentsInChildren<Button>(true);
        foreach (TMP_Text originalText in oldCanvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (originalText.name == "CombatResultText")
                legacyResult = originalText;
        }

        canvas = new GameObject("Dungeon HUD", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366, 768);
        scaler.matchWidthOrHeight = 1;
        RectTransform root = (RectTransform)canvas.transform;

        RectTransform heroCard = Frame(root, "Player card", TopLeft, 26, -34, 300, 205);
        CharacterCard(heroCard, playerName, heroClass, heroPortrait, Health,
            out heroHealth, out heroHealthFill, out heroStats);

        if (!string.IsNullOrEmpty(enemyName))
        {
            encounter = Frame(root, "Encounter card", TopLeft, 26, -249, 300, 350);
            CharacterCard(encounter, enemyName, "Opponent", enemyPortrait, Enemy,
                out enemyHealth, out enemyHealthFill, out enemyStats);
            (heroTray, heroDice, heroRollText) = CombatDice(encounter, "Your attack", 212);
            (enemyTray, enemyDice, enemyRollText) = CombatDice(encounter, "Enemy attack", 280);
            encounter.gameObject.SetActive(false);
        }

        RectTransform move = Frame(root, "Movement dice", Vector2.zero, 26, 12, 500, 96);
        Label(move, "MOVEMENT", 17, 5, 16, 170, 22, Gold, FontStyle.Bold);
        diceCount = Label(move, "", 325, 5, 16, 157, 22, Muted, align: TextAnchor.MiddleRight);
        movementTray = Frame(move, "Movement tray", TopLeft, 16, -30, 330, 43, true);
        CreateDice(movementTray, movementDice);
        roll = ActionButton(move, "Roll dice", 362, 30, 121, 43, Gold,
            () => originalRoll.onClick.Invoke());
        movementStatus = Label(move, "", 17, 74, 14, 466, 21, Muted);

        RectTransform controls = Frame(root, "Controls", BottomRight, -403, 24, 377, 76);
        int controlIndex = 0;
        foreach (string label in new[] { "Attack", "End turn", "Restart", "Menu" })
        {
            Button source = Array.Find(originalButtons, button => MatchesAction(button.name, label));
            if (source == null)
                continue;

            Button proxy = ActionButton(controls, label, 10 + controlIndex * 90, 16, 86, 44,
                label == "Attack" ? Enemy : Ink, () =>
                {
                    if (label == "End turn" && endTurn != null)
                        endTurn();
                    else
                        source.onClick.Invoke();
                });
            actions[controlIndex++] = (source, proxy);
        }

        encounterResult = Label(root, "", -250, 0, 21, 500, 40, Gold,
            FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.25f));
        AudioSource existingSound = originalRoll.GetComponent<AudioSource>();
        if (existingSound == null && diceSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.65f;
        }

        oldCanvas.enabled = false;
        if (oldCanvas.TryGetComponent(out GraphicRaycaster oldRaycaster))
            oldRaycaster.enabled = false;
    }

    public void SetHero(int health, int maxHealth, int agility, int armor, int strength)
    {
        if (heroStats == null)
            return;

        SetCharacter(heroHealth, heroHealthFill, heroStats, health, maxHealth, agility, armor, strength);
        int count = Mathf.Clamp(agility, 1, MaxStat);
        if (movementCount == count || rolling)
            return;

        movementCount = count;
        movementResults = null;
        diceCount.text = count + "D6";
        DisplayDice(movementTray, movementDice, movementCount, null);
    }

    public void SetEnemy(bool visible, int health, int maxHealth, int agility, int armor, int strength)
    {
        if (encounter == null)
            return;

        encounter.gameObject.SetActive(visible);
        if (visible)
            SetCharacter(enemyHealth, enemyHealthFill, enemyStats, health, maxHealth, agility, armor, strength);
    }

    public bool HasCombatRolls => heroResults != null || enemyResults != null;

    public void BeginCombatRound()
    {
        heroResults = null;
        enemyResults = null;
        DisplayCombatRolls();
    }

    public void RecordCombatRoll(bool hero, int[] results)
    {
        if (hero)
            heroResults = results;
        else
            enemyResults = results;
        DisplayCombatRolls();
    }

    public IEnumerator AnimateMovement(int[] results, float duration)
    {
        if (movementTray == null)
            yield break;

        rolling = true;
        movementCount = results.Length;
        movementResults = null;
        diceCount.text = results.Length + "D6";
        DisplayDice(movementTray, movementDice, movementCount, null);
        if (audioSource != null)
            audioSource.PlayOneShot(diceSound);

        duration = Mathf.Max(0.1f, duration);
        float nextFaceTime = 0f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / duration;
            bool refreshFaces = elapsed >= nextFaceTime;
            for (int i = 0; i < results.Length; i++)
            {
                Image die = movementDice[i];
                float angle = Mathf.Lerp(720f + i * 60f, 0f, progress * progress);
                die.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
                die.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.78f, 1f, progress * progress);
                if (refreshFaces)
                    die.sprite = Face(UnityEngine.Random.Range(1, 7));
            }
            if (refreshFaces)
                nextFaceTime = elapsed + Mathf.Lerp(0.06f, 0.24f, progress * progress);

            yield return null;
        }

        movementResults = results;
        DisplayDice(movementTray, movementDice, movementCount, movementResults);
        rolling = false;
    }

    private void LateUpdate()
    {
        if (roll == null)
            return;

        bool rollReady = originalRoll.interactable && !rolling;
        if (rollReady && !rollWasReady && movementResults != null)
        {
            movementResults = null;
            DisplayDice(movementTray, movementDice, movementCount, null);
        }

        rollWasReady = rollReady;
        roll.interactable = rollReady;
        foreach (var action in actions)
        {
            if (action.proxy != null)
                action.proxy.interactable = action.source.interactable;
        }

        movementStatus.text = legacyStatus.text;
        encounterResult.text = legacyResult != null && legacyResult.gameObject.activeSelf
            ? legacyResult.text : "";
    }

    private void DisplayCombatRolls()
    {
        if (encounter == null)
            return;

        ShowCombatDice(heroTray, heroDice, heroRollText, heroResults);
        ShowCombatDice(enemyTray, enemyDice, enemyRollText, enemyResults);
    }

    private void ShowCombatDice(RectTransform tray, Image[] dice, Text label, int[] results)
    {
        DisplayDice(tray, dice, results?.Length ?? 0, results);
        if (results == null)
        {
            label.text = "";
            return;
        }

        int sum = 0;
        foreach (int result in results)
            sum += result;
        label.text = "= " + sum;
    }

    private void DisplayDice(RectTransform tray, Image[] dice, int count, int[] results)
    {
        const int gap = 7;
        float width = tray.sizeDelta.x - 22;
        // Leave room for a rotating square to reach its full diagonal.
        float maxSize = tray == movementTray ? 38 : 29;
        float size = Mathf.Min(maxSize, (width - gap * (count - 1)) / Mathf.Max(1, count) * 0.7071f);
        float pitch = size / 0.7071f + gap;
        for (int i = 0; i < dice.Length; i++)
        {
            bool visible = i < count;
            dice[i].gameObject.SetActive(visible);
            if (!visible)
                continue;

            RectTransform rect = dice[i].rectTransform;
            rect.sizeDelta = Vector2.one * size;
            rect.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * pitch, 0);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            dice[i].sprite = Face(results == null ? 1 : results[i]);
            dice[i].color = results == null ? Muted : Ink;
        }
    }

    private void CreateDice(RectTransform tray, Image[] dice)
    {
        for (int i = 0; i < dice.Length; i++)
        {
            RectTransform rect = Place(tray, "Die " + (i + 1), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), 0, 0, 1, 1);
            dice[i] = AddImage(rect, Ink);
            dice[i].preserveAspect = true;
            rect.gameObject.SetActive(false);
        }
    }

    private Sprite Face(int value)
    {
        int i = Mathf.Clamp(value, 1, MaxStat) - 1;
        return faces != null && i < faces.Length ? faces[i] : null;
    }

    private (RectTransform tray, Image[] dice, Text total) CombatDice(RectTransform parent, string title, int y)
    {
        Label(parent, title, 17, y, 15, 180, 21, Muted);
        Text total = Label(parent, "", 222, y, 15, 60, 21, Gold, align: TextAnchor.MiddleRight);
        RectTransform tray = Frame(parent, title + " dice", TopLeft, 16, -y - 24, 268, 42, true);
        var dice = new Image[MaxStat];
        CreateDice(tray, dice);
        return (tray, dice, total);
    }

    private void CharacterCard(RectTransform card, string name, string role, Sprite portrait, Color tint,
        out Text healthLabel, out Image fill, out Text stats)
    {
        RectTransform portraitWell = Frame(card, "Portrait frame", TopLeft, 16, -16, 64, 64, true);
        Image icon = AddImage(Place(portraitWell, "Portrait", TopLeft, TopLeft, 8, -8, 48, 48), Ink);
        icon.sprite = portrait;
        icon.color = tint;
        icon.preserveAspect = true;
        Label(card, name, 94, 20, 23, 190, 30, Ink, FontStyle.Bold);
        Label(card, role, 94, 51, 16, 190, 22, tint);
        healthLabel = Label(card, "", 16, 91, 16, 268, 24, Ink, FontStyle.Bold);
        RectTransform bar = Frame(card, "Health track", TopLeft, 16, -122, 268, 15, true);
        RectTransform healthBar = Place(bar, "Health fill", new Vector2(0, 0.5f),
            new Vector2(0, 0.5f), 2, 0, 264, 11);
        fill = AddImage(healthBar, tint);
        RectTransform statBox = Frame(card, "Attributes", TopLeft, 16, -150, 268, 46, true);
        stats = Label(statBox, "", 12, 8, 16, 244, 30, Ink, align: TextAnchor.MiddleCenter);
    }

    private static void SetCharacter(Text hp, Image bar, Text stats,
        int health, int maxHealth, int agility, int armor, int strength)
    {
        hp.text = $"HP  {health} / {maxHealth}";
        float ratio = Mathf.Clamp01(health / Mathf.Max(1f, maxHealth));
        bar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 264 * ratio);
        stats.text = $"AGI  {agility}     ARM  {armor}     STR  {strength}";
    }

    private static bool MatchesAction(string name, string label) => label switch
    {
        "Attack" => name == "AttackButton",
        "End turn" => name == "EndTurnButton" || name == "EndTurn",
        "Restart" => name == "RestartButton",
        "Menu" => name == "MainMenuButton",
        _ => false
    };

    private Button ActionButton(RectTransform parent, string label, int x, int y, int w, int h,
        Color color, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform frame = Frame(parent, label + " button", TopLeft, x, -y, w, h, true);
        Image target = frame.GetChild(0).GetComponent<Image>();
        target.raycastTarget = true;
        Button button = frame.gameObject.AddComponent<Button>();
        button.targetGraphic = target;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.35f, 1.3f, 1.25f);
        colors.pressedColor = new Color(0.78f, 0.75f, 0.72f);
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f);
        button.colors = colors;
        button.onClick.AddListener(onClick);
        Label(frame, label, 1, 0, 16, w - 2, h, color, FontStyle.Bold, TextAnchor.MiddleCenter);
        return button;
    }

    private Text Label(RectTransform parent, string value, int x, int y, int fontSize, int w, int h,
        Color color, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.MiddleLeft,
        Vector2? anchor = null)
    {
        RectTransform rect = Place(parent, string.IsNullOrEmpty(value) ? "Label" : value,
            anchor ?? TopLeft, TopLeft, x, -y, w, h);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.text = value;
        return text;
    }

    private static RectTransform Frame(RectTransform parent, string name, Vector2 anchor,
        int x, int y, int w, int h, bool inset = false)
    {
        Vector2 pivot = anchor == BottomRight ? BottomRight : TopLeft;
        // Coordinates for the bottom-right frame are measured from its left edge.
        if (anchor == BottomRight)
            x += w;
        if (anchor == Vector2.zero)
            pivot = Vector2.zero;

        RectTransform frame = Place(parent, name, anchor, pivot, x, y, w, h);
        AddImage(frame, Edge);
        RectTransform surface = Place(frame, "Surface", TopLeft, TopLeft, 1, -1, w - 2, h - 2);
        AddImage(surface, inset ? Well : Panel);
        return frame;
    }

    private static RectTransform Place(RectTransform parent, string name, Vector2 anchor,
        Vector2 pivot, float x, float y, float w, float h)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }

    private static Image AddImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

}
