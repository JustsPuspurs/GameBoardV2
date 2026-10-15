using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Transform player1;
    public Transform player2;

    public Tile player1Tile;
    public Tile player2Tile;

    public TMP_Text diceText;
    public TMP_Text turnText;
    public TMP_Text scoreP1Text;
    public TMP_Text scoreP2Text;
    public TMP_Text winText; 

    private int currentPlayer = 1;
    private int remainingMoves = 0;
    
    private int itemsP1 = 0;
    private int itemsP2 = 0;
    
    private bool hasRolled = false;
    private bool gameOver = false;

    // Mainīgais, kas automātiski saglabās laukumu izmēru / attālumu
    private float gridOffset = 1f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (winText != null) winText.text = "";

        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        
        // 1. AUTOMĀTISKI APRĒĶINĀM GRID OFFSET (attālumu starp laukiem)
        if (allTiles.Length > 1)
        {
            // Atrodam mazāko atrasto distanci starp pirmo lauciņu un jebkuru citu, lai saprastu soli
            float minDistance = float.MaxValue;
            Vector3 firstPos = allTiles[0].transform.position;
            
            for (int i = 1; i < allTiles.Length; i++)
            {
                float d = Vector3.Distance(firstPos, allTiles[i].transform.position);
                if (d > 0.1f && d < minDistance)
                {
                    minDistance = d;
                }
            }
            gridOffset = minDistance;
            Debug.Log("Automātiski noteiktais attālums starp laukiem (Grid Offset): " + gridOffset);
        }

        // 2. PIEŠĶIRAM KOORDINĀTAS
        foreach (Tile t in allTiles)
        {
            // Izdalām ar offsetu un noapaļojam, lai iegūtu smuku 0, 1, 2 gridu neatkarīgi no fiziskā izmēra
            t.x = Mathf.RoundToInt(t.transform.position.x / gridOffset);
            t.y = Mathf.RoundToInt(t.transform.position.y / gridOffset);
        }

        UpdateUI();
    }

    public void RollDice()
    {
        if (hasRolled || gameOver)
            return;

        remainingMoves = Random.Range(1, 7);
        hasRolled = true;
        diceText.text = "Moves left: " + remainingMoves;
    }

    public void TryMoveToTile(Tile targetTile)
    {
        if (!hasRolled || remainingMoves <= 0 || gameOver)
            return;

        Tile currentTile = (currentPlayer == 1) ? player1Tile : player2Tile;

        if (currentTile == null)
        {
            Debug.LogError("Player " + currentPlayer + " Tile is not assigned in GameManager Inspector!");
            return;
        }

        // Aprēķinām distanci pēc jaunajām noapaļotajām koordinātām
        int distance = Mathf.Abs(targetTile.x - currentTile.x) + Mathf.Abs(targetTile.y - currentTile.y);
        
        // Tagad distance vienmēr būs precīzi 1 blakus esošam lauciņam!
        if (distance != 1)
        {
            Debug.LogWarning("Target tile is too far or diagonal. Distance: " + distance);
            return;
        }

        // Pārvietojam aktīvo spēlētāju
        if (currentPlayer == 1)
        {
            player1.position = targetTile.transform.position;
            player1Tile = targetTile;
        }
        else
        {
            player2.position = targetTile.transform.position;
            player2Tile = targetTile;
        }

        remainingMoves--;
        diceText.text = "Moves left: " + remainingMoves;

        EvaluateTileEffect(targetTile);

        if (gameOver) return;

        if (remainingMoves == 0)
        {
            EndTurn();
        }
    }

    private void EvaluateTileEffect(Tile tile)
    {
        if (tile.isBadTile && !tile.isRevealed)
        {
            tile.RevealTile(); 
            if (currentPlayer == 1)
                itemsP1 = Mathf.Max(0, itemsP1 - 2); 
            else
                itemsP2 = Mathf.Max(0, itemsP2 - 2);

            Debug.Log("Trap triggered! -2 items.");
        }

        if (tile.hasItem)
        {
            if (currentPlayer == 1) itemsP1++;
            else itemsP2++;

            tile.CollectItem(); 
            Debug.Log("Item collected! +1");
        }

        if (tile.isFinishTile)
        {
            if (currentPlayer == 1 && itemsP1 >= 10)
            {
                winText.text = "Player 1 reached Finish and WINS!";
                gameOver = true;
            }
            else if (currentPlayer == 2 && itemsP2 >= 10)
            {
                winText.text = "Player 2 reached Finish and WINS!";
                gameOver = true;
            }
            else
            {
                Debug.Log("You are on Finish, but you need 10 items to win!");
            }
        }

        UpdateUI();
    }

    private void EndTurn()
    {
        currentPlayer = currentPlayer == 1 ? 2 : 1;
        hasRolled = false;
        diceText.text = "Roll the dice!";
        UpdateUI();
    }

    private void UpdateUI()
    {
        turnText.text = "Turn: Player " + currentPlayer;
        scoreP1Text.text = "P1 Items: " + itemsP1 + "/10";
        scoreP2Text.text = "P2 Items: " + itemsP2 + "/10";
    }
}
