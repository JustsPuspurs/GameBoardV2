using UnityEngine;
using UnityEngine.EventSystems; // Nepieciešams, lai strādātu klikšķu sistēma

// Pievienojam IPointerClickHandler saskarni
public class Tile : MonoBehaviour, IPointerClickHandler
{
    public int x;
    public int y;

    public bool isBadTile = false;
    public bool isFinishTile = false;
    public bool hasItem = false;

    [HideInInspector] public bool isRevealed = false; 
    private SpriteRenderer spriteRenderer; 

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (spriteRenderer != null)
        {
            if (isFinishTile)
                spriteRenderer.color = Color.green; 
            else if (hasItem)
                spriteRenderer.color = Color.yellow; 
            else
                spriteRenderer.color = Color.white; 
        }
    }

    public void RevealTile()
    {
        isRevealed = true;
        if (spriteRenderer != null && isBadTile)
        {
            spriteRenderer.color = Color.red; 
        }
    }

    public void CollectItem()
    {
        hasItem = false;
        if (spriteRenderer != null && !isFinishTile)
        {
            spriteRenderer.color = Color.white; 
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager.Instance.TryMoveToTile(this);
    }
}