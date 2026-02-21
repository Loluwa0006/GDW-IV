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

    public bool IsDeflecting { private set; get; } = false;

    bool lockMaterial;

    bool wasDeflectingBeforeFreeze = false;

    Dictionary<string, object> getHitData = new();

    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Input; set {} }
    public bool UpdateDuringHitstop { get => false; set { } }

    SimulationManager simulationManager;
    HitstopManager hitstopManager;

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
        simulationManager = manager.simulationManager;
        hitstopManager = manager.hitstopManager;
        InitSimulated(simulationManager);
    }

    public void OnStateTransitioned(CharacterStateMachine.StateTransitionInfo transitionInfo)
    {
        stateAllowsDeflect = !transitionInfo.usingSkill; //if using a skill, can't deflect
        if (transitionInfo.usingSkill)
        {
            if (IsDeflecting)
            {
                SetDeflectEnabled(false);
                StartCooldown();
            }
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
        if (hitstopManager.InSpecialStop) return; 

        if (currentTick - deflectStartTick > deflectDuration && IsDeflecting)
        {
            SetDeflectEnabled(false);
            StartCooldown();
        }
    }
    public bool IsPartialDeflect()
    {
        int elasped = simulationManager.CurrentTick - deflectStartTick;

        return elasped > (deflectDuration - partialDeflectDuration) && IsDeflecting;
    }
    public bool DeflectOnCooldown(int currentTick)
    {
        return currentTick - cooldownStartTick <= deflectCooldown && cooldownStartTick > 0;
    }
    public int GetGoodDeflectDuration()
    {
        return deflectDuration - partialDeflectDuration;
    }
    public void SetDeflectEnabled(bool enabled)
    {
        deflectHitbox.enabled = enabled;
        mesh.enabled = enabled;
        IsDeflecting = enabled;
    }
    public void OnSuccessfulDeflect(BaseEcho ball, bool usedSkill) 
    {
        int currentTick = simulationManager.CurrentTick;
        bool wasPartial = IsPartialDeflect() || character.fsm.currentState is GetHitState;
        deflectPerformed.Invoke(character, wasPartial, (currentTick - deflectStartTick) / deflectDuration);
        deflectedBall.Invoke(ball, IsPartialDeflect(), usedSkill);
        SetDeflectEnabled(false);
        if (wasPartial)
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
       wasDeflectingBeforeFreeze = IsDeflecting;
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
            if (!wasDeflectingBeforeFreeze && (hitstopManager.InSpecialStop || hitstopManager.FrameAfterSpecialStop))
            {
                return; //can't deflect during freeze
            }
            deflectBuffer.Consume();
            bool isNowDeflecting = false; //if you weren't deflecting before, but you now are
            if (DeflectAvailable(currentTick) && !IsDeflecting)
            {
                StartDeflect();
                isNowDeflecting = true;
            }
            if (IsDeflecting && !isNowDeflecting) //!isNowDeflecting means you didn't trigger a deflect with this input, so you must be trying to cancel
            {
                StartCooldown();
                SetDeflectEnabled(false);
            }
        }
        DeflectLogic(currentTick);

        int elasped = currentTick - deflectStartTick;

        if (IsDeflecting)
        {
            Debug.Log("In deflect state for " + elasped + " frames");
            Debug.Log("Partial deflecting == " + IsPartialDeflect());
        }

        if (!lockMaterial) mesh.material = IsPartialDeflect() ? partialDeflect : baseDeflect;

    }
    public DeflectSnapshot CaptureState()
    {
        return new DeflectSnapshot
        {
            startTick = deflectStartTick,
            cooldownTick = cooldownStartTick,
            deflectActive = IsDeflecting,
            deflectingBeforeFreeze = wasDeflectingBeforeFreeze,
        };
    }
    public void RestoreState(DeflectSnapshot snapshot)
    {
        deflectStartTick = snapshot.startTick;
        IsDeflecting = snapshot.deflectActive;
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