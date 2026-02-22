 using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class InputManager : MonoBehaviour
{
  [SerializeField]  PlayerInput playerInput;

    InputHistory[] inputHistory = new InputHistory[SimulationManager.MAX_ROLLBACK_FRAMES];
    private void Awake()
    {
        if (playerInput ==  null) playerInput = GetComponent<PlayerInput>();
    }

    public  virtual void InitInputComponent(MatchData.PlayerInfo info)
    {
        if (!playerInput.user.valid)
        {
            Debug.Log("Invalid user for char " + name);
            return;
        }
        if (info.device is Gamepad)
        {
            playerInput.user.UnpairDevices(); //get rid of other gamepads / the keyboard
            InputUser.PerformPairingWithDevice(info.device, playerInput.user); // add this gamepad to the current player
        }
        playerInput.SwitchCurrentActionMap(info.controlScheme);
    }
    public virtual Vector3 GetMovementDirection()
    {
        float x = playerInput.actions["Right"].ReadValue<float>() - playerInput.actions["Left"].ReadValue<float>();
        float z = playerInput.actions["Up"].ReadValue<float>() - playerInput.actions["Down"].ReadValue<float>();
        Vector3 moveDir = new(x, 0, z);
        if (moveDir.magnitude > 1.0f)
        {
            moveDir = moveDir.normalized;
        }
        return moveDir;
    }

    public virtual bool IsActionPressed(string actionName)
    {
        var action = GetAction(actionName);
        if (action == null) 
        {
            Debug.LogWarning("Could not find action " + actionName);
            return false;
        }
        return action.IsPressed();
    }

    public virtual bool ActionPerformedThisFrame(string actionName)
    {
        var action = GetAction(actionName);
        if (action == null)
        {
            Debug.LogWarning("Could not find action " + actionName);
            return false;
        }
        return action.WasPerformedThisFrame();
    }

    public InputAction GetAction(string actionName)
    {
        return playerInput.actions.FindAction(actionName);
    }

    public virtual void ActivateInput()
    {
        playerInput.ActivateInput();
    }

    public virtual void DeactivateInput()
    {
        playerInput.DeactivateInput();
    }

    public void CaptureInput(int tick)
    {
        float x = playerInput.actions["Right"].ReadValue<float>() - playerInput.actions["Left"].ReadValue<float>();
        float y = playerInput.actions["Up"].ReadValue<float>() - playerInput.actions["Down"].ReadValue<float>();
        inputHistory[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = new InputHistory()
        {
            tick = tick,
            moveDirection = new Vector2(x,y),
            jumpPressed = IsActionPressed("Jump"),
            deflectPressed = IsActionPressed("Deflect"),
            skillOnePressed = IsActionPressed("SkillOne"),
            skillTwoPressed = IsActionPressed("SkillTwo")
        };
    }

    public InputHistory GetInputFromTick(int tick)
    {
        return inputHistory[tick % SimulationManager.MAX_ROLLBACK_FRAMES];
    }
}

public struct InputHistory
{
    public int tick;
    public Vector2 moveDirection;
    public bool jumpPressed;
    public bool deflectPressed;
    public bool skillOnePressed;
    public bool skillTwoPressed;
}

