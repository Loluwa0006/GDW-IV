using UnityEngine;
using UnityEngine.SceneManagement;
public class PauseManager : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] GameObject pauseScreen;

    [SerializeField] SettingsManager settingsManager;

    bool gamePaused;

    private void Awake()
    {
        OnResumePressed();
    }

    private void Start()
    {
        settingsManager.gameObject.SetActive(true);
        settingsManager.InitSettings();
        settingsManager.gameObject.SetActive(false);
    }
    public void ConnectPauseSignals(BaseCharacter cha)
    {
        cha.requestedPause.AddListener(OnPauseRequested);
    }

    public void OnPauseRequested(BaseCharacter cha)
    {
        gamePaused = !gamePaused;
        pauseScreen.SetActive(gamePaused);
    }


    public void OnResumePressed()
    {
        gamePaused = false;
        pauseScreen.SetActive(false);
    }

    public void OnQuitPressed()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void OnRestartPressed()
    {
        gamePaused = false;
        pauseScreen.SetActive(false);
        gameManager.currentGameMode.ResetGame();
    }

    public bool GamePaused()
    {
        return gamePaused;
    }
}
