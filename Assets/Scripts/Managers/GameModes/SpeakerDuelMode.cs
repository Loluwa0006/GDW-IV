using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static ReportManager;

public class SpeakerDuelMode : BaseGameMode
{
    const float SUDDEN_DEATH_SLOW_DOWN_DURATION = 2.5f;
    const float SUDDEN_DEATH_SLOW_DOWN_AMOUNT = 0.1f;

    [Header("UI Objects")]

    [SerializeField] protected BasePlayerUI healthUIPrefab;
    [SerializeField] protected GameObject UIHolder;
    [SerializeField] protected TMP_Text timerDisplay;
    [SerializeField] protected GameObject winScreen;
    [SerializeField] protected TMP_Text scoreText;
    [SerializeField] protected TMP_Text winText;

    [Header("Player Prefabs")]
    [SerializeField] protected BaseSpeaker speakerPrefab;
    [SerializeField] protected BaseSpeaker aiSpeakerPrefab;
    [SerializeField] protected BaseEcho echoPrefab;

    public HashSet<Transform> speakerList = new();
    static HashSet<BaseSpeaker> activeSpeakers = new();

    Dictionary<BaseCharacter, BasePlayerUI> characterUI = new();

    struct ScoreTracker
    {
        public int teamOneWins;
        public int teamTwoWins;
    }

    ScoreTracker scoreTracker;
    float timerTracker;

    List<TrackerData> trackerData = new();

    BaseEcho gameEcho;

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

 

    protected override IEnumerator StartGame()
    {
        yield return new WaitForFixedUpdate();

        if (gameManager.camManager != null) gameManager.camManager.cinemachineCam.CancelDamping(true); // make sure cam is in right spot before starting
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
        foreach (var speaker in activeSpeakers)
        {
            speaker.ShowPlayer();
        }
        yield return new WaitUntil(() => gameManager.announcementManager.annoucementPlaying);
        yield return new WaitUntil(() => !gameManager.announcementManager.annoucementPlaying);
        if (gameManager.reportManager != null)
        {
            gameManager.reportManager.OnMatchStart();
        }
        matchActive = true;

        gameEcho.EnableProjectile();
        
        foreach (var speaker in activeSpeakers)
        {
            speaker.ActivatePlayer();
        }
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
        Debug.Log("match length is " + MatchData.instance.gameLength);
        timerTracker = MatchData.instance.gameLength;
        timerDisplay.text = timerTracker.ToString();
    }

    protected virtual void InitSpeakers()
    {
        if (MatchData.instance == null) { return; }
        activeSpeakers.Clear();
        int memberIndex = 0;
        int teamIndex = 0;
        List<TrackerData> speakerData = new();
        foreach (MatchData.TeamInfo team in MatchData.instance.gameTeams)
        {
            teamIndex++;
            foreach (MatchData.PlayerInfo member in team.teamMembers)
            {
                member.teamIndex = teamIndex;
                memberIndex++;
                if (member.playerType == MatchData.PlayerType.Speaker)
                {
                    if (member.isAI)
                    {
                        inputManager.playerPrefab = aiSpeakerPrefab.gameObject;
                    }
                    else
                    {
                        inputManager.playerPrefab = speakerPrefab.gameObject;
                    }
                    queuedPlayerInfo.Enqueue(member);
                    inputManager.JoinPlayer(pairWithDevice: member.device);
                }

            }
        }
    }
    protected virtual void InitEcho()
    {
        gameEcho = Instantiate(echoPrefab);

        gameEcho.InitProjectile(speakerList, gameManager.spawnManager.GetAIEchoSpawn());
        gameManager.AddCharacterToCameraTargetGroup(gameEcho.transform, 1.0f, 2.5f);
        gameEcho.WarpToLocation(gameManager.spawnManager.GetAIEchoSpawn());
        gameEcho.SuspendProjectile();

    }
    public override void OnPlayerJoined(PlayerInput playerInput)
    {

        if (!playerInput.gameObject.TryGetComponent(out BaseSpeaker character)) { return; }
        if (speakerList.Contains(character.transform)) { return; }
        int index = playerInput.playerIndex + 1;
        MatchData.PlayerInfo info = null;
        if (queuedPlayerInfo.Count > 0)
        {
            info = queuedPlayerInfo.Dequeue();
            character.InitPlayer(info, index);
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
        gameManager.AddCharacterToCameraTargetGroup(character.transform);
        StartCoroutine(SetCharacterPosition(character));
        speakerList.Add(character.transform);
        activeSpeakers.Add(character);

        if (queuedPlayerInfo.Count == 0 && gameManager.reportManager != null)
        {
            gameManager.reportManager.InitManager(trackerData.ToArray());
        }
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
        gameManager.RemoveCharacterFromCameraTargetGroup(character.transform);
        character.DeactivatePlayer();
        activeSpeakers.Remove(character.GetComponent<BaseSpeaker>());
    }
    protected override void OnCharacterDefeated(DamageInfo info, HealthComponent victim)
    {
        if (!victim.hurtboxOwner.TryGetComponent(out BaseSpeaker defeated))
        {
            Debug.Log("Couldn't find base char component");
            return;
        }
        RemoveCharacter(defeated);
        Debug.Log(defeated.name + " has been defeated, " + activeSpeakers.Count + " characters remain");
        if (activeSpeakers.Count == 1)
        {
            StartCoroutine(OnCharacterVictorious());
        }
    }
       protected override IEnumerator OnCharacterVictorious()
    {
        if (gameManager.reportManager != null)
        {
            gameManager.reportManager.OnMatchEnd();
        }
        BaseSpeaker winner = activeSpeakers.ElementAt(0);
        winText.text = winner.name + " Wins";
        winner.staminaComponent.foresightAuraHum.Stop();
        winner.staminaComponent.foresightElectricityCrackle.Stop();
        if (scoreText != null)
        {
            UpdateScoreText(winner);
        }
        gameManager.bgmPlayer.Stop();
        gameManager.winBGMPlayer.PlayOneShot(gameManager.winSFX);
        AnnouncementData winAnnouncement = new()
        {
            announcementDuration = 2.0f,
            announcementText = "VERDICT",
            customTimescale = 0.1f,
            priority = 9999999
        };
        gameManager.announcementManager.QueueNewAnnouncement(winAnnouncement);
        yield return null;
        gameManager.postProcessingManager.ResetManager();
        yield return new WaitUntil(() => gameManager.announcementManager.annoucementPlaying);
        yield return new WaitUntil(() => !gameManager.announcementManager.annoucementPlaying);
        gameManager.winBGMPlayer.Play();
        winScreen.SetActive(true);
        Time.timeScale = 0.0f;
    }
    



    protected override void UpdateScoreText(BaseCharacter winner)
    {
        if (winner.teamIndex == 1)
        {
            scoreTracker.teamOneWins += 1;
        }
        else
        {
            scoreTracker.teamTwoWins += 1;
        }

        scoreText.text = scoreTracker.teamOneWins + "/" + scoreTracker.teamTwoWins;
    }

    private void Update()
    {
        if (matchActive && !GameManager.gamePaused) TimerLogic();
    }

    protected virtual void TimerLogic()
    {
        timerTracker -= Time.deltaTime;
        if (timerTracker <= 0.0f)
        {
            if (!inSuddenDeath)
            {
                inSuddenDeath = true;
                EnterSuddenDeath();
            }
        }
        else
        {
            timerTracker = Mathf.Clamp(timerTracker, 0.0f, MatchData.instance.gameLength);
            timerDisplay.text = Mathf.RoundToInt(timerTracker).ToString();
        }
    }
    protected override void EnterSuddenDeath()
    {
        Debug.Log("Entering sudden death");
        foreach (var cha in activeSpeakers)
        {
            cha.staminaComponent.EnterSuddenDeath();
        }
        gameEcho.EnterSuddenDeath();
        timerDisplay.text = "X";

        gameManager.postProcessingManager.OnSuddenDeathStarted();

        AnnouncementData suddenDeathAnnouncement = new()
        {
            announcementDuration = SUDDEN_DEATH_SLOW_DOWN_DURATION,
            announcementText = "SUDDEN DEATH",
            customTimescale = SUDDEN_DEATH_SLOW_DOWN_AMOUNT,
            priority = 999
        };
        gameManager.announcementManager.QueueNewAnnouncement(suddenDeathAnnouncement);

        inSuddenDeath = true;
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

        foreach (Transform cha in speakerList)
        {
            BaseSpeaker speaker = cha.GetComponent<BaseSpeaker>();
            ResetSpeaker(speaker);
            speaker.DeactivatePlayer();
        }
        ResetEcho(gameEcho);

        winScreen.SetActive(false);
        gameManager.ResetManager();
        StartCoroutine(StartGame());

    }

    protected void ResetSpeaker(BaseSpeaker cha)
    {
        cha.enabled = true;
        cha.ActivatePlayer();
        gameManager.AddCharacterToCameraTargetGroup(cha.transform);

        cha.ResetComponents();

        StartCoroutine(SetCharacterPosition(cha));
        activeSpeakers.Add(cha);
        characterUI[cha].gameObject.SetActive(true);
    }

    protected void ResetEcho(BaseEcho echo)
    {
        echo.WarpToLocation(gameManager.spawnManager.GetAIEchoSpawn());
        echo.ResetProjectile();
        echo.SuspendProjectile();
        echo.SetNewTarget(speakerList.ElementAt(0));
    }


}
