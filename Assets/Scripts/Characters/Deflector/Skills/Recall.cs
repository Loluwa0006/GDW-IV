using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Recall : SpeakerBaseSkill
{
    [SerializeField] RecallBlade blade;
    [SerializeField] Collider bladeCollider;

    [Header("Stamina Attributes")]
    [SerializeField] int activeBladeDrainRate = 9;
    [SerializeField] int warpCost = 10;
    [Header("Pulse Attributes")]
    [SerializeField] Collider hitbox;
    [SerializeField] DamageInfo hitboxInfo;
    [SerializeField] int warpPulseActiveFrames = 7;
    [SerializeField] LayerMask pulseMask;
    [Header("Holster Attributes")]
    [SerializeField] LayerMask holsterMask;
    [SerializeField] int framesUntilHolsterAllowed = 8;

    List<HealthComponent> struckEntities = new();

    bool releasedButton = true;

    int drainTracker = 9;
    int hitboxActiveFramesRemaining = 0;
    int framesRemainingUntilHolsterAllowed = 8;

    BaseSpeaker enemySpeaker;

    public override void InitState(BaseCharacter cha, CharacterStateMachine s_machine)
    {
        base.InitState(cha, s_machine);
        StartCoroutine(FindOppositeSpeaker());
        blade.Holster();
    }
    public override void Enter(Dictionary<string, object> msg = null)
    {
        releasedButton = false;
        framesRemainingUntilHolsterAllowed = framesUntilHolsterAllowed;
        skillBuffer.Consume();
        if (blade.status == RecallBlade.BladeState.Holstered)
        {
            OnSkillUsed();
            Vector3 throwDir = GetMovementDir();
            if (throwDir.magnitude < MOVE_DEADZONE) throwDir = enemySpeaker.transform.position - speaker.transform.position;
            blade.ThrowBlade(throwDir);
        }
        else
        {
            if (!staminaComponent.HasForesight()) staminaComponent.DamageStamina(warpCost, 0, false);
            else staminaComponent.ConsumeForesight();
            TeleportToBlade();
        }
        OnSkillOver();
    }

    IEnumerator FindOppositeSpeaker()
    {
        yield return new WaitForFixedUpdate();
        var speakers = FindObjectsByType<BaseSpeaker>(FindObjectsSortMode.None);
        foreach (var speaker in speakers)
        {
            if (speaker == character) { continue; }
            Debug.Log(character.name + " is looking at char " + speaker.name);
            enemySpeaker = speaker;
            break;
        }
    }
    public override void InactivePhysicsProcess()
    {
        blade.PhysicsUpdate();
        if (blade.status != RecallBlade.BladeState.Holstered) DrainLogic();
        if (staminaComponent.GetStamina() < staminaCost) blade.Holster();

        Vector3 moveDir = GetMovementDir();
        if (skillAction.IsPressed() && CanSteer(moveDir)) blade.SteerFlight(moveDir);
        if (hitboxActiveFramesRemaining > 0) HitboxLogic();
        if (blade.status != RecallBlade.BladeState.Holstered) HolsterLogic();
        if (framesRemainingUntilHolsterAllowed > 0) framesRemainingUntilHolsterAllowed--;
    }
    public override void InactiveProcess()
    {
        if (!skillAction.IsPressed()) releasedButton = true;
        blade.Process();
    }

    public override void ResetSkill()
    {
        blade.Holster();
    }

    void DrainLogic()
    {
        drainTracker--;
        if (drainTracker <= 0)
        {
            drainTracker = activeBladeDrainRate;
            if (!staminaComponent.HasForesight()) staminaComponent.DamageStamina(1, 0, false);
        }
    }

    void HitboxLogic()
    {
        var overlap = Physics.OverlapBox(hitbox.bounds.center, hitbox.bounds.size, speaker.transform.rotation, pulseMask, QueryTriggerInteraction.Collide);
        foreach (var hurtbox in overlap)
        {
            if (!hurtbox.TryGetComponent(out HealthComponent hp)) continue;
            else if (struckEntities.Contains(hp)) continue;
            else if (hp == speaker.healthComponent) continue;
            hp.Damage(hitboxInfo);
            struckEntities.Add(hp);
        }
        hitboxActiveFramesRemaining--;
        if (hitboxActiveFramesRemaining < 0) hitbox.enabled = false;
    }

    void TeleportToBlade()
    {
        Vector3 tpSpot = blade.transform.position;
        speaker.transform.position = tpSpot;
        struckEntities.Clear();
        hitbox.enabled = true;
        hitboxActiveFramesRemaining = warpPulseActiveFrames;
        blade.Holster();
    }

    bool CanSteer(Vector3 moveDir)
    {
        return !releasedButton
              && blade.status == RecallBlade.BladeState.Flying
              && moveDir.magnitude > MOVE_DEADZONE;
    }

    public void HolsterLogic()
    {
        if (framesRemainingUntilHolsterAllowed > 0) return;
        var overlap = Physics.OverlapBox(bladeCollider.bounds.center, bladeCollider.bounds.size, transform.rotation, holsterMask);
        foreach (Collider c in overlap)
        {
            Debug.Log("Found collider " + c.name);
            if (c.TryGetComponent(out BaseSpeaker detectedSpeaker))
            {
                if (detectedSpeaker != speaker) continue;
                blade.Holster();
            }
            else if (c.TryGetComponent(out DeathBox deathbox))
            {
                blade.Holster();
            }
        }
    }

    public override bool SkillAvailable()
    {
        if (blade.status == RecallBlade.BladeState.Deactivated) return false;

        int staCost = blade.status == RecallBlade.BladeState.Holstered ? staminaCost : warpCost;
        return (staminaComponent.HasForesight() || staminaComponent.GetStamina() > staCost);
    }
}