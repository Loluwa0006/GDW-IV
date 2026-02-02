using System.Collections.Generic;
using UnityEngine;
public class JumpState : SpeakerAirState
{
    public AirStateResource.JumpInfo currentJumpInfo;
    [SerializeField] int airJumps = 1;
    int remainingAirJumps = 1;
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm)
    {
        base.InitState(cha, fsm);
        currentJumpInfo.InitJumpInfo();
    }
    public override void Enter(Dictionary<string, object> msg)
    {
        base.Enter(msg);
        Vector3 currentSpeed = character.velocityManager.GetInternalSpeed();
        currentSpeed.y = currentJumpInfo.jumpVelocity;
        character.velocityManager.OverwriteInternalSpeed(currentSpeed);

    }
    public override void PhysicsProcess()
    {
       base.PhysicsProcess();
        if (character.velocityManager.GetInternalSpeed().y < 0)
        {
            fsm.TransitionTo<FallState>();
        }
    }
    
    public override void InactivePhysicsProcess()
    {
        if (IsGrounded())
        {
            remainingAirJumps = airJumps;
        }
    }

    public override Dictionary<string, object> GetStateData()
    {
        Dictionary<string, object> data = new()
        {
            ["AirJumps"] = remainingAirJumps
        };
        return data;
    }

    protected override float GetGravity()
    {
        return currentJumpInfo.jumpGravity;
    }

    protected override AirStateResource.JumpInfo GetJumpInfo()
    {
        return currentJumpInfo;
    }
}
