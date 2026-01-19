using System.Collections.Generic;
using UnityEngine;

public class BounceState : EchoBaseState
{

    [SerializeField] protected EchoDataResource echoData;
    [SerializeField] protected EchoParticleManager particleManager;

    Vector3 oldSpeed;

    int bounceTracker = 0;
    const int BOUNCE_DURATION = 7;
    public override void Enter(Dictionary<string, object> msg = null)
    {
        oldSpeed = character.velocityManager.GetInternalSpeed();
        character.velocityManager.OverwriteInternalSpeed(Vector3.zero);
        bounceTracker = BOUNCE_DURATION;
        base.Enter(msg);
    }
    public override void PhysicsProcess()
    {
        base.PhysicsProcess();
        if (GameManager.inSpecialStop || !echo.ballActive || echo.GetTarget() == null) { return; }
        bounceTracker -= 1;
        if (bounceTracker <= 0)
        {
            fsm.TransitionTo<FlyingState>();
            return;
        }
    }

    public override void Exit()
    {
        ApplyBounceVelocity();
        base.Exit();
    }

    public virtual void ApplyBounceVelocity()
    {

        if (echo.viableTargets.Count == 1)
        {
            character.velocityManager.OverwriteInternalSpeed(oldSpeed * -1.0f);
            Debug.Log("Flipping velocity due to no viable targets.");
        }
        else
        {
            echo.velocityManager.OverwriteInternalSpeed((echo.GetTarget().transform.position - transform.position).normalized * echo.GetSpeed());
        }
    }



}

