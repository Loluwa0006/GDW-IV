using NUnit.Framework;
using UnityEngine;

using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class PauseMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] GameObject pauseScreen;
    
    public void ConnectPauseSignals(BaseCharacter cha)
    {
        cha.requestedPause.AddListener(OnPauseRequested);
    }

    public void OnPauseRequested(BaseCharacter cha)
    {
        GameManager.gamePaused = !GameManager.gamePaused;
        pauseScreen.SetActive(GameManager.gamePaused);
    }


    public void OnResumePressed()
    {
        GameManager.gamePaused = false;
        pauseScreen.SetActive(false);
    }

    public void OnQuitPressed()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
