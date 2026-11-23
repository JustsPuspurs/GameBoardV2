using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public enum BonusType
{
    Points
}

[RequireComponent(typeof(SpriteRenderer))]
public class Tile : MonoBehaviour, IPointerClickHandler
{
    [Header("Tile Coordinates")]
    public int x;
    public int y;

    [Header("Movement Cost")]
    [Range(1, 2)]
    public int movementCost = 1;

    [Header("Tile Types")]
    public bool isBadTile = false;
    public bool isFinishTile = false;
    public bool hasItem = false;

    [Header("Tile Indicator")]
    public TMP_Text tileIndicatorText;

    [Header("Tile Colours")]
    public Color normalColor = Color.white;
    public Color pointsColor = Color.yellow;
    public Color wallColor = Color.gray;
    public Color trapColor = Color.red;
    public Color finishColor = Color.green;

    [Header("Tile State")]
    [HideInInspector] public bool isRevealed = false;
    [Tooltip("Blocks movement when enabled. This is a board obstacle, not a player ability.")]
    public bool isWall = false;
    [HideInInspector] public BonusType bonusType = BonusType.Points;

    private SpriteRenderer spriteRenderer;
    private LineRenderer moveHighlightRenderer;
    private static Material highlightMaterial;

    private void Awake()
    {
        CacheComponents();
        CreateMoveHighlight();
        RefreshColor();
    }

    private void Start()
    {
        RefreshColor();
    }

    private void OnValidate()
    {
        movementCost = Mathf.Clamp(movementCost, 1, 2);

        if (Application.isPlaying)
        {
            CacheComponents();
            RefreshColor();
        }
    }

    private void CacheComponents()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (tileIndicatorText == null)
            tileIndicatorText = GetComponentInChildren<TMP_Text>(true);
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
        hasItem = true;
        bonusType = BonusType.Points;
        RefreshColor();
    }

    public void HideItem()
    {
        hasItem = false;
        RefreshColor();
    }

    /// <summary>
    /// Sets a board obstacle. This does not create or activate a Wall ability.
    /// </summary>
    public void SetWall(bool blocked)
    {
        isWall = blocked;
        RefreshColor();
    }

    /// <summary>
    /// Updates the tile's appearance based on its current state.
    /// </summary>
    public void RefreshColor()
    {
        if (spriteRenderer == null)
            CacheComponents();

        if (spriteRenderer == null)
            return;

        string indicator = string.Empty;
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
            tileColor = pointsColor;
            indicator = "+1";
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
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.OnTileClicked(this);
    }

    private void CreateMoveHighlight()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Transform existingHighlight = transform.Find("MoveHighlight");
        if (existingHighlight != null)
        {
            moveHighlightRenderer = existingHighlight.GetComponent<LineRenderer>();
            if (moveHighlightRenderer != null)
            {
                moveHighlightRenderer.enabled = false;
                return;
            }
        }

        GameObject highlightObject = new GameObject("MoveHighlight");
        highlightObject.transform.SetParent(transform, false);

        moveHighlightRenderer = highlightObject.AddComponent<LineRenderer>();
        moveHighlightRenderer.useWorldSpace = false;
        moveHighlightRenderer.loop = true;
        moveHighlightRenderer.positionCount = 4;
        moveHighlightRenderer.numCornerVertices = 4;
        moveHighlightRenderer.numCapVertices = 2;

        Bounds bounds = spriteRenderer.sprite.bounds;
        float width = bounds.size.x;
        float height = bounds.size.y;
        float insetX = width * 0.08f;
        float insetY = height * 0.08f;
        float lineWidth = Mathf.Min(width, height) * 0.05f;

        moveHighlightRenderer.widthMultiplier = lineWidth;
        moveHighlightRenderer.SetPosition(0, new Vector3(bounds.min.x + insetX, bounds.min.y + insetY, -0.1f));
        moveHighlightRenderer.SetPosition(1, new Vector3(bounds.min.x + insetX, bounds.max.y - insetY, -0.1f));
        moveHighlightRenderer.SetPosition(2, new Vector3(bounds.max.x - insetX, bounds.max.y - insetY, -0.1f));
        moveHighlightRenderer.SetPosition(3, new Vector3(bounds.max.x - insetX, bounds.min.y + insetY, -0.1f));

        moveHighlightRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        moveHighlightRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        if (highlightMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("Tile: Could not find Sprites/Default shader for movement highlight.", this);
                moveHighlightRenderer.enabled = false;
                return;
            }

            highlightMaterial = new Material(shader);
            highlightMaterial.name = "Tile Movement Highlight Material";
        }

        moveHighlightRenderer.sharedMaterial = highlightMaterial;
        moveHighlightRenderer.startColor = Color.white;
        moveHighlightRenderer.endColor = Color.white;
        moveHighlightRenderer.enabled = false;
    }

    public void SetMoveHighlight(bool active, Color highlightColor)
    {
        if (moveHighlightRenderer == null)
            CreateMoveHighlight();

        if (moveHighlightRenderer == null)
            return;

        moveHighlightRenderer.startColor = highlightColor;
        moveHighlightRenderer.endColor = highlightColor;
        moveHighlightRenderer.enabled = active;
    }
}
