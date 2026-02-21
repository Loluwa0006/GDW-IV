using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GetHitState : SpeakerBaseState, ISimulated
{
    const float MAX_FALL_SPEED = 15.0f;

    [SerializeField] BufferHelper jumpBuffer;

    [SerializeField] GameObject characterModel;

    [SerializeField] int additionalIFramesPostEchoHit = 5;

    [SerializeField] ParticleSystem hitsparkParticles;

    DamageInfo hitInfo;

    int tickWhereHitOccured = 0;
    GameManager gameManager;

    public ISimulated.PriorityIndex Priority { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public bool UpdateDuringHitstop { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    SimulationManager simulationManager;
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        gameManager = manager;
    }

    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {
        base.EnterSimulated(msg);
        bool hasData = false;
        if (msg != null)
        {
            if (msg.TryGetValue("Data", out object data))
            {
                hitInfo = (DamageInfo)data;
                if (hitInfo.hitstunGravity == DamageInfo.USE_DEFAULT_HITSTUN_GRAVITY)  hitInfo.hitstunGravity = DamageInfo.DEFAULT_HITSTUN_GRAVITY;
                hasData = true;
                tickWhereHitOccured = simulationManager.CurrentTick;
            }
        } 
        if (!hasData)
        {
            Debug.LogError( character.name + " entered get-hit state without data");
            ExitHitstunState();
            return;
        }
        if (hitInfo.leaveTargetInvincible)
        {
            //InvulnerabilityEffect invulnerabilityEffect = new (DamageSource.Ball, simulationManager.CurrentTick, hitInfo.hitstun + additionalIFramesPostEchoHit);
            InvulnerabilityEffect invulnerabilityEffect = new InvulnerabilityEffect(DamageSource.Ball, hitInfo.hitstun + additionalIFramesPostEchoHit, simulationManager.CurrentTick, -1);
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
        Vector3 currentSpeed = character.velocityManager.GetInternalSpeed();
        if (currentSpeed.y > MAX_FALL_SPEED)
        {
            currentSpeed.y = MAX_FALL_SPEED;
        }
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
        else
        {
            fsm.TransitionTo<FallState>();
        }
    }
    void HitstunLogic(int currentTick)
    {
        if (gameManager.hitstopManager.InSpecialStop) { return; }
        if (currentTick - tickWhereHitOccured > hitInfo.hitstun)
        {
            ExitHitstunState();
        }
    }

    public void InitSimulated(SimulationManager simulationManager)
    {
        this.simulationManager = simulationManager;
        simulationManager.AddSimulatedObject(this);
    }

    public void SimulateUpdate(int currentTick)
    {
        character.velocityManager.AddInternalVelocity(new Vector3(0, -hitInfo.hitstunGravity, 0));
        Vector3 currentSpeed = character.velocityManager.GetInternalSpeed();
        if (currentSpeed.y < -MAX_FALL_SPEED)
        {
            currentSpeed.y = -MAX_FALL_SPEED;
            character.velocityManager.OverwriteInternalSpeed(currentSpeed);
        }

        HitstunLogic(currentTick);
    }
}
