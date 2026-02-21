using System.Collections.Generic;
using UnityEngine;

public class StrikeBounceState : BounceState
{
    [SerializeField] HitboxComponent hitbox;
    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {
        base.EnterSimulated(msg);
        if (msg != null)
        {
            if (msg.ContainsKey("victim")) OnHitboxCollision((HealthComponent)msg["victim"]);
        }
        else
        {
            Debug.LogWarning("Entered strike bounce state with no victims");
            fsm.TransitionTo<FlyingState>();
        }
    }
    public override void ApplyBounceVelocity()
    {
        echo.UpdateSpeed(echoData.minSpeed);
        base.ApplyBounceVelocity();
    }

    public virtual void OnHitboxCollision(HealthComponent hp)
    {
        if (hp.hurtboxOwner.TryGetComponent(out BaseSpeaker victim))
        {
            if (echo.GetTarget() != victim.transform) return;
            hitbox.damageInfo.knockbackDir = echo.velocityManager.GetTotalSpeed().normalized;
            echo.echoCollision.Invoke(echo);
        }
            OnSuccessfulHit(hp);
    }


    public void OnSuccessfulHit(HealthComponent hp)
    {
        if (hp.IsInvulnerableTo(hitbox.damageInfo.damageSource))
        {
            DamageInfo newInfo = hitbox.damageInfo.CloneInfo();
            newInfo.knockbackLaunch = 0;
            hp.Damage(newInfo);

            particleManager.PlayInvulnParticles();
            echo.FindNewTarget(hp.hurtboxOwner.transform);
        }
        else
        {
            hp.Damage(hitbox.damageInfo);
            ChangeDirectionAndSpeedPostCollision(hp.hurtboxOwner.transform);
            echoData.deflectStreak = 1;
        }
    }



    public void ChangeDirectionAndSpeedPostCollision(Transform entity)
    {
        echo.UpdateSpeed(echoData.minSpeed);
        echo.FindNewTarget(entity);
    }

}
