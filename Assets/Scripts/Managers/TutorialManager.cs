using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TutorialManager : GameManager
{

    public enum AnimatorLayers
    {
        EntryClip = 0,
        ExitClip = 1
    }

    [System.Serializable]
    public enum SectionName
    {
        Introduction,

        //Speaker 
        Speaker_Movement,
        Deflect,
        Select_Your_Path,
        Dash,
        Counterslash,
        Afterimage, 
        Grapple,
        SpeakerWinCondition,
        SpeakerFinalBattle,

        //Echo 
        EchoMovement,
        Lighten,
        Crash, 
        FlameWheel,
        Translucent,
        EchoWinCondition,
        EchoFinalBattle,


        //Generic
        Complete
        
    }
    [System.Serializable]
    public class TutorialSection
    {
        public int tutorialPoints = 0;
        public SectionName sectionName;
        public int pointsToContinue = 3;
        public SectionName nextSection = SectionName.Speaker_Movement;
        public SectionName prevSection;
        public bool failOnDeath = false;
        public List<SectionAddon> sectionAddons = new();
        public Transform spawnTransform;
        public AnimationClip entryClip;
        public AnimationClip exitClip;

    }

    public class SectionAddon
    {
        [SerializeField] protected TutorialSection section;
        [SerializeField] protected TutorialManager manager;
        [HideInInspector] public bool sectionCompletable = true; 
        public virtual void OnSectionStarted()
        {

        }

        public virtual void SectionLogic()
        {

        }

        public virtual void SectionPhysicsLogic()
        {

        }

        public virtual void OnSectionEnded()
        {

        }

        public virtual void OnSectionRestarted()
        {

        }
    }

    public class TimerAddon : SectionAddon
    {
        [SerializeField] float timerDuration = 10.0f;
        float timerTracker = 0.0f;
        public override void SectionLogic()
        {
            timerTracker += Time.deltaTime;
            if (timerTracker > timerDuration)
            {
                sectionCompletable = false; //you failed 
            }
        }

        public override void OnSectionRestarted()
        {
            timerTracker = 0.0f;
            sectionCompletable = true;
        }

        public override void OnSectionStarted()
        {
           // manager.timerDisplay.gameObject.SetActive(true);
        }

        public override void OnSectionEnded()
        {
          //  manager.timerDisplay.gameObject.SetActive(false);
        }
    }

    public UnityEvent<TutorialSection> sectionRestarted = new();
    public UnityEvent<TutorialSection> sectionEnded = new();
    public UnityEvent<TutorialSection> sectionStarted = new();
    public DialogueManager dialogueManager;

    [SerializeField] List<TutorialSection> tutorialSections = new();
    [SerializeField] Transform respawnPoint;
    [SerializeField] TMP_Text sectionDisplay;
    [SerializeField] Animator tutorialAnimator;


    TutorialSection currentSection =  null;
    Dictionary<SectionName, TutorialSection> sectionDict = new();

    

    protected override void InitManager()
    {
        base.InitManager();

        if (tutorialSections.Count <= 0) { return; }

        currentSection = tutorialSections[0];

        foreach (var section in tutorialSections)
        {
            sectionDict[section.sectionName] = section;
            section.tutorialPoints = 0;
        }

        InitSpeakers();
        InitEchoes();
        StartTutorial();
    }

    protected void InitEchoes()
    {
        //var echoList = FindObjectsByType<BaseEcho>(FindObjectsSortMode.InstanceID);
        //var 
        //foreach (var ball in echoList)
        //{
        //    ball.InitProjectile(speakerList);
        //    ball.SuspendProjectile();
        //}
    }

    protected void InitSpeakers()
    {
        InputDevice inputDevice = Gamepad.all.Count > 0 ? Gamepad.all[0] : Keyboard.current;


        MatchData.PlayerInfo tutorialPlayer = new ()
        {
            device = inputDevice,
            controlScheme = "Combat",
            playerType = MatchData.PlayerType.Speaker,
            skillOne = SkillName.None,
            skillTwo = SkillName.None,
        };
      // queuedPlayerInfo.Enqueue(tutorialPlayer);
      // inputManager.JoinPlayer(pairWithDevice: inputDevice);
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        //base.OnPlayerJoined(playerInput);
        if (dialogueManager != null)
        {
            dialogueManager.SetPlayerInput(playerInput);
        }
    }
    protected IEnumerator SetCharacterPosition(BaseSpeaker character)
    {
        yield return new WaitForFixedUpdate();
        character.transform.position = respawnPoint.position;
    }

    string GetFormattedSectionName(string name)
    {
        return name.Replace("_", "");
    }
    public void GainTutorialPoint()
    {
        currentSection.tutorialPoints++;
        if (currentSection.tutorialPoints >= currentSection.pointsToContinue)
        {
            EndSection();
            if (currentSection.nextSection != SectionName.Complete)
            {
               StartSection(currentSection.nextSection);
            }
            else
            {
                StartCoroutine(OnTutorialCompleted());
            }
        }
    }

    void StartTutorial()
    {
        currentSection = sectionDict[SectionName.Introduction];
        foreach (var addon in currentSection.sectionAddons)
        {
            addon.OnSectionStarted();
        }
        respawnPoint.position = currentSection.spawnTransform.position;
        sectionDisplay.text = GetFormattedSectionName(currentSection.sectionName.ToString());
        sectionStarted.Invoke(currentSection);
    }
   public void StartSection(SectionName newSection)
    {
        //if (currentSection != null)
        //{
        //    if (currentSection.exitClip != null)
        //    {
        //        tutorialAnimator.SetBool(GetFormattedSectionName(currentSection.sectionName.ToString() + "Complete"), true);
        //    }
        //} 
        currentSection = sectionDict[newSection];
        currentSection.tutorialPoints = 0;


        if (currentSection.sectionName == SectionName.Complete)
        {
           StartCoroutine(OnTutorialCompleted());
            return;
        }
      
        if (currentSection.entryClip != null)
        {
            tutorialAnimator.SetBool(GetFormattedSectionName(currentSection.sectionName.ToString() + "Started"), true);
        }
        
        foreach (var addon in currentSection.sectionAddons)
        {
            addon.OnSectionStarted();
        }
        if (currentSection.spawnTransform) respawnPoint.position = currentSection.spawnTransform.position;
        string sectionName = currentSection.sectionName.ToString();
        sectionName = sectionName.Replace("_", " ");
        sectionDisplay.text = sectionName;
        sectionStarted.Invoke(currentSection);
    }

    void EndSection()
    {
        sectionEnded.Invoke(currentSection);
        currentSection.tutorialPoints = 0;
        foreach (var addon in currentSection.sectionAddons)
        {
            addon.OnSectionEnded();
        }
    }

public void RedoSection()
    {
        foreach (var addon in currentSection.sectionAddons)
        {
            addon.OnSectionRestarted();
        }
        currentSection.tutorialPoints = 0;
        sectionRestarted.Invoke(currentSection);
    }

    protected void OnCharacterDefeated(DamageInfo info, HealthComponent victim)
    {
        if (!victim.hurtboxOwner.TryGetComponent(out BaseSpeaker defeated))
        {
            Debug.Log("Couldn't find base char component");
            return;
        }
        defeated.transform.position = respawnPoint.position;
        defeated.staminaComponent.ResetComponent(false);
        defeated.velocityManager.ResetComponent();
        if (currentSection.failOnDeath)
        {
            RedoSection();
        }
    }

    private IEnumerator OnTutorialCompleted()
    {
        //winScreen.SetActive(true);
       // winText.text = "Tutorial Complete";
        Time.timeScale = 0.0f;
        yield return new WaitForSecondsRealtime(5.0f);
        Time.timeScale = 1.0f;
        ReturnToMainMenu();
    }

    private void Update()
    {
        foreach (var addon in currentSection.sectionAddons)
        {
            addon.SectionLogic();
            if (!addon.sectionCompletable)
            {
                RedoSection();
                break;
            }
        }
    }
}