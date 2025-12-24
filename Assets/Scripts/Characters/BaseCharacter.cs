using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class BaseCharacter : MonoBehaviour
{

    public UnityEvent<BaseCharacter> requestedPause =  new();

    public CharacterStateMachine characterStateMachine;

    public StaminaComponent staminaComponent;
    public PlayerInput playerInput;
    public MeshRenderer playerModel;
    public VelocityManager velocityManager;
    public GroundIndicator groundIndicator;
    public AudioSource unscaledAudioSource; //unscaled so it plays during hit-stop

    public List<Material> playerColors = new();

    [HideInInspector] public int teamIndex;
    [HideInInspector] Transform lookTarget = null;
    protected bool init = false;


    public virtual void InitPlayer(MatchData.PlayerInfo info, int index)
    {

        teamIndex = index;
        playerModel.material = playerColors[index - 1];
        name = "Player " + index;
        groundIndicator.Init(playerColors[index - 1], index);


        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }
        if (unscaledAudioSource == null)
        {
            unscaledAudioSource = GetComponent<AudioSource>();
        }
        unscaledAudioSource.outputAudioMixerGroup.audioMixer.updateMode = UnityEngine.Audio.AudioMixerUpdateMode.UnscaledTime;

        StartCoroutine(InitStateMachine(info));
        StartCoroutine(AssignLookTarget());
        AssignPlayerDevice(info);
    }

    protected virtual IEnumerator InitStateMachine(MatchData.PlayerInfo info)
    {
        yield return new WaitForFixedUpdate();
        AssignPlayerDevice(info); //must do this first for state machine buffers, otherwise they will assume kb 1 speaker controls
        characterStateMachine.CreateSkills(info);
        characterStateMachine.InitMachine();
        init = true;
    }
    protected void AssignPlayerDevice(MatchData.PlayerInfo info)
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

        Debug.Log("device name is " + info.device.name);

        playerInput.SwitchCurrentActionMap(info.controlScheme);


    }
    IEnumerator AssignLookTarget()
    {
        yield return new WaitForFixedUpdate();
        lookTarget = FindFirstObjectByType<BaseEcho>().transform;
    }

    private void Update()
    {
        if (playerInput.actions["Pause"].WasPressedThisFrame())
        {
            Debug.Log("pressed pause button");
            requestedPause.Invoke(this);
        }
        if (GameManager.inSpecialStop || !init) { return; }
        characterStateMachine.UpdateState();
    }

    private void FixedUpdate()
    {
        if (GameManager.inSpecialStop || !init) { return; }
        characterStateMachine.FixedUpdateState();
        if (lookTarget != null)
        {
            playerModel.transform.LookAt(lookTarget);
        }
    }

    public virtual void DeactivatePlayer()
    {
        HidePlayer();
        enabled = false;
        playerInput.DeactivateInput();
    }

    public virtual void ActivatePlayer()
    {
        ShowPlayer();
        enabled = true;
        playerInput.ActivateInput();
    }


    public void ShowPlayer()
    {
        playerModel.gameObject.SetActive(true);
    }

    public void HidePlayer()
    {
        playerModel.gameObject.SetActive(false);
    }
    public void SetLookTarget(Transform target)
    {
        lookTarget = target;
        Debug.Log(name + " is looking at target " + target.name);
    }

    public Transform GetLookTarget()
    {
        return lookTarget;
    }

}
