using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class TrainingMode : BaseGameMode
{

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
    [HideInInspector] public BaseSpeaker playerSpeaker;

    Dictionary<BaseCharacter, BasePlayerUI> characterUI = new();

    float timerTracker;

    BaseEcho gameEcho;

    public override void InitGameMode(GameManager manager)
    {
        base.InitGameMode(manager);
        spawnPositions = gameManager.spawnManager.GetSpeakerDuelSpawns();
        InitUI();
        InitTimer();
        InitSpeakers();
        InitEcho();
        StartCoroutine(StartGame());
    }



    protected override IEnumerator StartGame()
    {
        yield return new WaitForFixedUpdate();

        if (gameManager.cameraManager != null) gameManager.cameraManager.OnGameStarted();
        
        matchActive = true;

        //gameEcho.EnableProjectile();

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
    protected void InitTimer()
    {
        timerDisplay.gameObject.SetActive(false);
    }

    protected void InitSpeakers()
    {
        if (MatchData.instance == null)
        {
            Debug.LogWarning("No match data found, cannot initialize speakers");
            return; 
        }
        activeSpeakers.Clear();
        MatchData.PlayerInfo playerSpeakerInfo = new()
        {
            teamIndex = 1, 
        };
        inputManager.playerPrefab = speakerPrefab.gameObject;
        playerSpeakerInfo.device = Gamepad.all.Count > 0 ? Gamepad.all[0] : Keyboard.current;
        queuedPlayerInfo.Enqueue(playerSpeakerInfo);
        inputManager.JoinPlayer(pairWithDevice: playerSpeakerInfo.device);

    }        
    public void InitEcho()
    {
        gameEcho = Instantiate(echoPrefab);

        gameEcho.InitProjectile(speakerList, gameManager.spawnManager.GetAIEchoSpawn(), gameManager);
        gameManager.cameraManager.AddCharacterToCameraTargetGroup(gameEcho.transform, 1.0f, 2.5f);
        gameEcho.WarpToLocation(gameManager.spawnManager.GetAIEchoSpawn());
        gameEcho.SuspendProjectile();
    }
    public override void OnPlayerJoined(PlayerInput playerInput)
    {

        if (!playerInput.gameObject.TryGetComponent(out BaseSpeaker character))
        {
            Debug.LogWarning("Player prefab doesn't have BaseSpeaker component");
            return;
        }
        if (speakerList.Contains(character.transform))
        {
            Debug.LogWarning("Player " + character.name + " already joined");
            return;
        }
        int index = playerInput.playerIndex + 1;
        MatchData.PlayerInfo info = null;
        if (queuedPlayerInfo.Count > 0)
        {
            info = queuedPlayerInfo.Dequeue();
            character.InitPlayer(info, gameManager, index);
        }
        else
        {
            Debug.LogWarning("No queued data for char " + character.name + ", using base speaker KB 1 controls");
        }

        Debug.Log("Added new player: " + character.name);
        StartCoroutine(InitSpeakerSignals(character));
        AddStaminaUIForCharacter(character, info);
        gameManager.cameraManager.AddCharacterToCameraTargetGroup(character.transform);
        if (speakerList.Count == 0)
        {
            character.ActivatePlayer();
            playerSpeaker = character;
        }
        StartCoroutine(SetCharacterPosition(character));
        speakerList.Add(character.transform);
        activeSpeakers.Add(character);
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
        yield break;
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
        gameManager.cameraManager.RemoveCharacterFromCameraTargetGroup(cha.transform);

        cha.ResetComponents();

        StartCoroutine(SetCharacterPosition(cha));
        activeSpeakers.Add(cha);
        characterUI[cha].gameObject.SetActive(true);
    }

    protected void ResetEcho(BaseEcho echo)
    {
        echo.EnableProjectile();
        echo.WarpToLocation(gameManager.spawnManager.GetAIEchoSpawn());
        echo.SuspendProjectile();
    }

}


