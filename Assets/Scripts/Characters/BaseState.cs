using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BaseState : MonoBehaviour
{

    public const float MOVE_DEADZONE = 0.1f;
    public const float BOXCAST_RATIO = 0.85f;
    public const float SAFE_MARGIN = 0.05f;

    [HideInInspector] public BaseCharacter character;
    public bool hasInactiveProcess = false;
    public bool hasInactivePhysicsProcess = false;

    protected CharacterStateMachine fsm;
    protected LayerMask groundMask;

    protected Collider _rbCollider;

    protected PlayerInput playerInput;


    public virtual void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        this.fsm = fsm;
        character = cha;
        groundMask = LayerMask.GetMask("Ground");
        _rbCollider = cha.GetComponent<Collider>();
        playerInput = cha.GetComponent<PlayerInput>();
    }

    public virtual void EnterSimulated(Dictionary<string, object> msg = null) // Version for rollback
    {

    }

    public virtual void EnterVisuals(Dictionary<string, object> msg = null) // Version for local
    {

    }
    public virtual void Exit()
    {

    }

    public virtual void Process()
    {

    }

    public virtual void PhysicsProcess()
    {

    }

    public virtual void InactiveProcess()
    {

    }

    public virtual void InactivePhysicsProcess()
    {

    }

    public bool IsGrounded()
    {
        float castDistance = (_rbCollider.bounds.size.y / 2.0f) + SAFE_MARGIN;
        bool hit = Physics.BoxCast
            (
            _rbCollider.bounds.center,
            _rbCollider.bounds.size / 2.0f * BOXCAST_RATIO,
            Vector3.down,
            character.transform.rotation,
            castDistance,
            groundMask
            );
        return hit;
    }

    protected Vector3 GetMovementDir()
    {
        return character.inputManager.GetMovementDirection();
    }

    public virtual Dictionary<string, object> GetStateData()
    {
        return new Dictionary<string, object>();
    }
    public virtual void OnSpecialStopStarted()
    {

    }
    public virtual bool OnCharacterHit(DamageInfo info) // returns whether or not to invoke damaged signal
    {
        Dictionary<string, object> msg = new()
        {
            ["Data"] = info
        };
        fsm.TransitionTo<GetHitState>(msg);
        return true;
    }
}
