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
    [HideInInspector] public bool matchActive = false;
    [SerializeField] protected PlayerInputManager inputManager;
    protected Queue<MatchData.PlayerInfo> queuedPlayerInfo = new();
    protected bool inSuddenDeath = false;
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
            announcementDuration = 60,
            announcementText = "3",
            customTimescale = 0.0f,
            priority = 5
        };
        AnnouncementData countdownDataTwo = countdownDataOne;
        AnnouncementData countdownDataThree = countdownDataTwo;
        AnnouncementData countdownDataFour = countdownDataThree;
        countdownDataTwo.announcementText = "2";
        countdownDataThree.announcementText = "1";
        countdownDataFour.announcementText = "BEGIN";
        countdownDataFour.customTimescale = 1.0f;
        gameManager.announcementManager.QueueNewAnnouncement(countdownDataOne, countdownDataTwo, countdownDataThree, countdownDataFour);
        yield return new WaitUntil(() => gameManager.announcementManager.announcementPlaying);
        yield return new WaitUntil(() => !gameManager.announcementManager.announcementPlaying);

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
            speaker.healthComponent.entityDamaged.AddListener(gameManager.postProcessingManager.OnSpeakerStruck);
            speaker.deflectManager.superDeflectPerformed.AddListener(gameManager.postProcessingManager.OnSuperDeflectPerformed);
        }

        if (gameManager.HUDAnimator != null)
        {
            speaker.healthComponent.entityDamaged.AddListener(gameManager.HUDAnimator.OnSpeakerStruck);
            speaker.deflectManager.deflectedBall.AddListener((echo, partial, usedSkill) => gameManager.HUDAnimator.OnEchoDeflected());
        }
        if (gameManager.cameraManager != null)
        {
            speaker.healthComponent.entityDamaged.AddListener((info) => gameManager.cameraManager.OnSpeakerStruck(speaker, info));
        }
        if (gameManager.reportManager != null)
        {
            speaker.deflectManager.deflectPerformed.AddListener(gameManager.reportManager.OnSpeakerDeflect);
            speaker.staminaComponent.foresightPerformed.AddListener(gameManager.reportManager.OnForesightUsed);
        }

        if (gameManager.pauseManager != null)
        {
            gameManager.pauseManager.ConnectPauseSignals(speaker);
        }

    }

    public virtual void UpdateMode()
    {

    }
    private void FixedUpdate()
    {
        UpdateMode();
    }
    protected virtual IEnumerator SetCharacterPosition(BaseCharacter character)
    {
        int spawnIndex = (character.teamIndex - 1) % spawnPositions.Count;
        yield return new WaitForFixedUpdate();
        character.transform.position = spawnPositions[spawnIndex];
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