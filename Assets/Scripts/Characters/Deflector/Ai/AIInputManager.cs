using UnityEngine;
using UnityEngine.InputSystem;

public class AIInputManager : InputManager
{
    [SerializeField] AIBrain AIBrain;
    public override void InitInputComponent(MatchData.PlayerInfo info, GameManager manager)
    {
        //don't need this, AI doesn't use inputs
        DeactivateInput();
    }

    public override Vector3 GetMovementDirection()
    {
        return AIBrain.GetMovementDirection();
    }


    public override void ActivateInput()
    {
        //doesn't use input
    }

}

