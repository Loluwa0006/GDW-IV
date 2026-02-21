using System.Collections.Generic;
using UnityEngine;
public class Dash : SpeakerBaseSkill, ITackleSkill, ISimulated, ISimulationSnapshot<DashSnapshot>, ISnapshotable
{

    const float DASH_DEADZONE_REQUIREMENT = 0.2f;

    [SerializeField] int dashDistance = 10;
    [SerializeField] int dashDuration = 12;
    [SerializeField] int tackleDuration = 7;
    [SerializeField, Range(0, 1)] float speedMaintained;
    [SerializeField] ParticleSystem dashParticles;

    [Header("SFX")]
    [SerializeField] AudioClip whooshClip;

    [Header("Tackle")]
    [SerializeField] HitboxComponent hitbox;
    [SerializeField] LayerMask tackleMask;
    float dashSpeed;

    Vector3 dashDir = Vector3.zero;
    List<int> struckTargets = new();


    int dashStartTick = 0;
    public List<int> StruckTargets { get => struckTargets; set => struckTargets = value; }
    public int TackleDuration { get => tackleDuration; set => tackleDuration = value; }
    public int TackleStartTick { get => dashStartTick; set => dashStartTick = value; }
    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Skill; set { } }
    public bool UpdateDuringHitstop { get => false; set { } }

    ITackleSkill tackleManager;
    HitstopManager hitstopManager;

    DashSnapshot[] dashSnapshots = new DashSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        dashSpeed = dashDistance / (float) dashDuration;
        SetDashParticleEmission(false);
        tackleManager = this as ITackleSkill;
        InitSimulated(manager.simulationManager);
        hitstopManager = manager.hitstopManager;
    }

    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {
        base.EnterSimulated(msg);
        struckTargets.Clear();
        hitbox.hitboxCollider.enabled = true;
        dashDir = GetMovementDir().normalized;
        base.OnSkillUsed();
        character.velocityManager.OverwriteInternalSpeed(dashDir * dashSpeed);
        hitbox.transform.rotation.SetLookRotation(dashDir);
        hitbox.damageInfo.knockbackDir = character.velocityManager.GetInternalSpeed().normalized;

        SetDashParticleEmission(true);

        dashStartTick = simulationManager.CurrentTick;
        skillBuffer.Consume();

        Debug.Log("Setting char speed to " + character.velocityManager.GetInternalSpeed());
    }

    public override void EnterVisuals(Dictionary<string, object> msg = null)
    {
        base.EnterVisuals(msg);
        if (whooshClip != null) character.unscaledAudioSource.PlayOneShot(whooshClip);
        dashParticles.transform.rotation = Quaternion.LookRotation(-dashDir);
    }


    void SetDashParticleEmission(bool value)
    {
        var emission = dashParticles.emission;
        emission.enabled = value;
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
        bool hasStamina = staminaComponent.Stamina > staminaCost || staminaComponent.ForesightEnabled;
        return hasStamina && GetMovementDir().magnitude > DASH_DEADZONE_REQUIREMENT;
    }

    public void InitSimulated(SimulationManager simulationManager)
    {
        simulationManager.AddSimulatedObject(this);
        this.simulationManager = simulationManager;
    }

    public void SimulateUpdate(int currentTick)
    {
        if (fsm.currentState != this) return;
        if (currentTick - dashStartTick >= dashDuration)
        {
            character.velocityManager.OverwriteInternalSpeed(dashSpeed * speedMaintained * dashDir);
            OnSkillOver();
        }
        tackleManager.HitboxCollisionLogic(hitbox, hitstopManager, tackleMask, speaker);
    }
    public DashSnapshot CaptureState()
    {
        DashSnapshot snapshot = new()
        {
            startTick = dashStartTick,
            tackleSnapshot = tackleManager.CaptureState()
        };
        return snapshot;
    }

    public void RestoreState(DashSnapshot snapshot)
    {
        dashStartTick = snapshot.startTick;

        struckTargets.Clear();

        tackleManager.RestoreState(snapshot, struckTargets);
    }

    public void CaptureCurrentState(int tick)
    {
        dashSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(dashSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }
}
public struct DashSnapshot
{
    public int startTick;
    public ITackleSkill.TackleSnapshot tackleSnapshot;
}