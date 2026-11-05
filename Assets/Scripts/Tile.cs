
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public enum BonusType
{
    Points,
    Wall,
    Freeze
}

public class Tile : MonoBehaviour, IPointerClickHandler
{
    [Header("Tile Coordinates")]
    public int x;
    public int y;

    [Header("Tile Types")]
    public bool isBadTile = false;
    public bool isFinishTile = false;
    public bool hasItem = false;

    [Header("Tile Indicator")]
    public TMP_Text tileIndicatorText;

    [Header("Tile Colours")]
    public Color normalColor = Color.white;
    public Color pointsColor = Color.yellow;
    public Color wallBonusColor = new Color(0.1f, 0.65f, 1f);
    public Color freezeBonusColor = new Color(0.65f, 0.25f, 1f);
    public Color wallColor = Color.gray;
    public Color trapColor = Color.red;
    public Color finishColor = Color.green;

    [HideInInspector] public bool isRevealed = false;
    [HideInInspector] public bool isWall = false;
    [HideInInspector] public int wallTurnsRemaining = 0;
    [HideInInspector] public BonusType bonusType = BonusType.Points;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        RefreshColor();
    }

    private void Start()
    {
        RefreshColor();
    }

    public void RevealTile()
    {
        isRevealed = true;
        RefreshColor();
    }

    public void SetBadTileHidden()
    {
        isBadTile = true;
        isRevealed = false;
        RefreshColor();
    }

    public void HideBadTile()
    {
        isBadTile = false;
        isRevealed = false;
        RefreshColor();
    }

    public void CollectItem()
    {
        hasItem = false;
        bonusType = BonusType.Points;
        RefreshColor();
    }

    public void ShowItem()
    {
        RefreshColor();
    }

    public void HideItem()
    {
        RefreshColor();
    }

    public void PlaceWall(int turns)
    {
        isWall = true;
        wallTurnsRemaining = turns;
        RefreshColor();
    }

    public void RemoveWall()
    {
        isWall = false;
        wallTurnsRemaining = 0;
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (spriteRenderer == null)
            return;

        string indicator = "";
        Color tileColor = normalColor;

        if (isWall)
        {
            tileColor = wallColor;
            indicator = "BLOCKED";
        }
        else if (isFinishTile)
        {
            tileColor = finishColor;
        }
        else if (hasItem)
        {
            switch (bonusType)
            {
                case BonusType.Points:
                    tileColor = pointsColor;
                    indicator = "+1";
                    break;

                case BonusType.Wall:
                    tileColor = wallBonusColor;
                    indicator = "WALL";
                    break;

                case BonusType.Freeze:
                    tileColor = freezeBonusColor;
                    indicator = "FREEZE";
                    break;
            }
        }
        else if (isBadTile && isRevealed)
        {
            tileColor = trapColor;
            indicator = "TRAP";
        }

        spriteRenderer.color = tileColor;

        if (tileIndicatorText != null)
        {
            bool showIndicator = !string.IsNullOrEmpty(indicator);

            tileIndicatorText.gameObject.SetActive(showIndicator);

            if (showIndicator)
                tileIndicatorText.text = indicator;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTileClicked(this);
    }
}
