using UnityEngine;

public class IdleState : SpeakerMoveState
{
    [SerializeField] protected float decelRate = 0.85f;
    public override void PhysicsProcess()
    {
        if (!IsGrounded())
        {
            fsm.TransitionTo<FallState>();
            return;
        }
        if (moveDir.magnitude > MOVE_DEADZONE)
        {
            fsm.TransitionTo<RunState>();
            return;
        }
        Vector3 newSpeed = character.velocityManager.GetInternalSpeed() * decelRate;
        character.velocityManager.OverwriteInternalSpeed(newSpeed);

        Debug.Log("doin physics stuff");
    }
}
