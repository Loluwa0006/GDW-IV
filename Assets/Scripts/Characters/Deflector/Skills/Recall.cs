using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recall : SpeakerBaseSkill, ITackleSkill
{
    [SerializeField] RecallBlade blade;
    [SerializeField] Collider bladeCollider;
    [SerializeField] ParticleSystem warpEffect;

    [Header("Stamina Attributes")]
    [SerializeField] int activeBladeDrainRate = 9;
    [SerializeField] int warpCost = 10;
    [Header("Pulse Attributes")]
    [SerializeField] HitboxComponent hitbox;
    [SerializeField] DamageInfo hitboxInfo;
    [SerializeField] int warpPulseActiveFrames = 7;
    [SerializeField] LayerMask pulseMask;
    [Header("Holster Attributes")]
    [SerializeField] LayerMask holsterMask;
    [SerializeField] int framesUntilHolsterAllowed = 8;

    List<int> struckEntities = new();

    bool releasedButton = true;
    int drainTracker = 9;
    int hitboxActiveFramesRemaining = 0;
    int framesRemainingUntilHolsterAllowed = 8;

    BaseSpeaker enemySpeaker;

    Rigidbody _rb;


    int tackleStartTick = 0;
    public List<int> StruckTargets { get => struckEntities; set => struckEntities = value; }
    public int TackleDuration { get => warpPulseActiveFrames; set => warpPulseActiveFrames = value ; }
    public int TackleStartTick { get => tackleStartTick; set => tackleStartTick = value; }

    ITackleSkill tackleManager;


    GameManager gameManager;
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        gameManager = manager;
        base.InitState(cha, fsm, manager);
        StartCoroutine(FindOppositeSpeaker());
        blade.Holster();
        _rb = speaker.GetComponent<Rigidbody>();
        tackleManager = this;
    }
    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {
        releasedButton = false;
        framesRemainingUntilHolsterAllowed = framesUntilHolsterAllowed;
        skillBuffer.Consume();
        if (blade.status == RecallBlade.BladeState.Holstered)
        {
            OnSkillUsed();
            Vector3 throwDir = GetMovementDir();
            if (throwDir.magnitude < MOVE_DEADZONE) throwDir = (enemySpeaker.transform.position - speaker.transform.position).normalized;
            blade.ThrowBlade(throwDir);
        }
        else
        {
            if (!staminaComponent.ForesightEnabled) staminaComponent.DamageStamina(warpCost, 0, false);
            else staminaComponent.ConsumeForesight();
            TeleportToBlade();
        }
        OnSkillOver();
    }

    IEnumerator FindOppositeSpeaker()
    {
        yield return new WaitForFixedUpdate();
        var speakers = FindObjectsByType<BaseSpeaker>(FindObjectsSortMode.InstanceID);
        foreach (var speaker in speakers)
        {
            if (speaker == character) continue; 
            enemySpeaker = speaker;
            break;
        }
    }
    public override void InactivePhysicsProcess()
    {
        blade.PhysicsUpdate();
        if (blade.status != RecallBlade.BladeState.Holstered) DrainLogic();
        if (staminaComponent.Stamina < staminaCost) blade.Holster();

        Vector3 moveDir = GetMovementDir();
        if (skillAction.IsPressed() && CanSteer(moveDir)) blade.SteerFlight(moveDir);
        if (hitboxActiveFramesRemaining > 0) tackleManager.HitboxCollisionLogic(hitbox, gameManager.hitstopManager, pulseMask, speaker);
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
            if (!staminaComponent.ForesightEnabled) staminaComponent.DamageStamina(1, 0, false);
        }
    }

    
    void TeleportToBlade()
    {
        Vector3 tpSpot = blade.transform.position;
        _rb.Move(tpSpot, _rb.transform.rotation);
        struckEntities.Clear();
        hitbox.enabled = true;
        hitboxActiveFramesRemaining = warpPulseActiveFrames;
        warpEffect.Play();
        framesRemainingUntilHolsterAllowed = 1; // holster next frame for safety
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
        return (staminaComponent.ForesightEnabled || staminaComponent.Stamina > staCost);
    }
}