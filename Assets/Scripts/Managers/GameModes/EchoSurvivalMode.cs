using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static ReportManager;
using System.Linq;
using System;

public class EchoSurvivalMode : BaseGameMode
{
    const float SUDDEN_DEATH_SLOW_DOWN_DURATION = 2.5f;
    const float SUDDEN_DEATH_SLOW_DOWN_AMOUNT = 0.1f;
    const float TIME_UNTIL_NEW_ECHO_CREATED = 15.0f;
    const int MAX_ECHO_COUNT = 6;
    [Header("UI Objects")]

    [SerializeField] protected BasePlayerUI healthUIPrefab;
    [SerializeField] protected GameObject UIHolder;
    [SerializeField] protected TMP_Text timerDisplay;
    [SerializeField] protected GameObject winScreen;
    [SerializeField] protected TMP_Text scoreText;
    [SerializeField] protected TMP_Text winText;

    [Header("Player Prefabs")]
    [SerializeField] protected BaseSpeaker speakerPrefab;
    [SerializeField] protected BaseEcho echoPrefab;

    [Header("Other Settings")]
    [SerializeField] protected float speakerCameraRadius = 12.0f;
    [SerializeField] protected float echoCameraWeight = 0.45f; // less weight because there may be multiple echoes

    //public HashSet<Transform> speakerList = new();
    //static HashSet<BaseSpeaker> activeSpeakers = new();

    BaseSpeaker speakerPlayer;

    Dictionary<BaseCharacter, BasePlayerUI> characterUI = new();

    struct ScoreTracker
    {
        public float timeToBeat;
    }

    ScoreTracker scoreTracker;
    float timerTracker;
    float timeUntilNextEcho = TIME_UNTIL_NEW_ECHO_CREATED;

    List<TrackerData> trackerData = new();

    List<BaseEcho> gameEchoes = new();
    TimeSpan matchDuration = new();

    public override void InitGameMode(GameManager manager)
    {
        base.InitGameMode(manager);
        spawnPositions = gameManager.spawnManager.GetSpeakerDuelSpawns();
        Debug.Log("Initializing UI");
        InitUI();
        Debug.Log("Initializing Timer");
        InitTimer();
        Debug.Log("Initializing Players");
        InitSpeakers();
        Debug.Log("Initializing Echoes");
        InitEcho();
        Debug.Log("Starting Game");
        StartCoroutine(StartGame());
    }

    protected virtual void InitEcho()
    {
        foreach(var echo in gameEchoes)
        {
            Destroy(echo.gameObject);
        }
        gameEchoes.Clear();
        var startingEcho = AddNewEcho();
        startingEcho.SuspendProjectile();
    }

    BaseEcho AddNewEcho()
    {
        if (gameEchoes.Count >= MAX_ECHO_COUNT) { return null; }
        var newEcho = Instantiate(echoPrefab);

        HashSet<Transform> speakerList = new()
        {
            speakerPlayer.transform
        };
        newEcho.InitProjectile(speakerList, gameManager.spawnManager.GetAIEchoSpawn());
        gameManager.cameraManager.AddCharacterToCameraTargetGroup(newEcho.transform, echoCameraWeight, 2.5f);
        gameEchoes.Add(newEcho);
        newEcho.EnableProjectile();
        newEcho.transform.name = "Echo " + gameEchoes.Count;
        return newEcho;
    }

    protected override IEnumerator StartGame()
    {
        yield return new WaitForFixedUpdate();

        if (gameManager.cameraManager != null) gameManager.cameraManager.OnGameStarted();
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
        speakerPlayer.ShowPlayer();
        
        yield return new WaitUntil(() => gameManager.announcementManager.announcementPlaying);
        yield return new WaitUntil(() => !gameManager.announcementManager.announcementPlaying);
        if (gameManager.reportManager != null)
        {
            gameManager.reportManager.OnMatchStart();
        }
        matchActive = true;

        gameEchoes[0].EnableProjectile();

        speakerPlayer.ActivatePlayer();
        
    }

    protected override void InitUI()
    {
        foreach (Transform t in UIHolder.transform)
        {
            Destroy(t.gameObject);
        }
        winScreen.SetActive(false);
    }
    protected virtual void InitTimer()
    {
        timerDisplay.gameObject.SetActive(true);
        timerTracker = 0.0f;
        timerDisplay.text = timerTracker.ToString();
    }

    protected virtual void InitSpeakers()
    {
        if (MatchData.instance == null) { return; }

        var team = MatchData.instance.gameTeams[0];
        var member = team.teamMembers.ElementAt(0);
        queuedPlayerInfo.Enqueue(member);
        inputManager.playerPrefab = speakerPrefab.gameObject;
        inputManager.JoinPlayer(pairWithDevice: member.device);
    }
    public override void OnPlayerJoined(PlayerInput playerInput)
    {

        if (!playerInput.gameObject.TryGetComponent(out BaseSpeaker character)) { return; }
        if (speakerPlayer != null) { return; }
        int index = playerInput.playerIndex + 1;
        MatchData.PlayerInfo info = null;
        if (queuedPlayerInfo.Count > 0)
        {
            info = queuedPlayerInfo.Dequeue();
            character.InitPlayer(info, gameManager, index);
            trackerData.Add(new TrackerData()
            {
                speaker = character,
                speakerInfo = info,
            });
        }
        else
        {
            Debug.LogWarning("No queued data for char " + character.name + ", using base speaker KB 1 controls");
        }
        StartCoroutine(InitSpeakerSignals(character));
        AddStaminaUIForCharacter(character, info);
        gameManager.cameraManager.AddCharacterToCameraTargetGroup(character.transform, 1.0f, speakerCameraRadius);
        StartCoroutine(SetCharacterPosition(character));

        if (queuedPlayerInfo.Count == 0 && gameManager.reportManager != null)
        {
            gameManager.reportManager.InitManager(MatchData.instance.selectedGameMode.gameType, trackerData.ToArray());
        }

        speakerPlayer = character;
    }
    protected void AddStaminaUIForCharacter(BaseSpeaker character, MatchData.PlayerInfo info)
    {
        BasePlayerUI newUI = Instantiate(healthUIPrefab, UIHolder.transform);
        newUI.InitDisplay(character, info);
        characterUI[character] = newUI;
    }

    public override void RemoveCharacter(BaseCharacter character)
    {
        if (characterUI.ContainsKey(character))
        {
            characterUI[character].gameObject.SetActive(false);
        }
        gameManager.cameraManager.RemoveCharacterFromCameraTargetGroup(character.transform);
        character.DeactivatePlayer();
    }
    protected override void OnCharacterDefeated(DamageInfo info, HealthComponent victim)
    {
        if (!victim.hurtboxOwner.TryGetComponent(out BaseSpeaker _))
        {
            Debug.Log("Couldn't find base char component");
            return;
        }
        matchActive = false;
        StartCoroutine(OnCharacterVictorious());
    }
    protected override IEnumerator OnCharacterVictorious()
    {
        if (gameManager.reportManager != null)
        {
            gameManager.reportManager.OnMatchEnd();
        }
        TimeSpan timeSpan = TimeSpan.FromSeconds(timerTracker);
        winText.text = "Survived for " + timeSpan.Minutes + ":" + timeSpan.Seconds + ":" + timeSpan.Milliseconds;
        if (scoreText != null)
        {
            UpdateScoreText(speakerPlayer);
        }
        gameManager.bgmManager.OnGameOver();
        gameManager.bgmManager.OnGameWon();
        gameManager.announcementManager.QueueNewAnnouncement(AnnouncementData.winData);
        yield return null;
        gameManager.postProcessingManager.ResetManager();
        yield return new WaitUntil(() => gameManager.announcementManager.announcementPlaying);
        yield return new WaitUntil(() => !gameManager.announcementManager.announcementPlaying);
        winScreen.SetActive(true);
        Time.timeScale = 0.0f;
    }




    protected override void UpdateScoreText(BaseCharacter winner)
    {
        if (timerTracker > scoreTracker.timeToBeat)
        {
            scoreTracker.timeToBeat = timerTracker;
            scoreText.text = "NEW HIGH SCORE";
        }
        else
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(scoreTracker.timeToBeat);
            scoreText.text = "High Score: " + timeSpan.Minutes + ":" + timeSpan.Seconds + ":" + timeSpan.Milliseconds;
        }

    }

    private void Update()
    {
        if (matchActive && !gameManager.pauseManager.GamePaused()) TimerLogic();
    }

    protected virtual void TimerLogic()
    {
        timerTracker += Time.deltaTime;
        timeUntilNextEcho -= Time.deltaTime;
        if (timeUntilNextEcho <= 0.0f)
        {
            timeUntilNextEcho = TIME_UNTIL_NEW_ECHO_CREATED;
            AddNewEcho();
        }
        matchDuration = TimeSpan.FromSeconds(timerTracker);
        timerDisplay.text = $"<mspace=0.6em>{matchDuration.ToString("mm\\:ss\\.ff")}</mspace>";
    }
    protected override void EnterSuddenDeath()
    {
       
    }

    public override void ResetGame()
    {
        matchActive = false;
        inSuddenDeath = false;

        if (MatchData.instance != null)
        {
            timerTracker = MatchData.instance.gameLength;
        }
        else
        {
            timerTracker = DEFAULT_MATCH_LENGTH;
        }
        timerDisplay.text = Mathf.RoundToInt(timerTracker).ToString();


        ResetSpeaker(speakerPlayer);
        speakerPlayer.DeactivatePlayer();
        
        InitEcho();

        winScreen.SetActive(false);
        gameManager.ResetManager();
        StartCoroutine(StartGame());

        timerTracker = 0.0f;
        timeUntilNextEcho = TIME_UNTIL_NEW_ECHO_CREATED;
    }

    protected void ResetSpeaker(BaseSpeaker cha)
    {
        cha.enabled = true;
        cha.ActivatePlayer();
        gameManager.cameraManager.AddCharacterToCameraTargetGroup(cha.transform);


        cha.ResetComponents();

        StartCoroutine(SetCharacterPosition(cha));
        characterUI[cha].gameObject.SetActive(true);
    }

}
