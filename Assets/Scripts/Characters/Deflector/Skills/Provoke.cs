
using System;
using System.Collections.Generic;
using UnityEngine;
public class Provoke : SpeakerBaseSkill
{
    BaseEcho[] activeEchoes;
    [SerializeField] int tauntDuration = 12;
    [SerializeField] int maxStaminaRecoveryRate = 9;
    int  durationTracker = 0;

    int staminaToRegen = 0;
    int regenTracker = 0;
    AirStateResource.JumpInfo jumpInfo;


    public override void InitState(BaseCharacter cha, CharacterStateMachine s_machine)
    {
        base.InitState(cha, s_machine);
        activeEchoes = FindObjectsByType<BaseEcho>(FindObjectsSortMode.None);
        JumpState jumpState = (JumpState) fsm.TryGetState<JumpState>();
        if (jumpState != null )
        {
            jumpInfo = jumpState.currentJumpInfo;
        }
    }
    public override void Enter(Dictionary<string, object> msg = null)
    {
        base.Enter(msg);
        durationTracker = tauntDuration;
        foreach (var e in activeEchoes)
        {
            e.SetNewTarget(speaker.transform);
            Vector3 dir = speaker.transform.position - e.transform.position;
            float speed = dir.magnitude;
            e.velocityManager.OverwriteInternalSpeed(dir.normalized * speed);
        }
        OnSkillUsed();
    }

    public override void PhysicsProcess()
    {
        durationTracker--;
        if (durationTracker == 0) ExitState();
        if (oppositeSkillBuffer.Buffered)
        {
            fsm.TransitionToSkill(oppositeSkillIndex);
            return;
        }
        Vector3 currentSpeed = speaker.velocityManager.GetInternalSpeed();
        currentSpeed.y -= GetGravity() * Time.fixedDeltaTime;                                                                                                      
        currentSpeed.y = Mathf.Max(jumpInfo.maxFallSpeed, currentSpeed.y);
        speaker.velocityManager.OverwriteInternalSpeed(currentSpeed);
    }

    float GetGravity()
    {
        return speaker.velocityManager.GetTotalSpeed().y > 0 ? jumpInfo.jumpGravity : jumpInfo.fallGravity;
    }


    void ExitState()
    {
        if (!IsGrounded())
        {
            fsm.TransitionTo<FallState>();
        }
        else
        {
            if (GetMovementDir().magnitude < MOVE_DEADZONE)
            {
                fsm.TransitionTo<IdleState>();
            }
            else
            {
                fsm.TransitionTo<RunState>();
            }
        }
    }


    public override void InactivePhysicsProcess()
    {
        if (staminaToRegen == 0) return;
        regenTracker--;
        if (regenTracker == 0)
        {
            regenTracker = maxStaminaRecoveryRate;
            staminaComponent.RegenMaxStamina(1);
            staminaToRegen--;
        }
    }

    public override void OnSkillUsed()
    {
        if (!staminaComponent.HasForesight())
        {
            staminaComponent.DamageStamina(staminaCost, 0, false);
            staminaToRegen += staminaCost;
        }
        else
        {
            staminaComponent.ConsumeForesight();
        }
        staminaComponent.DamageStamina(0, staminaCost, false);
    }

    public override void Exit()
    {
        regenTracker = maxStaminaRecoveryRate;
    }

}
