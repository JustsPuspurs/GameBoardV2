using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu")]
    public GameObject gameTitle;
    public GameObject playButton;
    public GameObject rulesButton;
    public GameObject exitButton;

    [Header("Rules")]
    public GameObject rulesPanel;

    private void Start()
    {
        CloseRules();
    }

    // Sāk spēli
    public void PlayGame()
    {
        SceneManager.LoadScene("DR04_Dungeon5x5");
    }

    // Atvērt spēles noteikumus
    public void OpenRules()
    {
        gameTitle.SetActive(false);
        playButton.SetActive(false);
        rulesButton.SetActive(false);
        exitButton.SetActive(false);

        rulesPanel.SetActive(true);
    }

    // Atgriezties galvenajā izvēlnē
    public void CloseRules()
    {
        rulesPanel.SetActive(false);

        gameTitle.SetActive(true);
        playButton.SetActive(true);
        rulesButton.SetActive(true);
        exitButton.SetActive(true);
    }

    // Iziet no spēles
    public void ExitGame()
    {
        Debug.Log("Iziet no spēles");
        Application.Quit();
    }
}