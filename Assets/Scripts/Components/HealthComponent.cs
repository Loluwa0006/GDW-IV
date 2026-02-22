using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class HealthComponent : MonoBehaviour, ISimulated, ISimulationSnapshot<HealthSnapshot>, ISnapshotable
{

    
    [System.Serializable]
    public enum StatusType
    {
        Invulnerability,
        Vulnerability,
        Armor,
        Slow,
        Stun,
        ReversedControls,
    }
    public enum DamageResult
    {
        InvincibleToType,
        Success,
        Armored,
        Weakened,
        Other
    }

    public Transform hurtboxOwner;
    public UnityEvent<DamageInfo> entityDamaged = new();
    public UnityEvent<DamageInfo, HealthComponent> entityDefeated = new();
    public IDComponent idComponent; 

    SortedDictionary<StatusEffectIDs, StatusEffect> statusEffects = new();

    bool playerDead = false;
    List<StatusEffectIDs> expiredEffects = new();

    GameManager gameManager;

    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Default; set { } }

    public bool UpdateDuringHitstop { get => false; set { } }


    HealthSnapshot[] healthSnapshots = new HealthSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];

    public void InitComponent(GameManager manager)
    {
        gameManager = manager;
        InitSimulated(manager.simulationManager);
        if (idComponent == null) idComponent = GetComponentInParent<IDComponent>();
    }

    public void AddStatusEffect(StatusEffect effect, StatusEffectIDs ID)
    {
        if (statusEffects.ContainsKey(ID))
        {
            return;
        }
        else
        {
            statusEffects.Add(ID, effect);
        }
     }

    public void RemoveStatusEffect(StatusEffectIDs ID)
    {
        if (statusEffects.ContainsKey(ID))
        {
            if (statusEffects[ID].removable)
            {
                statusEffects.Remove(ID);
            }
        }
    }
    public bool IsInvulnerableTo(DamageSource source)
    {
        foreach (var effect in statusEffects.Values)
        {
            if (effect.statusType == StatusType.Invulnerability)
            {
                var invuln = effect as InvulnerabilityEffect;
                if (invuln.invincibilityType == source)
                {
                    return true;
                }
            }
        }
        return false;
    }
    public virtual DamageResult Damage(DamageInfo originalInfo)
    {
        DamageInfo modifiedInfo = originalInfo.CloneInfo();

        foreach (var effect in statusEffects.Values)
        {
            modifiedInfo.damage = effect.ModifyDamage(originalInfo);
        }
        if (modifiedInfo.damage <= 0) modifiedInfo.damage = 0; //if damage is negative, entity heals, which is wrong;

        else if (!playerDead)
        {
            entityDamaged.Invoke(modifiedInfo);
            gameManager.hitstopManager.ApplySpecialStop(modifiedInfo.hitstop);
        }
            
        OnEntityDamaged(modifiedInfo);

        return GetDamageResult(originalInfo, modifiedInfo);
    }
    DamageResult GetDamageResult(DamageInfo originalInfo, DamageInfo modifiedInfo)
    {
        if (originalInfo.damage > modifiedInfo.damage)
        {
            return DamageResult.Weakened;
        }
        else if (originalInfo.damage < modifiedInfo.damage)
        {
            if (modifiedInfo.damage == 0)
            {
                return DamageResult.InvincibleToType;
            }
            return DamageResult.Armored;
        }
        return DamageResult.Success;
    }
    public virtual void KillEntity(DamageInfo info, HealthComponent hp)
    {
        entityDefeated.Invoke(info, this);
        playerDead = true;
    }

    public void OnEntityDamaged(DamageInfo info)
    {
        if (playerDead)   return; 
        if (!hurtboxOwner.TryGetComponent(out BaseSpeaker speaker)) return;
        
        if (speaker.fsm.currentState.OnCharacterHit(info))
        {
            Vector3 currentSpeed = speaker.velocityManager.GetInternalSpeed();
            currentSpeed.y = info.knockbackLaunch;
            Vector3 knockbackVector = new Vector3(info.knockbackDir.x, currentSpeed.y, info.knockbackDir.z).normalized * info.knockbackDistance;
            speaker.velocityManager.OverwriteInternalSpeed(knockbackVector);
           

            if (info.hitSFX != null)
            {
                speaker.unscaledAudioSource.PlayOneShot(info.hitSFX);
            }
        }
        
    }
    public bool IsAlive()
    {
        return !playerDead;
    }

    public void ResetComponent()
    {
        playerDead = false;
        statusEffects.Clear();
    }

    public HealthSnapshot CaptureState()
    {
        HealthSnapshot snapshot = new ();

        snapshot.dead = playerDead;

        int index = 0;
        foreach (var kvp in statusEffects)
        {
            switch (index)
            {
                case 0: snapshot.status1 = new StatusEffectSnapshot(kvp.Value.tickWhenStatusEffectObtained, kvp.Value.duration, kvp.Value.statusType, kvp.Value.removable, kvp.Key, kvp.Value.potency); break;
                case 1: snapshot.status2 = new StatusEffectSnapshot(kvp.Value.tickWhenStatusEffectObtained, kvp.Value.duration, kvp.Value.statusType, kvp.Value.removable, kvp.Key, kvp.Value.potency); break;
                case 2: snapshot.status3 = new StatusEffectSnapshot(kvp.Value.tickWhenStatusEffectObtained, kvp.Value.duration, kvp.Value.statusType, kvp.Value.removable, kvp.Key, kvp.Value.potency); break;
                case 3: snapshot.status4 = new StatusEffectSnapshot(kvp.Value.tickWhenStatusEffectObtained, kvp.Value.duration, kvp.Value.statusType, kvp.Value.removable, kvp.Key, kvp.Value.potency); break;
                case 4: snapshot.status5 = new StatusEffectSnapshot(kvp.Value.tickWhenStatusEffectObtained, kvp.Value.duration, kvp.Value.statusType, kvp.Value.removable, kvp.Key, kvp.Value.potency); break;
                case 5: snapshot.status6 = new StatusEffectSnapshot(kvp.Value.tickWhenStatusEffectObtained, kvp.Value.duration, kvp.Value.statusType, kvp.Value.removable, kvp.Key, kvp.Value.potency); break;
                case 6: snapshot.status7 = new StatusEffectSnapshot(kvp.Value.tickWhenStatusEffectObtained, kvp.Value.duration, kvp.Value.statusType, kvp.Value.removable, kvp.Key, kvp.Value.potency); break;
                case 7: snapshot.status8 = new StatusEffectSnapshot(kvp.Value.tickWhenStatusEffectObtained, kvp.Value.duration, kvp.Value.statusType, kvp.Value.removable, kvp.Key, kvp.Value.potency); break;
                case 8: snapshot.status9 = new StatusEffectSnapshot(kvp.Value.tickWhenStatusEffectObtained, kvp.Value.duration, kvp.Value.statusType, kvp.Value.removable, kvp.Key, kvp.Value.potency); break;
                case 9: snapshot.status10 = new StatusEffectSnapshot(kvp.Value.tickWhenStatusEffectObtained, kvp.Value.duration, kvp.Value.statusType, kvp.Value.removable, kvp.Key, kvp.Value.potency); break;
            }
            index++;
        }
        snapshot.numberOfStatusEffects = index;
        return snapshot;
    }
    public void RestoreState(HealthSnapshot snapshot)
    {
        playerDead = snapshot.dead;

        statusEffects.Clear();
        Debug.Log("Restoring " + snapshot.numberOfStatusEffects);
        for (int i = 0; i < snapshot.numberOfStatusEffects; i++)
        {
            switch (i)
            {
                case 0: statusEffects.Add(snapshot.status1.ID, new StatusEffect(snapshot.status1.type, snapshot.status1.duration, snapshot.status1.tickWhenStatusEffectObtained, snapshot.status1.potency)); break;
                case 1: statusEffects.Add(snapshot.status2.ID, new StatusEffect(snapshot.status2.type, snapshot.status2.duration, snapshot.status2.tickWhenStatusEffectObtained, snapshot.status2.potency)); break;
                case 2: statusEffects.Add(snapshot.status3.ID, new StatusEffect(snapshot.status3.type, snapshot.status3.duration, snapshot.status3.tickWhenStatusEffectObtained, snapshot.status3.potency)); break;
                case 3: statusEffects.Add(snapshot.status4.ID, new StatusEffect(snapshot.status4.type, snapshot.status4.duration, snapshot.status4.tickWhenStatusEffectObtained, snapshot.status4.potency)); break;
                case 4: statusEffects.Add(snapshot.status5.ID, new StatusEffect(snapshot.status5.type, snapshot.status5.duration, snapshot.status5.tickWhenStatusEffectObtained, snapshot.status5.potency)); break;
                case 5: statusEffects.Add(snapshot.status6.ID, new StatusEffect(snapshot.status6.type, snapshot.status6.duration, snapshot.status6.tickWhenStatusEffectObtained, snapshot.status6.potency)); break;
                case 6: statusEffects.Add(snapshot.status7.ID, new StatusEffect(snapshot.status7.type, snapshot.status7.duration, snapshot.status7.tickWhenStatusEffectObtained, snapshot.status7.potency)); break;
                case 7: statusEffects.Add(snapshot.status8.ID, new StatusEffect(snapshot.status8.type, snapshot.status8.duration, snapshot.status8.tickWhenStatusEffectObtained, snapshot.status8.potency)); break;
                case 8: statusEffects.Add(snapshot.status9.ID, new StatusEffect(snapshot.status9.type, snapshot.status9.duration, snapshot.status9.tickWhenStatusEffectObtained, snapshot.status9.potency)); break;
                case 9: statusEffects.Add(snapshot.status10.ID, new StatusEffect(snapshot.status10.type, snapshot.status10.duration, snapshot.status10.tickWhenStatusEffectObtained, snapshot.status10.potency)); break;
            }
        }
    }
    public void InitSimulated(SimulationManager simulationManager)
    {
        simulationManager.AddSimulatedObject(this);
    }

    public void SimulateUpdate(int currentTick)
    {
        if (gameManager.hitstopManager.InSpecialStop || gameManager.pauseManager.GamePaused()) return;

        foreach (var effect in statusEffects)
        {
            if (currentTick - effect.Value.tickWhenStatusEffectObtained > effect.Value.duration)
            {
                expiredEffects.Add(effect.Key);
            }
        }
        foreach (var id in expiredEffects)
        {
            if (!statusEffects.ContainsKey(id)) continue;
            statusEffects[id].OnExpire();
            statusEffects.Remove(id);
        }
        expiredEffects.Clear();
    }

    public void CaptureCurrentState(int tick)
    {
        healthSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(healthSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }
}

public struct HealthSnapshot
{
    public bool dead;

    public StatusEffectSnapshot status1;
    public StatusEffectSnapshot status2;
    public StatusEffectSnapshot status3;
    public StatusEffectSnapshot status4;
    public StatusEffectSnapshot status5;
    public StatusEffectSnapshot status6;
    public StatusEffectSnapshot status7;
    public StatusEffectSnapshot status8;
    public StatusEffectSnapshot status9;
    public StatusEffectSnapshot status10;

    public int numberOfStatusEffects;
}

public struct StatusEffectSnapshot
{
    public int tickWhenStatusEffectObtained;
    public int duration;
    public HealthComponent.StatusType type;
    public bool removable;
    public StatusEffectIDs ID;
    public int potency;

    public StatusEffectSnapshot(int start, int length, HealthComponent.StatusType type, bool remove, StatusEffectIDs id, int potency)
    {
        tickWhenStatusEffectObtained = start;
        duration = length;
        this.type = type;
        removable = remove;
        ID = id;
        this.potency = potency;
    }
}
[System.Serializable]
public class StatusEffect
{
    public int tickWhenStatusEffectObtained;
    public int duration = 0;
    public HealthComponent.StatusType statusType;
    public bool removable = false;
    public int potency;

    public StatusEffect(HealthComponent.StatusType statusType, int duration, int currentTick, int potency, bool removable = false)
    {
        this.duration = duration;
        this.statusType = statusType;
        this.removable = removable;
        this.potency = potency;
        tickWhenStatusEffectObtained = currentTick;
    }

    public virtual int ModifyDamage(DamageInfo info) => info.damage;
    public virtual void OnExpire()
    {

    }
}
[System.Serializable]
public class InvulnerabilityEffect : StatusEffect
{
    public DamageSource invincibilityType;

    public InvulnerabilityEffect(DamageSource type, int duration, int currentTick, int potency, bool removable = false)
        : base(HealthComponent.StatusType.Invulnerability, duration, currentTick, 0, removable)
    {
        invincibilityType = type;
    }

    public override int ModifyDamage(DamageInfo info)
    {
        if (info.damageSource == invincibilityType)
            return 0;
        return info.damage;
    }
}
public enum StatusEffectIDs
{
    EchoInvulnerability,
    TakebackCatchEchoInvulnerability,
    TakebackPostSuccessfulTackleInvulnerability,
}
