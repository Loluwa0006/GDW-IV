
using System;
using System.Collections.Generic;
using UnityEngine;
public class Provoke : SpeakerBaseSkill
{
    BaseEcho[] activeEchoes;
    [SerializeField] int tauntDuration = 12;
    [SerializeField] int maxStaminaRecoveryRate = 9;
    [SerializeField] GameObject warningCanvas;
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
        warningCanvas.SetActive(false);
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
        warningCanvas.SetActive(true);
    }

    public override void PhysicsProcess()
    {
        durationTracker--;
        if (durationTracker == 0) OnSkillOver();
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

    public override void InactivePhysicsProcess()
    {
        if (staminaToRegen <= 0) return;
        regenTracker--;
        if (regenTracker == 0)
        {
            regenTracker = maxStaminaRecoveryRate;
            int staToRegen = staminaComponent.HasForesight() ? 2 : 1;
            staminaComponent.RegenMaxStamina(staToRegen);
            staminaToRegen -= staToRegen;
        }
        Debug.Log(staminaToRegen + " = stamina to regen");
    }

    protected override void OnSkillUsed()
    {
        staminaComponent.DamageStamina(0, staminaCost, false);
        staminaToRegen += staminaCost;
    }

    public override void Exit()
    {
        regenTracker = maxStaminaRecoveryRate;
        warningCanvas.SetActive(false);
    }

}
