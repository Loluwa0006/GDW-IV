using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static ReportManager;

public class SpeakerDuelMode : BaseGameMode, ISimulationSnapshot<SpeakerDuelSnapshot>, ISnapshotable
{

    const int SUDDEN_DEATH_SLOW_DOWN_DURATION = 150;
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


    protected SpeakerDuelNetworkManager networkManager;

    public List<BaseSpeaker> speakerList = new();
    List<BaseSpeaker> activeSpeakers = new();

    Dictionary<BaseCharacter, BasePlayerUI> characterUI = new();

    struct ScoreTracker
    {
        public int teamOneWins;
        public int teamTwoWins;
    }

    ScoreTracker scoreTracker;
    int modeTimer;

    List<TrackerData> trackerData = new();

    BaseEcho gameEcho;

    SpeakerDuelSnapshot[] modeSnapshots = new SpeakerDuelSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];

    public override void InitGameMode(GameManager manager)
    { 
        gameManager = manager;
        gameManager.simulationManager.AddSnapshotableObject(this);
        networkManager = gameManager.speakerDuelNetworkManager;
        base.InitGameMode(manager);
        spawnPositions = gameManager.spawnManager.GetSpeakerDuelSpawns();
        InitUI();
        InitTimer();
        InitSpeakers();
        InitEcho();
        if (networkManager != null) networkManager.InitComponent(this);
        StartCoroutine(StartGame());
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
        foreach (var speaker in activeSpeakers)
        {
            speaker.ShowPlayer();
        }
        yield return new WaitUntil(() => gameManager.announcementManager.announcementPlaying);
        yield return new WaitUntil(() => !gameManager.announcementManager.announcementPlaying);
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
        modeTimer = MatchData.instance.gameLength * 60;//convert to seconds;
        timerDisplay.text = Mathf.RoundToInt(modeTimer / 60).ToString();
    }

    protected virtual void InitSpeakers()
    {
        if (MatchData.instance == null) { return; }
        activeSpeakers.Clear();
        int memberIndex = 0;
        int teamIndex = 0;
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

        List<Transform> speakerTransforms = new();
        foreach (var speaker in speakerList)
        {
            speakerTransforms.Add(speaker.transform);
        }
        gameEcho.InitProjectile(speakerTransforms, gameManager.spawnManager.GetAIEchoSpawn(), gameManager);
        gameManager.cameraManager.AddCharacterToCameraTargetGroup(gameEcho.transform, 1.0f, 2.5f);
        gameEcho.WarpToLocation(gameManager.spawnManager.GetAIEchoSpawn());
        gameEcho.SuspendProjectile();

    }
    public override void OnPlayerJoined(PlayerInput playerInput)
    {

        if (!playerInput.gameObject.TryGetComponent(out BaseSpeaker character)) { return; }
        if (speakerList.Contains(character)) { return; }
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
        gameManager.cameraManager.AddCharacterToCameraTargetGroup(character.transform);
        StartCoroutine(SetCharacterPosition(character));
        speakerList.Add(character);
        activeSpeakers.Add(character);

        if (queuedPlayerInfo.Count == 0 && gameManager.reportManager != null)
        {
            gameManager.reportManager.InitManager(MatchData.instance.selectedGameMode.gameType, trackerData.ToArray());
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
        gameManager.cameraManager.RemoveCharacterFromCameraTargetGroup(character.transform);
        character.DeactivatePlayer();
        activeSpeakers.Remove(character.GetComponent<BaseSpeaker>());
    }
    protected override void OnCharacterDefeated(DamageInfo info, HealthComponent victim)
    {
        if (!victim.hurtboxOwner.TryGetComponent(out BaseSpeaker defeated))
        {
            return;
        }
        RemoveCharacter(defeated);
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
        gameManager.bgmManager.OnGameOver();
        gameManager.bgmManager.OnGameWon();
        AnnouncementData winAnnouncement = AnnouncementData.winData;
        gameManager.announcementManager.QueueNewAnnouncement(winAnnouncement);
        yield return null;
        gameManager.postProcessingManager.ResetManager();
        yield return new WaitUntil(() => gameManager.announcementManager.announcementPlaying);
        yield return new WaitUntil(() => !gameManager.announcementManager.announcementPlaying);
        gameManager.bgmManager.OnGameWon();
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

    public override void UpdateMode()
    {
        if (matchActive && !gameManager.pauseManager.GamePaused()) TimerLogic();
    }
    protected virtual void TimerLogic()
    { 
        if (modeTimer > 0)
        {
            modeTimer--;
            if (modeTimer <= 0)
            {
                if (!inSuddenDeath) EnterSuddenDeath();
            }
            else timerDisplay.text = Mathf.RoundToInt(modeTimer / 60).ToString();
        }
        else timerDisplay.text = "X";
        
    }
    protected override void EnterSuddenDeath()
    {
        inSuddenDeath = true;
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
            modeTimer = MatchData.instance.gameLength;
        }
        else
        {
            modeTimer = DEFAULT_MATCH_LENGTH;
        }
        timerDisplay.text = Mathf.RoundToInt(modeTimer).ToString();

        foreach (BaseSpeaker speaker in speakerList)
        {
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
        gameManager.cameraManager.AddCharacterToCameraTargetGroup(cha.transform);

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
        echo.SetNewTarget(speakerList[0].transform);
    }

    public SpeakerDuelSnapshot CaptureState()
    {
        return new SpeakerDuelSnapshot()
        {
            suddenDeathActive = inSuddenDeath,
            timeRemaining = modeTimer
        };
    }

    public void RestoreState(SpeakerDuelSnapshot snapshot)
    {
        modeTimer = snapshot.timeRemaining;
        inSuddenDeath = snapshot.suddenDeathActive;
    }

    public void CaptureCurrentState(int tick)
    {
        modeSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(modeSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }

    public SpeakerDuelWorldSnapshot CreateWorldSnapshot ()
    {
        SpeakerDuelWorldSnapshot snapshot = new ();

        snapshot.echoSnapshot = gameEcho.GetWorldSnapshot();
        snapshot.entityManagerSnapshot = gameManager.entityManager.CaptureState();
        snapshot.speakerDuelSnapshot = CaptureState();

        snapshot.speakerOneSnapshot = speakerList[0].GetWorldSnapshot();
        snapshot.speakerTwoSnapshot = speakerList[1].GetWorldSnapshot();
        return snapshot;
    }
}
public struct SpeakerDuelSnapshot
{
    public int timeRemaining;
    public bool suddenDeathActive;
}
/// <summary>
/// Snapshot used for network consolidation
/// </summary>
public struct SpeakerDuelWorldSnapshot 
{
    public SpeakerWorldSnapshot speakerOneSnapshot;
    public SpeakerWorldSnapshot speakerTwoSnapshot;
    public EchoWorldSnapshot echoSnapshot;
    public EntityManagerSnapshot entityManagerSnapshot;
    public SpeakerDuelSnapshot speakerDuelSnapshot;
}