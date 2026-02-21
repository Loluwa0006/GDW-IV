using UnityEngine;
using UnityEngine.InputSystem;

public class SpeakerMoveState : SpeakerBaseState
{
    protected const float DAMPING_RATE = 0.97f;

    [SerializeField] protected float moveAcceleration = 2.5f;
    [SerializeField] protected float moveSpeed = 20.0f;

    [SerializeField] BufferHelper jumpBuffer;
    [SerializeField] BufferHelper skillOneBuffer;
    [SerializeField] BufferHelper skillTwoBuffer;

    protected Rigidbody _rb;

    protected Vector3 moveDir = new();
    
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        _rb = cha.GetComponent<Rigidbody>();
        playerInput = cha.GetComponent<PlayerInput>();
    }

    public override void Process()
    {
        moveDir = GetMovementDir();
        if (jumpBuffer.Buffered)
        {
            jumpBuffer.Consume();
            fsm.TransitionTo<JumpState>();
            return;
        }
        if (skillOneBuffer.Buffered)
        {
            fsm.TransitionToSkill(1);
            
        }
        else if (skillTwoBuffer.Buffered)
        {
           fsm.TransitionToSkill(2);
        }
    }


    public override void PhysicsProcess()
    {
        if (!IsGrounded())
        {
            fsm.TransitionTo<FallState>();

            return;
        }
        if (moveDir.magnitude <= MOVE_DEADZONE)
        {
            fsm.TransitionTo<IdleState>();
            return;
        }
        Vector3 newSpeed = moveDir * moveAcceleration;
     
        character.velocityManager.AddInternalVelocity(newSpeed);
        character.velocityManager.ClampInternalVelocity(moveSpeed);
    }
}
