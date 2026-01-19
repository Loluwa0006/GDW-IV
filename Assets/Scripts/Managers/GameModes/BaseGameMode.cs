using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BaseGameMode : MonoBehaviour
{

    protected const int DEFAULT_MATCH_LENGTH = 60;
    public const float TWEEN_TO_REGULAR_SPEED_DURATION = 0.35f;

    [HideInInspector] public GameManager gameManager;
    [SerializeField] protected PlayerInputManager inputManager;
 
    protected Queue<MatchData.PlayerInfo> queuedPlayerInfo = new();


   protected bool inSuddenDeath = false;

   protected bool matchActive = false;

    protected List<Vector3> spawnPositions = new();


    public virtual void InitGameMode(GameManager manager)
    {
        gameManager = manager;
        if (inputManager == null)  inputManager = GetComponent<PlayerInputManager>();
    }

    protected virtual IEnumerator StartGame()
    {
        yield return new WaitForFixedUpdate();

        AnnouncementData countdownDataOne = new()
        {
            announcementDuration = 1.0f,
            announcementText = "3",
            customTimescale = 0.0f,
            priority = 5
        };
        AnnouncementData countdownDataTwo = new(countdownDataOne);
        AnnouncementData countdownDataThree = new(countdownDataTwo);
        AnnouncementData countdownDataFour = new(countdownDataThree);
        countdownDataTwo.announcementText = "2";
        countdownDataThree.announcementText = "1";
        countdownDataFour.announcementText = "BEGIN";
        countdownDataFour.customTimescale = 1.0f;
        gameManager.announcementManager.QueueNewAnnouncement(countdownDataOne, countdownDataTwo, countdownDataThree, countdownDataFour);
        yield return new WaitUntil(() => gameManager.announcementManager.annoucementPlaying);
        yield return new WaitUntil(() => !gameManager.announcementManager.annoucementPlaying);

        matchActive = true;
    }

    protected virtual void InitUI()
    {
     
    }

    public virtual void OnPlayerJoined(PlayerInput playerInput)
    {

    }

    protected virtual IEnumerator InitSpeakerSignals(BaseSpeaker speaker)
    {
        yield return null;
        speaker.healthComponent.entityDefeated.AddListener(OnCharacterDefeated);

        if (gameManager.postProcessingManager != null)
        {
            Debug.Log("Post processing manager not null");
            speaker.healthComponent.entityDamaged.AddListener(gameManager.postProcessingManager.OnSpeakerStruck);
            speaker.deflectManager.superDeflectPerformed.AddListener(gameManager.postProcessingManager.OnSuperDeflectPerformed);
        }

        if (gameManager.HUDAnimator != null)
        {
            speaker.healthComponent.entityDamaged.AddListener(gameManager.HUDAnimator.OnSpeakerStruck);
            speaker.deflectManager.deflectedBall.AddListener((echo, partial, usedSkill) => gameManager.HUDAnimator.OnEchoDeflected());
        }
        if (gameManager.camManager != null)
        {
            speaker.healthComponent.entityDamaged.AddListener((info) => gameManager.camManager.OnSpeakerStruck(speaker, info));
        }
        if (gameManager.reportManager != null)
        {
            speaker.deflectManager.deflectPerformed.AddListener(gameManager.reportManager.OnSpeakerDeflect);
            speaker.staminaComponent.foresightPerformed.AddListener(gameManager.reportManager.OnForesightUsed);
        }

        if (gameManager.pauseMenu != null)
        {
            gameManager.pauseMenu.ConnectPauseSignals(speaker);
        }

    }

    protected virtual IEnumerator SetCharacterPosition(BaseCharacter character)
    {
        int spawnIndex = (character.teamIndex - 1) % spawnPositions.Count;
        yield return new WaitForFixedUpdate();
        character.transform.position = spawnPositions[spawnIndex];
        if (gameManager.camManager != null) gameManager.camManager.cinemachineCam.CancelDamping(true);
    }

    public virtual void RemoveCharacter(BaseCharacter character)
    {
        
    }
    protected virtual void OnCharacterDefeated(DamageInfo info, HealthComponent victim)
    {
        
    }

    protected virtual IEnumerator OnCharacterVictorious()
    {
        yield break;
    }

    protected virtual void UpdateScoreText(BaseCharacter winner)
    {

    }

    protected virtual void EnterSuddenDeath()
    {

    }

    public bool InSuddenDeath()
    {
        return inSuddenDeath;
    }


    public virtual void ResetGame()
    {
       StartCoroutine(StartGame());
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneRegistry.MainMenu.ToString());
    }

    public void ToggleReportDisplay(bool status)
    {
        gameManager.reportManager.reportDisplay.SetActive(status);
    }
}