using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BaseCharacter : MonoBehaviour
{
    public UnityEvent<BaseCharacter> requestedPause =  new();

    public CharacterStateMachine fsm;

    public StaminaComponent staminaComponent;
    public InputManager inputManager;
    public MeshRenderer playerModel;
    public VelocityManager velocityManager;
    public GroundIndicator groundIndicator;
    public AudioSource unscaledAudioSource; //unscaled so it plays during hit-stop
    public List<Material> playerColors = new();

    [HideInInspector] public int teamIndex = 1;
    protected Transform lookTarget = null;
    protected bool init = false;

    public virtual void InitPlayer(MatchData.PlayerInfo info, int index)
    {

        teamIndex = index;
        playerModel.material = playerColors[index - 1];
        name = "Player " + index;
        groundIndicator.Init(playerModel.material, index);


        if (inputManager == null)
        {
            inputManager = GetComponent<InputManager>();
        }
        if (unscaledAudioSource == null)
        {
            unscaledAudioSource = GetComponent<AudioSource>();
        }
        unscaledAudioSource.outputAudioMixerGroup.audioMixer.updateMode = UnityEngine.Audio.AudioMixerUpdateMode.UnscaledTime;

        StartCoroutine(InitStateMachine(info));
        StartCoroutine(AssignLookTarget());
    }
    protected virtual IEnumerator InitStateMachine(MatchData.PlayerInfo info)
    {
        yield return new WaitForFixedUpdate();
        inputManager.InitInputComponent(info); //must do this first for state machine buffers, otherwise they will assume kb 1 speaker controls
        fsm.CreateSkills(info);
        fsm.InitMachine();
        init = true;
    }
  
    IEnumerator AssignLookTarget()
    {
        yield return new WaitForFixedUpdate();
        lookTarget = FindFirstObjectByType<BaseEcho>().transform;
    }

    private void Update()
    {
        if (inputManager.ActionPerformedThisFrame("Pause"))
        {
            Debug.Log("pressed pause button");
            requestedPause.Invoke(this);
        }
        if (GameManager.inSpecialStop || !init) { return; }
        fsm.UpdateState();
    }

    private void FixedUpdate()
    {
        if (GameManager.inSpecialStop || !init) { return; }
        fsm.FixedUpdateState();
        if (lookTarget != null)
        {
            playerModel.transform.LookAt(lookTarget);
        }
    }

    public virtual void DeactivatePlayer()
    {
        HidePlayer();
        enabled = false;
        inputManager.DeactivateInput();
    }

    public virtual void ActivatePlayer()
    {
        ShowPlayer();
        enabled = true;
        inputManager.ActivateInput();
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

    public virtual void ResetComponents()
    {
        enabled = true;
        ActivatePlayer();


        staminaComponent.ResetComponent(true);
        velocityManager.ResetComponent();
        fsm.ResetComponent();
    }

}
