using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static ReportManager;

public class GameManager : MonoBehaviour
{

    const float SUDDEN_DEATH_SLOW_DOWN_DURATION = 2.5f;
    const float SUDDEN_DEATH_SLOW_DOWN_AMOUNT = 0.1f;
    public const float TWEEN_TO_REGULAR_SPEED_DURATION = 0.35f;
    const int DEFAULT_MATCH_LENGTH = 60;

    [HideInInspector] public static bool gamePaused = false;
    [HideInInspector] public static bool inSpecialStop = false; //hitstop, parrystop etc
    [HideInInspector] public static bool frameAfterSpecialStop = false; // cannot deflect the frame after special stop happens
    public PlayerInputManager inputManager;
    public List<BaseEcho> echoList = new();
    public AudioSource bgmPlayer;
    public AudioSource winBGMPlayer;
    public AudioClip winSFX;
    [Header("Managers")]
    public PostProcessingManager postProcessingManager;
    public HUDAnimator HUDAnimator;
    public CameraManager camManager;
    public AnnouncementManager announcementManager;
    public ReportManager reportManager;
    public PauseMenu pauseMenu;
    public BGMManager bgmManager;

    [Header("Player Prefabs")]
    [SerializeField] protected BaseSpeaker speakerPrefab;
    [SerializeField] protected AISpeaker aiSpeakerPrefab;

    [Header("UI Objects")]

    [SerializeField] protected PlayerUI healthUIPrefab;
    [SerializeField] protected GameObject UIHolder;
    [SerializeField] protected List<GameObject> spawnPositions = new();
    [SerializeField] protected TMP_Text timerDisplay;
    [SerializeField] protected GameObject winScreen;
    [SerializeField] protected TMP_Text winText;
    [SerializeField] protected TMP_Text scoreText;
    [SerializeField] protected CinemachineTargetGroup targetGroup;
    [SerializeField] protected Animator mapAnimator;

    public HashSet<Transform> speakerList = new();
    static HashSet<BaseSpeaker> activeSpeakers = new();
      
    Dictionary<BaseSpeaker, PlayerUI> characterUI = new();
    protected Queue<MatchData.PlayerInfo> queuedPlayerInfo = new();

    struct ScoreTracker
    {
        public int teamOneWins;
        public int teamTwoWins;
    }

    ScoreTracker scoreTracker;
    float timerTracker;

    bool inSuddenDeath = false;

    static int stopFrames = 0;


    bool matchActive = false;


    List<TrackerData> trackerData = new();
    private void Start()
    {
        InitManager();
    }

    protected virtual void InitManager()
    {
        if (inputManager == null) inputManager = GetComponent<PlayerInputManager>();

        if (announcementManager == null)
        {
            announcementManager = FindFirstObjectByType<AnnouncementManager>();
        }
        Debug.Log("Initializing UI");
        InitUI();
        Debug.Log("Initializing Timer");
        InitTimer();
        Debug.Log("Initializing Players");
        InitSpeakers();
        Debug.Log("Initializing Echoes");
        InitEchoes();
        Debug.Log("Starting Game");
        StartCoroutine(StartGame());
    }

    protected virtual void InitEchoes()
    {
        foreach (var ball in echoList)
        {
            ball.InitProjectile(speakerList);
            ball.SuspendProjectile();
            AddCharacterToCameraTargetGroup(ball.transform, 1.0f, 2.5f);
        }
    }

    protected virtual IEnumerator StartGame()
    {
        yield return new WaitForFixedUpdate();

        if (camManager != null) camManager.cinemachineCam.CancelDamping(true); // make sure cam is in right spot before starting
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
        announcementManager.QueueNewAnnouncement(countdownDataOne, countdownDataTwo, countdownDataThree, countdownDataFour);
        foreach (var speaker in activeSpeakers)
        {
            speaker.ShowPlayer();
        }
        yield return new WaitUntil(() => announcementManager.annoucementPlaying);
        yield return new WaitUntil(() => !announcementManager.annoucementPlaying);
        if (reportManager != null)
        { 
            reportManager.OnMatchStart();
        }
        matchActive = true;

        foreach (var ball in echoList)
        {
            ball.EnableProjectile();
        }

        foreach (var speaker in activeSpeakers)
        {
            speaker.ActivatePlayer();
        }
    }

    protected virtual void InitUI()
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


    public virtual void OnPlayerJoined(PlayerInput playerInput)
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
        StartCoroutine(InitCharacterSignals(character));
        AddStaminaUIForCharacter(character,info);
        AddCharacterToCameraTargetGroup(character.transform);
        StartCoroutine(SetCharacterPosition(character));
        speakerList.Add(character.transform);
        activeSpeakers.Add(character);

        if (queuedPlayerInfo.Count == 0 && reportManager != null)
        {
            reportManager.InitManager(trackerData.ToArray());
        }
    }
    protected void AddStaminaUIForCharacter(BaseSpeaker character, MatchData.PlayerInfo info)
    {
        PlayerUI newUI = Instantiate(healthUIPrefab, UIHolder.transform);
        newUI.InitDisplay(character, info);
        characterUI[character] = newUI;
    }

    protected virtual IEnumerator InitCharacterSignals(BaseSpeaker character)
    {
        yield return null;
        character.healthComponent.entityDefeated.AddListener(OnCharacterDefeated);

        if (postProcessingManager != null)
        {
            Debug.Log("Post processing manager not null");
            character.healthComponent.entityDamaged.AddListener(postProcessingManager.OnSpeakerStruck);
            character.deflectManager.superDeflectPerformed.AddListener(postProcessingManager.OnSuperDeflectPerformed);
        }

        if (HUDAnimator != null)
        {
            character.healthComponent.entityDamaged.AddListener(HUDAnimator.OnSpeakerStruck);
            character.deflectManager.deflectedBall.AddListener( (echo, partial, usedSkill) => HUDAnimator.OnEchoDeflected());
        }
        if (camManager != null)
        {
            character.healthComponent.entityDamaged.AddListener((info) => camManager.OnSpeakerStruck(character, info));
        }
        if (reportManager != null)
        {
            character.deflectManager.deflectPerformed.AddListener(reportManager.OnSpeakerDeflect);
            character.staminaComponent.foresightPerformed.AddListener(reportManager.OnForesightUsed);
        }

        if (pauseMenu != null)
        {
            pauseMenu.ConnectPauseSignals(character);
        }

    }

    protected virtual IEnumerator SetCharacterPosition(BaseSpeaker character)
    {
        int spawnIndex = (character.teamIndex - 1) % spawnPositions.Count;
        yield return new WaitForFixedUpdate();
        character.transform.position = spawnPositions[spawnIndex].transform.position;
        if (camManager != null) camManager.cinemachineCam.CancelDamping(true);
    }

    public void RemoveCharacter(BaseSpeaker character)
    {
        if (characterUI.ContainsKey(character))
        {
           characterUI[character].gameObject.SetActive(false);
        }
        targetGroup.RemoveMember(character.transform);
        character.DeactivatePlayer();
        activeSpeakers.Remove(character);
    }
    protected virtual void OnCharacterDefeated(DamageInfo info, HealthComponent victim)
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

   protected IEnumerator OnCharacterVictorious()
    {
        if (reportManager != null)
        {
            reportManager.OnMatchEnd();
        }
        BaseSpeaker winner = activeSpeakers.ElementAt(0);
        winText.text = winner.name + " Wins";
        winner.staminaComponent.foresightAuraHum.Stop();
        winner.staminaComponent.foresightElectricityCrackle.Stop();
        if (scoreText != null)
        {
            UpdateScoreText(winner);
        }
        bgmPlayer.Stop();
        winBGMPlayer.PlayOneShot(winSFX);
        AnnouncementData winAnnouncement = new()
        {
            announcementDuration = 2.0f,
            announcementText = "VERDICT",
            customTimescale = 0.1f,
            priority = 9999999
        };
        announcementManager.QueueNewAnnouncement(winAnnouncement);
        yield return null;
        postProcessingManager.ResetManager();
        yield return new WaitUntil(() => announcementManager.annoucementPlaying);
        yield return new WaitUntil(() => !announcementManager.annoucementPlaying);
        winBGMPlayer.Play();
        winScreen.SetActive(true);
        Time.timeScale = 0.0f;
    }

    void UpdateScoreText(BaseSpeaker winner)
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
        if (matchActive && !gamePaused) TimerLogic();
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

    private void FixedUpdate()
    {
        if (gamePaused) return;
        if (frameAfterSpecialStop) frameAfterSpecialStop = false;
        if (inSpecialStop)
        {
            stopFrames -= 1;
            if (stopFrames <= 0)
            {
                stopFrames = 0;
                inSpecialStop = false;
                frameAfterSpecialStop = true;
            }
        }
    }
    protected void EnterSuddenDeath()
    {
        Debug.Log("Entering sudden death");
        foreach (var cha in activeSpeakers)
        {
            cha.staminaComponent.EnterSuddenDeath();
        }
        foreach (var ball in echoList)
        {
            ball.EnterSuddenDeath();
        }

        timerDisplay.text = "X";

        postProcessingManager.OnSuddenDeathStarted();

        AnnouncementData suddenDeathAnnouncement = new ()
        {
            announcementDuration = SUDDEN_DEATH_SLOW_DOWN_DURATION,
            announcementText = "SUDDEN DEATH",
            customTimescale = SUDDEN_DEATH_SLOW_DOWN_AMOUNT,
            priority = 999
        };
        announcementManager.QueueNewAnnouncement(suddenDeathAnnouncement);

        inSuddenDeath = true;
    }

    public bool InSuddenDeath()
    {
        return inSuddenDeath;
    }
    public static void ApplyHitstop(int frames)
    {
        if (gamePaused || frames <= 0) { return; }
        inSpecialStop = true;
        stopFrames =  Mathf.Max(stopFrames, frames);
        Debug.Log("stopping game for " + frames + " frames");
        foreach (var speaker in activeSpeakers)
        {
            speaker.deflectManager.OnSpecialStopStarted();
        }
    }

    public virtual void ResetGame()
    {
        Time.timeScale = 1.0f;
        matchActive = false;
        bgmPlayer.time = 0;
        bgmPlayer.Play();
        inSuddenDeath = false;

        postProcessingManager.ResetManager();

        if (MatchData.instance != null)
        {
            timerTracker = MatchData.instance.gameLength;
        }
        else
        {
            timerTracker = DEFAULT_MATCH_LENGTH;
        }
        timerDisplay.text = Mathf.RoundToInt(timerTracker).ToString();
        inSpecialStop = false;
        stopFrames = 0;
        frameAfterSpecialStop = false;
        foreach (Transform cha in speakerList)
        {
            BaseSpeaker speaker = cha.GetComponent<BaseSpeaker>();
            ResetPlayer(speaker);
            speaker.DeactivatePlayer();
            activeSpeakers.Add(speaker);
        }
        InitEchoes();
        if (mapAnimator)
        {
            mapAnimator.Play("Reset", 0, 0.0f);
        }

        winScreen.SetActive(false);
        winBGMPlayer.Stop();
        StartCoroutine(StartGame());

        if (reportManager != null)
        {
            reportManager.OnMatchStart();
        }
        if (bgmManager != null)
        {
            bgmManager.PlayNewTrack();
        }
    }

    void ResetPlayer(BaseSpeaker cha)
    {
        cha.enabled = true;
        cha.ActivatePlayer();
        AddCharacterToCameraTargetGroup(cha.transform);


        cha.staminaComponent.ResetComponent(true);
        cha.healthComponent.ResetComponent();
        cha.velocityManager.ResetComponent();
        cha.characterStateMachine.ResetComponent();
        cha.deflectManager.ResetComponent();

        StartCoroutine(SetCharacterPosition(cha));
        activeSpeakers.Add(cha);
        characterUI[cha].gameObject.SetActive(true);
    }
   public void AddCharacterToCameraTargetGroup(Transform chaTransform, float weight = 1.0f, float radius = 5.0f)
    {
        targetGroup.AddMember(chaTransform, weight, radius);
    }


    public void RemoveCharacterFromCameraTargetGroup(Transform chaTransform)
    {
        targetGroup.RemoveMember(chaTransform);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneRegistry.MainMenu.ToString());
    }
}
