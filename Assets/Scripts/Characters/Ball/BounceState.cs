using System.Collections.Generic;
using UnityEngine;

public class BounceState : EchoBaseState, ISimulationSnapshot<BounceSnapshot>, ISnapshotable
{
    public const int BOUNCE_DURATION = 7;

    [SerializeField] protected EchoDataResource echoData;
    [SerializeField] protected EchoParticleManager particleManager;

    Vector3 oldSpeed;

    int bounceTracker = 0;

    protected GameManager gameManager;

    BounceSnapshot[] bounceSnapshots = new BounceSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        gameManager = manager;
    }
    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {
        oldSpeed = character.velocityManager.GetInternalSpeed();
        character.velocityManager.OverwriteInternalSpeed(Vector3.zero);
        bounceTracker = BOUNCE_DURATION;
        base.EnterSimulated(msg);
    }
    public override void PhysicsProcess()
    {
        base.PhysicsProcess();
        if (gameManager.hitstopManager.InSpecialStop|| !echo.ballActive || echo.GetTarget() == null) { return; }
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
        }
        else
        {
            echo.velocityManager.OverwriteInternalSpeed((echo.GetTarget().transform.position - transform.position).normalized * echo.GetSpeed());
        }
    }

    public BounceSnapshot CaptureState()
    {
        return new BounceSnapshot()
        {
            bounceDuration = bounceTracker,
        };
    }

    public void RestoreState(BounceSnapshot snapshot)
    {
        bounceTracker = snapshot.bounceDuration;
    }

    public void CaptureCurrentState(int tick)
    {
        bounceSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(bounceSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }
}


public struct BounceSnapshot
{
    public int bounceDuration;
}