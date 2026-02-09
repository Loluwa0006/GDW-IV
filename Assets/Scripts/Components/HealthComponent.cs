using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class HealthComponent : MonoBehaviour
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
    public bool ownedByEntity = true;



    Dictionary<string, StatusEffect> statusEffects = new();

    bool playerDead = false;
    List<string> expiredEffects = new();


    public void AddStatusEffect(StatusEffect effect, string ID)
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

    public void RemoveStatusEffect(string ID)
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
            GameManager.ApplySpecialStop(modifiedInfo.hitstop);
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
        if (playerDead) { Debug.Log("Player " + hurtboxOwner.name + " is dead.");  return; }
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

    private void FixedUpdate()
    {

        if (GameManager.inSpecialStop || GameManager.gamePaused) return;
        
            foreach (var effect in statusEffects)
            {

                effect.Value.duration -= 1;
                //Debug.Log("decreased status effect " + effect.Key + " to new duration " +  effect.Value.duration);
                if (effect.Value.duration <= 0)
                {
                    Debug.Log(effect.Key + " has expired");

                    expiredEffects.Add(effect.Key);
                }
            }
        

        foreach (var id in expiredEffects)
        {
            if (!statusEffects.ContainsKey(id)) continue;
            statusEffects[id].OnExpire();
            statusEffects.Remove(id);
            Debug.Log("Removed status effect " + id);
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
}

[System.Serializable]
public class StatusEffect
{
    public int duration = 0;
    public HealthComponent.StatusType statusType;
    public bool removable = false;

    public StatusEffect(HealthComponent.StatusType statusType, int duration, int amount = 0, bool removable = false)
    {
        this.duration = duration;
        this.statusType = statusType;
        this.removable = removable;
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

    public InvulnerabilityEffect(DamageSource type, int duration, bool removable = false)
        : base(HealthComponent.StatusType.Invulnerability, duration, 0, removable)
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


