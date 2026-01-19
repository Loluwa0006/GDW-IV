using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Dash : SpeakerBaseSkill
{

    const float DASH_DEADZONE_REQUIREMENT = 0.2f;

    [SerializeField] int dashDistance = 10;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField, Range(0, 1)] float speedMaintained;
    [SerializeField] ParticleSystem dashParticles;

    [Header("SFX")]
    [SerializeField] AudioClip whooshClip;

    [Header("Tackle")]
    [SerializeField] HitboxComponent hitbox;
    [SerializeField] LayerMask tackleMask;
    float dashSpeed;
    float dashTracker;

    Vector3 dashDir = Vector3.zero;
    List<HealthComponent> struckTargets = new();


    public override void InitState(BaseCharacter cha, CharacterStateMachine s_machine)
    {
        base.InitState(cha, s_machine);
        dashSpeed = dashDistance / dashDuration;
        SetDashParticleEmission(false);
    }

    public override void Enter(Dictionary<string, object> msg = null)
    {
        base.Enter(msg);
        struckTargets.Clear();
        hitbox.hitboxCollider.enabled = true;
        dashDir = GetMovementDir().normalized;
        dashTracker = 0;
        base.OnSkillUsed();
        character.velocityManager.OverwriteInternalSpeed(dashDir * dashSpeed);
        hitbox.transform.rotation.SetLookRotation(dashDir);
        hitbox.damageInfo.knockbackDir = character.velocityManager.GetInternalSpeed().normalized;

        SetDashParticleEmission(true);
        if (whooshClip != null) character.unscaledAudioSource.PlayOneShot(whooshClip);
    }
    

    void SetDashParticleEmission(bool value)
    {
        var emission = dashParticles.emission;
        emission.enabled = value;
    }

     void HitboxCollisionLogic()
    {
        var overlap = Physics.OverlapBox(hitbox.hitboxCollider.bounds.center, hitbox.hitboxCollider.bounds.size, hitbox.transform.rotation, tackleMask, QueryTriggerInteraction.Collide);
        List<HealthComponent> newVictims = new();
        foreach (var obj in overlap)
        {
            if (!obj.transform.TryGetComponent(out HealthComponent hp)) continue;
            else if (hp == speaker.healthComponent) continue;
            else if (struckTargets.Contains(hp)) continue;
            Debug.Log("Found tackle victim: " + hp.hurtboxOwner.name);
            struckTargets.Add(hp);
            newVictims.Add(hp);
        }
        bool hitEntity = false;
        foreach (var victim in newVictims)
        {
            Debug.Log("Tackling " + victim.name + " with dash");
            victim.Damage(hitbox.damageInfo);
            if (victim.ownedByEntity) hitEntity = true;
        }
        if (hitEntity)  GameManager.ApplySpecialStop(hitbox.damageInfo.hitstop);
    }
    public override void PhysicsProcess()
    {
        dashTracker += Time.fixedDeltaTime;
        if (dashTracker >= dashDuration)
        {
            character.velocityManager.OverwriteInternalSpeed(dashSpeed * speedMaintained * dashDir);
            OnSkillOver();
        }

        dashParticles.transform.rotation = Quaternion.LookRotation(-dashDir);
        HitboxCollisionLogic();
    }

    public override void Process()
    {
        CancelSkillIfOppositeSkillBuffered();
    }
    public override void Exit()
    {
        SetDashParticleEmission(false);
        hitbox.hitboxCollider.enabled = false;
    }

    public override bool SkillAvailable()
    {
        bool hasStamina = staminaComponent.GetStamina() > staminaCost || staminaComponent.HasForesight();
        return hasStamina && GetMovementDir().magnitude > DASH_DEADZONE_REQUIREMENT;
    }
}
