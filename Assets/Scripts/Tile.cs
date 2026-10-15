using UnityEngine;
using UnityEngine.EventSystems;

// Lauciņa skripts
public class Tile : MonoBehaviour, IPointerClickHandler
{
    public int x;
    public int y;

    public bool isBadTile = false;
    public bool isFinishTile = false;
    public bool hasItem = false;

    [HideInInspector]
    public bool isRevealed = false;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        RefreshColor();
    }

    public void RevealTile()
    {
        // Parādām aktivizēto slazdu
        isRevealed = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }
    }

    public void SetBadTileHidden()
    {
        // Izveidojam paslēptu slazdu
        isBadTile = true;
        isRevealed = false;

        RefreshColor();
    }

    public void HideBadTile()
    {
        // Noņemam veco slazdu
        isBadTile = false;
        isRevealed = false;

        RefreshColor();
    }

    public void CollectItem()
    {
        // Noņemam savākto priekšmetu
        hasItem = false;

        RefreshColor();
    }

    public void ShowItem()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        // Finish nekad nedrīkst kļūt dzeltens
        if (isFinishTile)
        {
            spriteRenderer.color = Color.green;
            return;
        }

        if (hasItem)
        {
            spriteRenderer.color = Color.yellow;
        }
    }

    public void HideItem()
    {
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        // Finish vienmēr ir zaļš
        if (isFinishTile)
        {
            spriteRenderer.color = Color.green;
        }
        // Priekšmets ir dzeltens
        else if (hasItem)
        {
            spriteRenderer.color = Color.yellow;
        }
        // Tikai aktivizēts slazds ir redzams sarkans
        else if (isBadTile && isRevealed)
        {
            spriteRenderer.color = Color.red;
        }
        // Paslēptie slazdi izskatās kā parasti balti lauciņi
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError(
                "GameManager nav atrasts!"
            );

            return;
        }

        GameManager.Instance.TryMoveToTile(this);
    }
}