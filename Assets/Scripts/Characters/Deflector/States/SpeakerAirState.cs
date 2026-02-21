using UnityEngine;

public class SpeakerAirState : SpeakerBaseState
{
    [SerializeField] protected AirStateResource airStateHelper;
    [SerializeField] protected BufferHelper skillOneBuffer;
    [SerializeField] protected BufferHelper skillTwoBuffer;
    protected float DAMPING_RATE = 0.985f;
    protected Rigidbody _rb;
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        _rb = cha.GetComponent<Rigidbody>();
    }

    public override void Process()
    {
        if (skillOneBuffer.Buffered)
        {
            fsm.TransitionToSkill(1);
        }
        else if (skillTwoBuffer.Buffered) 
        {
            fsm.TransitionToSkill(2);
        }
    }

    protected Vector3 AirStrafeLogic() 
    {
        Vector3 moveDir = GetMovementDir();

        Vector3 currentVel = character.velocityManager.GetInternalSpeed();
        Vector3 moveSpeed = new (currentVel.x, 0, currentVel.z);
        if (moveSpeed.magnitude < airStateHelper.airMoveSpeed)
        {
            Vector3 strafeSpeed = new Vector3(moveDir.x, 0, moveDir.z).normalized * airStateHelper.AirAcceleration;
            moveSpeed.x += strafeSpeed.x;
            moveSpeed.z += strafeSpeed.z;

            moveSpeed = Vector3.ClampMagnitude(moveSpeed, airStateHelper.airMoveSpeed);
        }
        else
        {
            moveSpeed.x *= DAMPING_RATE;
            moveSpeed.z *= DAMPING_RATE;
        }
        return moveSpeed;
    }


    public override void PhysicsProcess()
    {
        Vector3 newVel = character.velocityManager.GetInternalSpeed();
        Vector3 strafeSpeed = AirStrafeLogic();
        newVel.x = strafeSpeed.x;
        newVel.z = strafeSpeed.z;
        newVel.y -= GetGravity() * Time.fixedDeltaTime;
        newVel.y = Mathf.Max(GetJumpInfo().maxFallSpeed, newVel.y);
        
        character.velocityManager.OverwriteInternalSpeed(newVel);
    }

    protected virtual float GetGravity()
    {
        return 1;
    }

    protected virtual AirStateResource.JumpInfo GetJumpInfo()
    {
        return null;
    }
    
}