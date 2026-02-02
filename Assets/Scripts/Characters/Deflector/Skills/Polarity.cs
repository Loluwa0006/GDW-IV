using System.Collections.Generic;
using UnityEngine;

public class Polarity : SpeakerBaseSkill
{
    [SerializeField] int grenadeModeSwapStaminaCost = 10; 
    [SerializeField] float throwDistance = 20.0f;
    [SerializeField] PolarityGrenade grenade;
    [SerializeField] VelocityManager grenadeVelocityManager;

    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm)
    {
        base.InitState(cha, fsm);
        grenade.InitProjectile();
    }

    public override void Enter(Dictionary<string, object> msg = null)
    {
        base.Enter(msg);
        Vector3 throwDir = GetMovementDir();
        if (throwDir.magnitude <= MOVE_DEADZONE) throwDir = GetDirectionToNearestSpeaker();
        Vector3 speakerSpeed = character.velocityManager.GetTotalSpeed();
        grenadeVelocityManager.OverwriteInternalSpeed((throwDir * throwDistance) + speakerSpeed);
        grenade.transform.position = character.transform.position;
        grenade.OnGrenadeThrown();
        OnSkillUsed();
        OnSkillOver();
        skillBuffer.Consume();
    }


    Vector3 GetDirectionToNearestSpeaker()
    {
        var speakers = FindObjectsByType<BaseSpeaker>(FindObjectsSortMode.InstanceID);
        float distanceToBeat = int.MaxValue;
        BaseSpeaker closestSpeaker = null;
        foreach (var speaker in speakers)
        {
            if (!speaker.healthComponent.IsAlive()) continue;
            float currentDistance = Vector3.Distance(speaker.transform.position, character.transform.position);
            if (currentDistance < distanceToBeat)
            {
                distanceToBeat = currentDistance;
                closestSpeaker = speaker;
            }
        }
        if (closestSpeaker != null) return  (closestSpeaker.transform.position - character.transform.position).normalized;
        return GetMovementDir();
    }



    public override void InactiveProcess()
    {
        var hasForesight = staminaComponent.HasForesight();
        if (staminaComponent.GetStamina() <= grenadeModeSwapStaminaCost && !hasForesight) return;
        if (skillBuffer.Buffered)
        {
            if (ModeSwappable())
            {
                if (hasForesight) staminaComponent.ConsumeForesight();
                else staminaComponent.DamageStamina(grenadeModeSwapStaminaCost, 0, false);

                grenade.SwapMode();
            }
            else if (grenade.state == PolarityGrenade.GrenadeState.Travelling)
            {
                grenade.ActivateGrenade();
            }
            skillBuffer.Consume();
        }
    }

    public override void InactivePhysicsProcess()
    {
        grenade.UpdateProjectile();
    }

    bool ModeSwappable()
    {
        return (grenade.state == PolarityGrenade.GrenadeState.Attracting || grenade.state == PolarityGrenade.GrenadeState.Repulsing);
    } 
    public override bool SkillAvailable()
    {
        return (staminaComponent.GetStamina() > staminaCost || staminaComponent.HasForesight()) && grenade.state == PolarityGrenade.GrenadeState.Holstered;
    }

    public override void ResetSkill()
    {
        grenade.HolsterGrenade();
    }
}
