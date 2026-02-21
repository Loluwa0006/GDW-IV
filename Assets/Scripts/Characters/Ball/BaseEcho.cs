using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(Rigidbody))]

public class BaseEcho : BaseCharacter, ISimulationSnapshot<EchoSnapshot>, ISnapshotable, ISimulated
{
    public HashSet<Transform> viableTargets = new();
    public UnityEvent<BaseEcho> echoCollision = new();
    public UnityEvent<BaseEcho> echoDeflected = new();
    public UnityEvent<Vector3> echoWarped = new();
    public UnityEvent<Transform> echoTargetChanged = new();

    [HideInInspector] public bool ballActive = false;
    [HideInInspector] public bool isIgnited = false;
    [SerializeField] TrailRenderer echoTrail;
    [SerializeField] EchoParticleManager particleManager;
   
    public bool playerControlled = false;
    public Rigidbody _rb;

    [SerializeField]  EchoDataResource echoData;

    protected Transform currentTarget;

    Vector3 resetPos;

    HitstopManager hitstopManager;

    public EchoSnapshot[] echoSnapshots = new EchoSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];

    /// <summary>
    /// For creating a player controlled echo.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="manager"></param>
    /// <param name="index"></param>
    public override void InitPlayer(MatchData.PlayerInfo info, GameManager manager, int index)
    {
        teamIndex = index;
        name = "Echo " + index;
        groundIndicator.Init(playerColors[index - 1], index);
     
        InitStateMachine(info, manager);

        gameManager = manager;
        hitstopManager = gameManager.hitstopManager;
        characterID = manager.entityManager.RegisterEntity(EntityDatabaseID.Echo, transform);
        InitSimulated(manager.simulationManager);
    }

    protected override void InitStateMachine(MatchData.PlayerInfo info, GameManager manager)
    {
        if (playerControlled && info != null)
        {
            inputManager.InitInputComponent(info);        //must do this first for state machine buffers, otherwise they will assume kb 1 speaker controls
            fsm.CreateSkills(info);
        }
        fsm.InitMachine(manager);
        init = true;
    }
    /// <summary>
    /// For creating an AI controlled echo.
    /// </summary>
    /// <param name="charList"></param>
    /// <param name="startingPos"></param>
    /// <param name="manager"></param>
   public virtual void InitProjectile(HashSet<Transform> charList, Vector3 startingPos, GameManager manager)
    {
        playerControlled = false;
        if (inputManager == null) inputManager = GetComponentInChildren<InputManager>();
        
        if (unscaledAudioSource == null) unscaledAudioSource = GetComponent<AudioSource>();
        

        viableTargets = charList;
        currentTarget = viableTargets.ElementAt(0);
        transform.position = startingPos;
        resetPos = startingPos;

        unscaledAudioSource.outputAudioMixerGroup.audioMixer.updateMode = UnityEngine.Audio.AudioMixerUpdateMode.UnscaledTime;

        echoData.InitData();

        InitStateMachine(null, manager);
        velocityManager.InitManager(manager);

        gameManager = manager;
        hitstopManager = gameManager.hitstopManager;
        characterID = manager.entityManager.RegisterEntity(EntityDatabaseID.Echo, transform);
        InitSimulated(manager.simulationManager);
    }
    public void EnableProjectile()
    {
        transform.position = resetPos;
        UpdateSpeed(echoData.activeMinSpeed);
        ResumeProjectile();
    }

    public void ResumeProjectile()
    {
        playerModel.enabled = true;
        ballActive = true;
        fsm.TransitionTo<FlyingState>();
        velocityManager.freeze = false;
    }

    public void ResetProjectile()
    {
        transform.position = resetPos;
        playerModel.enabled = false;
        ballActive = false;
        fsm.TransitionTo<FlyingState>();
        velocityManager.ResetComponent();
        echoData.InitData();
        UpdateSpeed(echoData.minSpeed);
    }

    public override void ActivatePlayer()
    {
        if (!playerControlled)
        {
            playerModel.enabled = true;
            ballActive = true;

            echoData.InitData();
        }
        else
        {
            base.ActivatePlayer();
        }
    }

    public override void DeactivatePlayer()
    {
        if (playerControlled) base.DeactivatePlayer();
    }

    public void SuspendProjectile(bool hide = true, bool hitboxActive = false)
    {
        playerModel.enabled = !hide;
        ballActive = hitboxActive;
        velocityManager.freeze = true;
    }

    public override void SimulateUpdate(int currentTick)
    {
        if (hitstopManager == null) return;
        if (hitstopManager.InSpecialStop || !ballActive || currentTarget == null)  return;
        playerModel.transform.LookAt(currentTarget.transform.position);
        fsm.SimulateUpdate(currentTick);
    }
    private void Update()
    {
        if (hitstopManager == null) return;
        if (hitstopManager.InSpecialStop || !ballActive || currentTarget == null)  return; 
        fsm.UpdateState();
    }


    public virtual void FindNewTarget(Transform lastHitCharacter)
    {
        if (viableTargets.Count <= 1)
        {
            Debug.LogWarning("Echo " + name + " has no viable targets to switch to!");
            return;
        }
        HashSet<Transform> targetList = new (viableTargets);
        targetList.Remove(lastHitCharacter);
        int randomIndex = Random.Range(0, targetList.Count);
        currentTarget = targetList.ElementAt(randomIndex);
        echoTargetChanged.Invoke(currentTarget);
    }

    public void SetNewTarget(Transform target)
    {
        if (target == currentTarget) return;
        currentTarget = target;
        echoTargetChanged.Invoke(target);
    }

    public Transform GetTarget()
    {
        return currentTarget;
    }

    public float GetSpeed()
    {
        return echoData.currentSpeed;
    }


    public void EnterSuddenDeath()
    {
        echoData.activeMinSpeed = echoData.igniteSpeed;
       if ( fsm.currentState.TryGetComponent(out EchoBaseState state) )
        {
            state.OnBallIgnited();
        }
    }

    public void WarpToLocation(Vector3 pos)
    {
        Vector3 previousPos = transform.position;
        if (transform.parent == null) transform.position = pos;
        echoWarped.Invoke(pos - previousPos);
        echoTrail.Clear();
    }

    public virtual void UpdateSpeed(float newSpeed)
    {
        if (echoData.currentSpeed == newSpeed) return;
        echoData.currentSpeed = Mathf.Clamp(newSpeed, echoData.activeMinSpeed, echoData.activeMaxSpeed);
        isIgnited = (echoData.currentSpeed >= echoData.igniteSpeed);
        particleManager.OnSpeedUpdated(echoData.currentSpeed, isIgnited);
    }

    public void ForceDeflect(BaseSpeaker speaker)
    {
        var msg = new Dictionary<string, object>()
        {
            ["deflector"] = speaker,
            ["usedSkill"] = true,  
        };
        fsm.TransitionTo<DeflectionBounceState>(msg);
    }

    public EchoSnapshot CaptureState()
    {
        CharacterSnapshot charSnap = new(enabled, init);

        return new EchoSnapshot()
        {
            ignited = isIgnited,
            active = ballActive,
            speed = GetSpeed(),
            maxSpeed = echoData.maxSpeed,
            minSpeed = echoData.minSpeed,
            targetID = gameManager.entityManager.GetId(currentTarget),
            characterSnapshot = charSnap
        };
    }

    public void RestoreState(EchoSnapshot snapshot)
    {
        currentTarget = gameManager.entityManager.GetEntity(snapshot.targetID);
        isIgnited = snapshot.ignited;
        ballActive = snapshot.active;
        UpdateSpeed(snapshot.speed);
        echoData.maxSpeed = snapshot.maxSpeed;
        echoData.minSpeed = snapshot.minSpeed;

    }

    public void CaptureCurrentState(int tick)
    {
        echoSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(echoSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }
}

public struct EchoSnapshot
{
    public bool ignited;
    public bool active;
    public float speed;
    public float minSpeed;
    public float maxSpeed;
    public int targetID;
    public CharacterSnapshot characterSnapshot;
}


