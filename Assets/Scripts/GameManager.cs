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

    private int currentPlayer = 1;

    private int remainingMoves = 0;

    private int scoreP1 = 0;
    private int scoreP2 = 0;

    private bool hasRolled = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void RollDice()
    {
        if (hasRolled)
            return;

        remainingMoves = Random.Range(1, 7);

        hasRolled = true;

        diceText.text = "Dice: " + remainingMoves;
    }

    public void TryMoveToTile(Tile targetTile)
    {
        if (!hasRolled || remainingMoves <= 0)
            return;

        Tile currentTile;

        if (currentPlayer == 1)
            currentTile = player1Tile;
        else
            currentTile = player2Tile;

        int distance =
            Mathf.Abs(targetTile.x - currentTile.x) +
            Mathf.Abs(targetTile.y - currentTile.y);

        // Drīkst iet tikai uz blakus lauciņu
        if (distance != 1)
            return;

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

        if (remainingMoves == 0)
        {
            CheckTile(targetTile);
            EndTurn();
        }
    }

    private void CheckTile(Tile tile)
    {
        if (!tile.isBadTile)
            return;

        if (currentPlayer == 1)
        {
            scoreP1 -= 2;

            if (scoreP1 < 0)
                scoreP1 = 0;
        }
        else
        {
            scoreP2 -= 2;

            if (scoreP2 < 0)
                scoreP2 = 0;
        }

        Debug.Log("BAD TILE! -2 score");

        UpdateUI();
    }

    private void EndTurn()
    {
        currentPlayer = currentPlayer == 1 ? 2 : 1;

        hasRolled = false;

        diceText.text = "Dice: -";

        UpdateUI();
    }

    private void UpdateUI()
    {
        turnText.text = "Turn: Player " + currentPlayer;

        scoreP1Text.text = "P1: " + scoreP1 + "/10";
        scoreP2Text.text = "P2: " + scoreP2 + "/10";
    }
}