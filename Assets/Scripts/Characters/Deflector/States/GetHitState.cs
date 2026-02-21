using FishNet.Component.Transforming.Beta;
using System.Collections.Generic;
using UnityEngine;

public class GetHitState : SpeakerBaseState, ISimulationSnapshot<GetHitSnapshot>, ISnapshotable
{
    const float MAX_FALL_SPEED = 15.0f;

    [SerializeField] BufferHelper jumpBuffer;

    [SerializeField] GameObject characterModel;

    [SerializeField] int additionalIFramesPostEchoHit = 5;

    [SerializeField] ParticleSystem hitsparkParticles;

    DamageInfo hitInfo;
    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Speaker; set { } }
    public bool UpdateDuringHitstop { get => true; set { } }

    SimulationManager simulationManager;
    HitstopManager hitstopManager;

    GetHitSnapshot[] getHitSnapshots = new GetHitSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];

    int hitstunRemaining;
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        simulationManager = manager.simulationManager;
        hitstopManager = manager.hitstopManager;
        InitSimulated(simulationManager);
    }
    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {
        base.EnterSimulated(msg);
        bool hasData = false;
        if (msg != null)
        {
            if (msg.TryGetValue("Data", out object data))
            {
                hitInfo = (DamageInfo) data;
                if (hitInfo.hitstunGravity == DamageInfo.USE_DEFAULT_HITSTUN_GRAVITY)  hitInfo.hitstunGravity = DamageInfo.DEFAULT_HITSTUN_GRAVITY;
                hasData = true;
                hitstunRemaining = hitInfo.hitstun;
            }
        } 
        if (!hasData)
        {
            Debug.LogError(character.name + " entered get-hit state without data");
            ExitHitstunState();
            return;
        }
        if (hitInfo.leaveTargetInvincible)
        {
            //InvulnerabilityEffect invulnerabilityEffect = new (DamageSource.Ball, simulationManager.CurrentTick, hitInfo.hitstun + additionalIFramesPostEchoHit);
            InvulnerabilityEffect invulnerabilityEffect = new (DamageSource.Ball, hitInfo.hitstun + additionalIFramesPostEchoHit, simulationManager.CurrentTick, -1);
            speaker.healthComponent.AddStatusEffect(invulnerabilityEffect, StatusEffectIDs.EchoInvulnerability);
        }
    }

    public override void EnterVisuals(Dictionary<string, object> msg = null)
    {
        base.EnterVisuals(msg);
        if (hitsparkParticles != null)
        {
            var newSparks = Instantiate(hitsparkParticles, null);
            newSparks.transform.position = character.transform.position;
            newSparks.Play();
        }
    }
    void ExitHitstunState()
    {
        hitstunRemaining = 0;
        Vector3 currentSpeed = character.velocityManager.GetInternalSpeed();
        if (currentSpeed.y > MAX_FALL_SPEED) currentSpeed.y = MAX_FALL_SPEED;
        character.velocityManager.OverwriteInternalSpeed(currentSpeed);
        if (jumpBuffer.Buffered)
        {
            if (IsGrounded())
            {
                jumpBuffer.Consume();
                fsm.TransitionTo<JumpState>();
                return;
            }
        }  
        else fsm.TransitionTo<FallState>();
    }
    void HitstunLogic()
    {
        if (hitstopManager.InSpecialStop) return;
        hitstunRemaining--;
        if (hitstunRemaining <= 0) ExitHitstunState();
    }

    public void InitSimulated(SimulationManager simulationManager)
    {
        this.simulationManager = simulationManager;
        simulationManager.AddSnapshotableObject(this);
    }

    public override void PhysicsProcess()
    {
        character.velocityManager.AddInternalVelocity(new Vector3(0, -hitInfo.hitstunGravity, 0));
        Vector3 currentSpeed = character.velocityManager.GetInternalSpeed();
        if (currentSpeed.y < -MAX_FALL_SPEED)
        {
            currentSpeed.y = -MAX_FALL_SPEED;
            character.velocityManager.OverwriteInternalSpeed(currentSpeed);
        }

        HitstunLogic();
    }

    public GetHitSnapshot CaptureState()
    {
        return new GetHitSnapshot()
        {
            hitstunLeft = hitstunRemaining
        };
    }

    public void RestoreState(GetHitSnapshot snapshot)
    {
        hitstunRemaining = snapshot.hitstunLeft;
    }

    public void CaptureCurrentState(int tick)
    {
        getHitSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(getHitSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }
}

public struct GetHitSnapshot
{
    public int hitstunLeft;
}