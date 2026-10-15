using System.Collections;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Transform player1;
    public Transform player2;

    public Tile player1Tile;
    public Tile player2Tile;

    // Finish lauciņš
    public Tile finishTile;

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

    // Visi spēles lauciņi
    private Tile[] allTiles;

    // Sākuma lauciņus saglabājam atsevišķi,
    // lai uz tiem nekad neparādītos priekšmeti vai slazdi
    private Tile player1StartTile;
    private Tile player2StartTile;

    // Attālums starp lauciņiem
    private float gridOffset = 1f;

    // Vienlaikus uz laukuma būs 6 priekšmeti
    private int activeItemsCount = 6;

    // Vienlaikus uz laukuma būs 2 paslēpti slazdi
    private int activeBadTilesCount = 2;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Paslēpjam uzvaras tekstu spēles sākumā
        if (winText != null)
        {
            winText.text = "";
        }

        // Atrodam visus spēles lauciņus
        allTiles = Object.FindObjectsByType<Tile>(
            FindObjectsSortMode.None
        );

        // Saglabājam sākuma lauciņus
        player1StartTile = player1Tile;
        player2StartTile = player2Tile;

        // Ja Finish ir piešķirts Inspector, atzīmējam to kā Finish
        if (finishTile != null)
        {
            finishTile.isFinishTile = true;
        }
        else
        {
            // Ja Finish nav piešķirts, mēģinām to atrast automātiski
            foreach (Tile tile in allTiles)
            {
                if (tile.isFinishTile)
                {
                    finishTile = tile;
                    break;
                }
            }
        }

        // Automātiski aprēķinām attālumu starp lauciņiem
        if (allTiles.Length > 1)
        {
            float minDistance = float.MaxValue;
            Vector3 firstPos = allTiles[0].transform.position;

            for (int i = 1; i < allTiles.Length; i++)
            {
                float distance = Vector3.Distance(
                    firstPos,
                    allTiles[i].transform.position
                );

                if (distance > 0.1f && distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            gridOffset = minDistance;

            Debug.Log(
                "Automātiski noteiktais attālums starp laukiem: "
                + gridOffset
            );
        }

        // Piešķiram katram lauciņam režģa koordinātas
        foreach (Tile tile in allTiles)
        {
            tile.x = Mathf.RoundToInt(
                tile.transform.position.x / gridOffset
            );

            tile.y = Mathf.RoundToInt(
                tile.transform.position.y / gridOffset
            );
        }

        // Novietojam pirmo spēlētāju uz sākuma lauciņa
        if (player1 != null && player1Tile != null)
        {
            player1.position = player1Tile.transform.position;
        }

        // Novietojam otro spēlētāju uz sākuma lauciņa
        if (player2 != null && player2Tile != null)
        {
            player2.position = player2Tile.transform.position;
        }

        // Vispirms izveidojam 2 paslēptus slazdus
        GenerateRandomBadTiles();

        // Pēc tam izveidojam 6 priekšmetus
        GenerateRandomItems();

        UpdateUI();
    }

    public void RollDice()
    {
        // Neļaujam mest kauliņu atkārtoti viena gājiena laikā
        if (hasRolled || gameOver)
        {
            return;
        }

        remainingMoves = Random.Range(1, 7);

        hasRolled = true;
    diceText.text =
            "Moves left: " + remainingMoves;
    }

    public void TryMoveToTile(Tile targetTile)
    {
        // Pārvietoties drīkst tikai pēc kauliņa mešanas
        if (!hasRolled || remainingMoves <= 0 || gameOver)
        {
            return;
        }

        Tile currentTile =
            currentPlayer == 1 ? player1Tile : player2Tile;

        if (currentTile == null)
        {
            Debug.LogError(
                "Spēlētāja lauciņš nav piešķirts!"
            );

            return;
        }

        // Aprēķinām attālumu līdz izvēlētajam lauciņam
        int distance =
            Mathf.Abs(targetTile.x - currentTile.x) +
            Mathf.Abs(targetTile.y - currentTile.y);

        // Drīkst pārvietoties tikai uz blakus esošu lauciņu
        if (distance != 1)
        {
            Debug.LogWarning(
                "Izvēlētais lauciņš nav blakus."
            );

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

        diceText.text =
            "Moves left: " + remainingMoves;

        // Pārbaudām jaunā lauciņa efektu
        EvaluateTileEffect(targetTile);

        if (gameOver)
        {
            return;
        }

        // Kad visi soļi izmantoti, mainām spēlētāju
        if (remainingMoves == 0)
        {
            EndTurn();
        }
    }

    private void EvaluateTileEffect(Tile tile)
    {
        // Pārbaudām, vai spēlētājs uzkāpa uz paslēpta slazda
        if (tile.isBadTile && !tile.isRevealed)
        {
            // Parādām sarkano lauciņu
            tile.RevealTile();

            // Atņemam 2 punktus
            if (currentPlayer == 1)
            {
                itemsP1 = Mathf.Max(
                    0,
                    itemsP1 - 2
                );
            }
            else
            {
                itemsP2 = Mathf.Max(
                    0,
                    itemsP2 - 2
                );
            }

            Debug.Log(
                "Slazds! -2 punkti."
            );

            // Vecais slazds vairs nav aktīvs
            tile.isBadTile = false;

            // Pēc īsa brīža paslēpjam sarkano krāsu
            // un izveidojam jaunu slazdu citā vietā
            StartCoroutine(
                ReplaceBadTile(tile)
            );
        }

        // Pārbaudām, vai uz lauciņa ir priekšmets
        if (tile.hasItem)
        {
            if (currentPlayer == 1)
            {
                itemsP1++;
            }
            else
            {
                itemsP2++;
            }

            tile.CollectItem();

            Debug.Log(
                "Priekšmets savākts! +1 punkts."
            );

            // Izveidojam jaunu priekšmetu citā vietā
            SpawnNewItem();
        }

        // Pārbaudām Finish
        if (tile == finishTile || tile.isFinishTile)
        {
            if (currentPlayer == 1 && itemsP1 >= 10)
            {
                if (winText != null)
                {
                    winText.text =
                        "PLAYER 1 WINS!";
                }

                gameOver = true;
            }
            else if (
                currentPlayer == 2 &&
                itemsP2 >= 10
            )
            {
                if (winText != null)
                {
                    winText.text =
                        "PLAYER 2 WINS!";
                }

                gameOver = true;
            }
            else
            {
                Debug.Log(
                    "Lai uzvarētu, nepieciešami 10 punkti!"
                );
            }
        }

        UpdateUI();
    }
    private void GenerateRandomItems()
    {
        // Noņemam visus iepriekš iestatītos priekšmetus
        foreach (Tile tile in allTiles)
        {
            tile.hasItem = false;
            tile.HideItem();
        }

        int spawnedItems = 0;
        int attempts = 0;

        while (
            spawnedItems < activeItemsCount &&
            attempts < 500
        )
        {
            attempts++;

            Tile randomTile =
                allTiles[
                    Random.Range(0, allTiles.Length)
                ];

            if (!CanSpawnItem(randomTile))
            {
                continue;
            }

            randomTile.hasItem = true;
            randomTile.ShowItem();

            spawnedItems++;
        }

        Debug.Log(
            "Izveidoti priekšmeti: "
            + spawnedItems
        );
    }

    private void SpawnNewItem()
    {
        int attempts = 0;

        while (attempts < 500)
        {
            attempts++;

            Tile randomTile =
                allTiles[
                    Random.Range(0, allTiles.Length)
                ];

            if (!CanSpawnItem(randomTile))
            {
                continue;
            }

            randomTile.hasItem = true;
            randomTile.ShowItem();

            Debug.Log(
                "Jauns priekšmets: "
                + randomTile.name
            );

            return;
        }
    }

    private bool CanSpawnItem(Tile tile)
    {
        // Priekšmets nedrīkst būt uz Finish
        if (tile == finishTile || tile.isFinishTile)
        {
            return false;
        }

        // Priekšmets nedrīkst būt uz sākuma lauciņiem
        if (
            tile == player1StartTile ||
            tile == player2StartTile
        )
        {
            return false;
        }

        // Priekšmets nedrīkst parādīties zem spēlētāja
        if (
            tile == player1Tile ||
            tile == player2Tile
        )
        {
            return false;
        }

        // Priekšmets nedrīkst būt uz slazda
        if (tile.isBadTile || tile.isRevealed)
        {
            return false;
        }

        // Uz viena lauciņa nevar būt divi priekšmeti
        if (tile.hasItem)
        {
            return false;
        }

        return true;
    }

    private void GenerateRandomBadTiles()
    {
        // Noņemam vecos slazdus
        foreach (Tile tile in allTiles)
        {
            tile.HideBadTile();
        }

        int spawnedBadTiles = 0;
        int attempts = 0;

        // Izveidojam tieši 2 paslēptus slazdus
        while (
            spawnedBadTiles < activeBadTilesCount &&
            attempts < 500
        )
        {
            attempts++;

            Tile randomTile =
                allTiles[
                    Random.Range(0, allTiles.Length)
                ];

            if (!CanSpawnBadTile(randomTile))
            {
                continue;
            }

            randomTile.SetBadTileHidden();

            spawnedBadTiles++;
        }

        Debug.Log(
            "Izveidoti slazdi: "
            + spawnedBadTiles
        );
    }

    private void SpawnNewBadTile()
    {
        int attempts = 0;

        while (attempts < 500)
        {
            attempts++;

            Tile randomTile =
                allTiles[
                    Random.Range(0, allTiles.Length)
                ];

            if (!CanSpawnBadTile(randomTile))
            {
                continue;
            }

            randomTile.SetBadTileHidden();

            Debug.Log(
                "Jauns paslēpts slazds: "
                + randomTile.name
            );

            return;
        }

        Debug.LogWarning(
            "Neizdevās atrast vietu jaunam slazdam."
        );
    }

    private bool CanSpawnBadTile(Tile tile)
    {
        // Slazds nedrīkst būt uz Finish
        if (tile == finishTile || tile.isFinishTile)
        {
            return false;
        }
        // Slazds nedrīkst būt uz sākuma lauciņiem
        if (
            tile == player1StartTile ||
            tile == player2StartTile
        )
        {
            return false;
        }

        // Slazds nedrīkst parādīties zem spēlētāja
        if (
            tile == player1Tile ||
            tile == player2Tile
        )
        {
            return false;
        }

        // Slazds nedrīkst būt uz priekšmeta
        if (tile.hasItem)
        {
            return false;
        }

        // Uz viena lauciņa nevar būt divi slazdi
        if (tile.isBadTile || tile.isRevealed)
        {
            return false;
        }

        return true;
    }

    private IEnumerator ReplaceBadTile(Tile oldTile)
    {
        // Īsu brīdi parādām sarkano lauciņu
        yield return new WaitForSeconds(0.6f);

        // Vecais slazds kļūst par parastu lauciņu
        oldTile.HideBadTile();

        // Ja spēle vēl turpinās, izveidojam jaunu slazdu
        if (!gameOver)
        {
            SpawnNewBadTile();
        }
    }

    private void EndTurn()
    {
        // Mainām aktīvo spēlētāju
        currentPlayer =
            currentPlayer == 1 ? 2 : 1;

        hasRolled = false;

        diceText.text =
            "Roll the dice!";

        UpdateUI();
    }

    private void UpdateUI()
    {
        turnText.text =
            "Turn: Player " + currentPlayer;

        scoreP1Text.text =
            "P1: " + itemsP1 + "/10";

        scoreP2Text.text =
            "P2: " + itemsP2 + "/10";
    }
}
