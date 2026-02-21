using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class DeflectManager : MonoBehaviour, ISimulated, ISimulationSnapshot<DeflectSnapshot>, ISnapshotable
{
    public UnityEvent<BaseSpeaker, bool, float> deflectPerformed = new();
    public UnityEvent<BaseSpeaker> superDeflectPerformed;
    public UnityEvent<BaseEcho, bool, bool> deflectedBall;
    public bool deflectEnabled = true;

    [HideInInspector] public bool stateAllowsDeflect = true;


    [SerializeField] BoxCollider deflectHitbox;

    [SerializeField] BaseSpeaker character;

    [SerializeField] MeshRenderer mesh;

    [SerializeField] ParticleSystem partialDeflectBrokenParticles;
    [SerializeField] BufferHelper deflectBuffer;

    [Header("Materials")]
    [SerializeField] Material baseDeflect;
    [SerializeField] Material partialDeflect;
    [SerializeField] Material failedDeflect;

    [Header("Deflect Settings")]
    [SerializeField] int deflectCooldown = 36;
    [SerializeField] int deflectDuration = 66;
    [SerializeField] int partialDeflectDuration = 27;
    [SerializeField] DamageInfo partialDeflectInfo;
    [Header("Deflect Gamefeel")]
    [SerializeField] ParticleSystem deflectSparks;
    [SerializeField] ParticleSystem partialDeflectSparks;
    [SerializeField] ParticleSystem ignitionShockwaves;

    [Header("Sound")]
    [SerializeField] List<AudioClip> deflectSFXList;
    [SerializeField] AudioClip ignitionDeflectSFX;

    int deflectStartTick = 0;

    int cooldownStartTick = 0;

    bool isDeflecting = false;

    bool lockMaterial;

    bool wasDeflectingBeforeFreeze = false;


    Dictionary<string, object> getHitData = new();

    GameManager gameManager;

    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Input; set {} }
    public bool UpdateDuringHitstop { get => false; set { } }

    SimulationManager simulationManager;

    DeflectSnapshot[] deflectSnapshots = new DeflectSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];
    private void Awake()
    {
        getHitData["Data"] = partialDeflectInfo;
        deflectHitbox.enabled = false;
        if (mesh == null)
        {
            mesh = GetComponent<MeshRenderer>();
        }
        character.fsm.transitionedStates.AddListener(OnStateTransitioned);
        partialDeflectBrokenParticles.Stop();
        mesh.enabled = false;
    }

    public void InitManager(GameManager manager)
    {
        gameManager = manager;
        InitSimulated(gameManager.simulationManager);
    }

    public void OnStateTransitioned(CharacterStateMachine.StateTransitionInfo transitionInfo)
    {
        stateAllowsDeflect = !transitionInfo.usingSkill; //if using a skill, can't deflect
        if (transitionInfo.usingSkill)
        {
            if (isDeflecting)
            {
                SetDeflectEnabled(false);
                StartCooldown();
            }
        }
    }
    private void Update()
    {
        if (!lockMaterial && simulationManager != null)
        {
            mesh.material = IsPartialDeflect() ? partialDeflect : baseDeflect;
        }
    }
    public bool DeflectAvailable(int currentTick)
    {
        return
        stateAllowsDeflect
        && !DeflectOnCooldown(currentTick);
    }
    void StartDeflect()
    {
        SetDeflectEnabled(true);
        deflectStartTick = simulationManager.CurrentTick;
    }
    void StartCooldown()
    {
        cooldownStartTick = simulationManager.CurrentTick;
    }
    void DeflectLogic(int currentTick)
    {
        if (gameManager.hitstopManager.InSpecialStop) { return; }

        if (currentTick - deflectStartTick > deflectDuration && isDeflecting)
        {
            SetDeflectEnabled(false);
            StartCooldown();
        }
    }
    public bool IsPartialDeflect()
    {
        return simulationManager.CurrentTick - deflectStartTick >= (simulationManager.CurrentTick - partialDeflectDuration) && IsDeflecting();
    }
    public bool IsDeflecting()
    {
        return isDeflecting;
    }
    public bool DeflectOnCooldown(int currentTick)
    {
        return currentTick - cooldownStartTick > deflectCooldown && cooldownStartTick > 0;
    }
    public int GetGoodDeflectDuration()
    {
        return deflectDuration - partialDeflectDuration;
    }
    public void SetDeflectEnabled(bool enabled)
    {
        deflectHitbox.enabled = enabled;
        mesh.enabled = enabled;
        isDeflecting = enabled;
    }
    public void OnSuccessfulDeflect(BaseEcho ball, bool usedSkill) 
    {
        int currentTick = simulationManager.CurrentTick;
        bool wasPartial = IsPartialDeflect();
        deflectPerformed.Invoke(character, wasPartial, (currentTick - deflectStartTick) / deflectDuration);
        deflectedBall.Invoke(ball, IsPartialDeflect(), usedSkill);
        SetDeflectEnabled(false);
        if (wasPartial || character.fsm.currentState is GetHitState)
        {
            partialDeflectSparks.Play();
            partialDeflectInfo.knockbackDir = (ball.transform.position - character.transform.position).normalized;
            character.fsm.TransitionTo<GetHitState>(getHitData);
        }
        else deflectSparks.Play();
        character.unscaledAudioSource.PlayOneShot(GetRandomDeflectSFX());
        if (ball.isIgnited)
        {
            ignitionShockwaves.Play();
            character.unscaledAudioSource.PlayOneShot(ignitionDeflectSFX);
        }
       if (!usedSkill) ball.transform.position = transform.position; //hide tunnelling
    }
    public void OnDeflectBroken()
    {
        partialDeflectBrokenParticles.Play();
    }
    public void OnSpecialStopStarted()
    {
       wasDeflectingBeforeFreeze = IsDeflecting();
       var skillOne = character.fsm.TryGetSkill(1);
       if (skillOne != null) skillOne.OnSpecialStopStarted();
       var skillTwo = character.fsm.TryGetSkill(2);
       if (skillTwo != null) skillTwo.OnSpecialStopStarted();
    }
    public void ResetComponent()
    {
        SetDeflectEnabled(false);
    }
    public AudioClip GetRandomDeflectSFX()
    {
        int index = Random.Range(0, deflectSFXList.Count);
        return deflectSFXList[index];
    }
    public void SimulateUpdate(int currentTick)
    {
        if (deflectBuffer.Buffered)
        {
            if (!wasDeflectingBeforeFreeze && (gameManager.hitstopManager.InSpecialStop || gameManager.hitstopManager.FrameAfterSpecialStop))
            {
                return; //can't deflect during freeze
            }
            deflectBuffer.Consume();
            bool isNowDeflecting = false; //if you weren't deflecting before, but you now are
            if (DeflectAvailable(currentTick) && !isDeflecting)
            {
                StartDeflect();
                isNowDeflecting = true;
            }
            if (isDeflecting && !isNowDeflecting) //!isNowDeflecting means you didn't trigger a deflect with this input, so you must be trying to cancel
            {
                StartCooldown();
                SetDeflectEnabled(false);
            }
        }
        DeflectLogic(currentTick);
    }
    public DeflectSnapshot CaptureState()
    {
        return new DeflectSnapshot
        {
            startTick = deflectStartTick,
            cooldownTick = cooldownStartTick,
            deflectActive = isDeflecting,
            deflectingBeforeFreeze = wasDeflectingBeforeFreeze,
        };
    }
    public void RestoreState(DeflectSnapshot snapshot)
    {
        deflectStartTick = snapshot.startTick;
        isDeflecting = snapshot.deflectActive;
        wasDeflectingBeforeFreeze = snapshot.deflectingBeforeFreeze;
        cooldownStartTick = snapshot.cooldownTick;
    }
    public void InitSimulated(SimulationManager simulationManager)
    {
        this.simulationManager = simulationManager;
        simulationManager.AddSimulatedObject(this);
    }

    public void CaptureCurrentState(int tick)
    {
        deflectSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(deflectSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }
}
public struct DeflectSnapshot
{
    public int startTick;
    public int cooldownTick;
    public bool deflectActive;
    public bool deflectingBeforeFreeze;
}