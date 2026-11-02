using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Players")]
    public Transform player1;
    public Transform player2;

    public Tile player1Tile;
    public Tile player2Tile;

    public Tile finishTile;

    [Header("UI")]
    public TMP_Text diceText;
    public TMP_Text turnText;
    public TMP_Text scoreP1Text;
    public TMP_Text scoreP2Text;
    public TMP_Text winText;

    [Header("Dice Animation")]
    public Image diceImage;
    public Button rollButton;

    public Sprite[] diceFaces = new Sprite[6];

    [Min(0.1f)]
    public float diceAnimationDuration = 1.5f;

    private int currentPlayer = 1;
    private int remainingMoves = 0;

    private int itemsP1 = 0;
    private int itemsP2 = 0;

    private bool hasRolled = false;
    private bool isRolling = false;
    private bool gameOver = false;

    private Tile[] allTiles;

    private Tile player1StartTile;
    private Tile player2StartTile;

    private float gridOffset = 1f;

    private int activeItemsCount = 6;
    private int activeBadTilesCount = 2;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Paslēpjam uzvaras tekstu
        if (winText != null)
        {
            winText.text = "";
        }

        // Aktivizējam kauliņa pogu
        if (rollButton != null)
        {
            rollButton.interactable = true;
        }

        // Sākumā parādām kauliņa pirmo pusi
        if (diceImage != null &&
            diceFaces != null &&
            diceFaces.Length > 0 &&
            diceFaces[0] != null)
        {
            diceImage.sprite = diceFaces[0];
            diceImage.color = Color.white;

            diceImage.rectTransform.localRotation =
                Quaternion.identity;
        }

        // Atrodam visus spēles lauciņus
        allTiles = Object.FindObjectsByType<Tile>(
            FindObjectsSortMode.None
        );

        // Saglabājam sākuma lauciņus
        player1StartTile = player1Tile;
        player2StartTile = player2Tile;

        // Atrodam Finish lauciņu
        if (finishTile != null)
        {
            finishTile.isFinishTile = true;
        }
        else
        {
            foreach (Tile tile in allTiles)
            {
                if (tile.isFinishTile)
                {
                    finishTile = tile;
                    break;
                }
            }
        }

        // Aprēķinām attālumu starp lauciņiem
        if (allTiles.Length > 1)
        {
            float minDistance = float.MaxValue;

            Vector3 firstPos =
                allTiles[0].transform.position;

            for (int i = 1; i < allTiles.Length; i++)
            {
                float distance = Vector3.Distance(
                    firstPos,
                    allTiles[i].transform.position
                );

                if (distance > 0.1f &&
                    distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            if (minDistance < float.MaxValue)
            {
                gridOffset = minDistance;
            }
        }

        // Piešķiram koordinātas
        foreach (Tile tile in allTiles)
        {
            tile.x = Mathf.RoundToInt(
                tile.transform.position.x / gridOffset
            );

            tile.y = Mathf.RoundToInt(
                tile.transform.position.y / gridOffset
            );
        }

        // Novietojam spēlētājus
        if (player1 != null && player1Tile != null)
        {
            player1.position =
                player1Tile.transform.position;
        }

        if (player2 != null && player2Tile != null)
        {
            player2.position =
                player2Tile.transform.position;
        }

        // Izveidojam slazdus un priekšmetus
        GenerateRandomBadTiles();
        GenerateRandomItems();

        UpdateUI();
        if (diceText != null)
        {
            diceText.text = "Roll the dice!";
        }
    }

    // KAULIŅA MEŠANA
    public void RollDice()
    {
        // Neļaujam atkārtotu metienu
        if (hasRolled || isRolling || gameOver)
        {
            return;
        }

        hasRolled = true;
        isRolling = true;

        remainingMoves = 0;

        // Bloķējam pogu animācijas laikā
        if (rollButton != null)
        {
            rollButton.interactable = false;
        }

        if (diceText != null)
        {
            diceText.text = "Rolling...";
        }

        StartCoroutine(AnimateDiceRoll());
    }

    // KAULIŅA ANIMĀCIJA
    private IEnumerator AnimateDiceRoll()
    {
        // Izvēlamies nejaušu rezultātu
        int result = Random.Range(1, 7);

        bool canAnimate =
            diceImage != null &&
            diceFaces != null &&
            diceFaces.Length >= 6;

        // Pārbaudām visas kauliņa puses
        if (canAnimate)
        {
            for (int i = 0; i < 6; i++)
            {
                if (diceFaces[i] == null)
                {
                    canAnimate = false;
                    break;
                }
            }
        }

        if (canAnimate)
        {
            float elapsed = 0f;
            float nextFaceTime = 0f;

            // Animācija ilgst 1.5 sekundes
            while (elapsed < diceAnimationDuration)
            {
                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / diceAnimationDuration
                );

                // Pakāpeniski samazinām rotācijas ātrumu
                float rotationSpeed = Mathf.Lerp(
                    1100f,
                    100f,
                    progress
                );

                diceImage.rectTransform.Rotate(
                    0f,
                    0f,
                    -rotationSpeed * Time.deltaTime
                );

                // Ātri mainām kauliņa puses
                if (elapsed >= nextFaceTime)
                {
                    int randomFace = Random.Range(0, 6);

                    diceImage.sprite =
                        diceFaces[randomFace];

                    // Animācija pakāpeniski palēninās
                    float faceInterval = Mathf.Lerp(
                        0.06f,
                        0.24f,
                        progress * progress
                    );

                    nextFaceTime =
                        elapsed + faceInterval;
                }

                yield return null;
            }

            // Parādām īsto rezultātu
            diceImage.sprite = diceFaces[result - 1];

            // Atgriežam sākotnējo rotāciju
            diceImage.rectTransform.localRotation =
                Quaternion.identity;
        }
        else
        {
            Debug.LogWarning(
                "Dice Animation: piešķir Dice Image un visas 6 Dice Faces!"
            );
        }

        // Piešķiram spēlētājam gājienus
        remainingMoves = result;

        isRolling = false;

        if (diceText != null)
        {
            diceText.text =
                "Moves left: " + remainingMoves;
        }

        Debug.Log("Kauliņš: " + result);
    }

    // SPĒLĒTĀJA PĀRVIETOŠANĀS
    public void TryMoveToTile(Tile targetTile)
    {
        // Animācijas laikā pārvietoties nedrīkst
        if (targetTile == null ||
            !hasRolled ||
            isRolling ||
            remainingMoves <= 0 ||
            gameOver)
        {
            return;
        }

        Tile currentTile =
            currentPlayer == 1
                ? player1Tile
                : player2Tile;

        if (currentTile == null)
        {
            Debug.LogError(
                "Spēlētāja lauciņš nav piešķirts!"
            );

            return;
        }

        // Pārbaudām attālumu
        int distance =
            Mathf.Abs(targetTile.x - currentTile.x) +
            Mathf.Abs(targetTile.y - currentTile.y);
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
            player1.position =
                targetTile.transform.position;

            player1Tile = targetTile;
        }
        else
        {
            player2.position =
                targetTile.transform.position;

            player2Tile = targetTile;
        }

        remainingMoves--;

        if (diceText != null)
        {
            diceText.text =
                "Moves left: " + remainingMoves;
        }

        // Pārbaudām lauciņa efektu
        EvaluateTileEffect(targetTile);

        if (gameOver)
        {
            return;
        }

        // Mainām spēlētāju
        if (remainingMoves == 0)
        {
            EndTurn();
        }
    }

    // LAUCIŅA EFEKTI
    private void EvaluateTileEffect(Tile tile)
    {
        // Slazds
        if (tile.isBadTile && !tile.isRevealed)
        {
            tile.RevealTile();

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

            Debug.Log("Slazds! -2 punkti.");

            tile.isBadTile = false;

            StartCoroutine(
                ReplaceBadTile(tile)
            );
        }

        // Priekšmets
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

            SpawnNewItem();
        }

        // Finish
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

        if (gameOver && rollButton != null)
        {
            rollButton.interactable = false;
        }

        UpdateUI();
    }

    // PRIEKŠMETU ĢENERĒŠANA
    private void GenerateRandomItems()
    {
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
            "Izveidoti priekšmeti: " + spawnedItems
        );
    }

    // JAUNS PRIEKŠMETS
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
                "Jauns priekšmets: " + randomTile.name
            );

            return;
        }
    }

    // PĀRBAUDĀM PRIEKŠMETA VIETU
    private bool CanSpawnItem(Tile tile)
    {
        if (tile == finishTile || tile.isFinishTile)
        {
            return false;
        }

        if (tile == player1StartTile ||
            tile == player2StartTile)
        {
            return false;
        }

        if (tile == player1Tile ||
            tile == player2Tile)
        {
            return false;
        }

        if (tile.isBadTile || tile.isRevealed)
        {
            return false;
        }

        if (tile.hasItem)
        {
            return false;
        }

        return true;
    }

    // SLAZDU ĢENERĒŠANA
    private void GenerateRandomBadTiles()
    {
        foreach (Tile tile in allTiles)
        {
            tile.HideBadTile();
        }

        int spawnedBadTiles = 0;
        int attempts = 0;

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
            "Izveidoti slazdi: " + spawnedBadTiles
        );
    }

    // JAUNS SLAZDS
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
                "Jauns paslēpts slazds: " + randomTile.name
            );

            return;
        }

        Debug.LogWarning(
            "Neizdevās atrast vietu jaunam slazdam."
        );
    }

    // PĀRBAUDĀM SLAZDA VIETU
    private bool CanSpawnBadTile(Tile tile)
    {
        if (tile == finishTile || tile.isFinishTile)
        {
            return false;
        }

        if (tile == player1StartTile ||
            tile == player2StartTile)
        {
            return false;
        }

        if (tile == player1Tile ||
            tile == player2Tile)
        {
            return false;
        }

        if (tile.hasItem)
        {
            return false;
        }

        if (tile.isBadTile || tile.isRevealed)
        {
            return false;
        }

        return true;
    }

    // PĀRVIETOJAM SLAZDU
    private IEnumerator ReplaceBadTile(Tile oldTile)
    {
        yield return new WaitForSeconds(0.6f);

        oldTile.HideBadTile();

        if (!gameOver)
        {
            SpawnNewBadTile();
        }
    }

    // GĀJIENA BEIGAS
    private void EndTurn()
    {
        currentPlayer =
            currentPlayer == 1 ? 2 : 1;

        hasRolled = false;

        if (diceText != null)
        {
            diceText.text = "Roll the dice!";
        }

        if (rollButton != null)
        {
            rollButton.interactable = true;
        }

        UpdateUI();
    }

    // ATJAUNOJAM INTERFEISU
    private void UpdateUI()
    {
        if (turnText != null)
        {
            turnText.text =
                "Turn: Player " + currentPlayer;
        }

        if (scoreP1Text != null)
        {
            scoreP1Text.text =
                "P1: " + itemsP1 + "/10";
        }

        if (scoreP2Text != null)
        {
            scoreP2Text.text =
                "P2: " + itemsP2 + "/10";
        }
    }
}