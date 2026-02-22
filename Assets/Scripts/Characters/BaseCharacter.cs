using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BaseCharacter : NetworkBehaviour, ISimulated
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
    public IDComponent idComponent;

    [HideInInspector] public int teamIndex = 1;
    protected Transform lookTarget = null;
    protected bool init = false;

    protected GameManager gameManager;


    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Speaker; set { } }
    public bool UpdateDuringHitstop { get => false; set { } }

    public virtual void InitPlayer(MatchData.PlayerInfo info, GameManager manager, int index)
    {

        gameManager = manager;
        teamIndex = index;
        playerModel.material = playerColors[index - 1];
        name = "Player " + index;
        groundIndicator.Init(playerModel.material, index);
        staminaComponent.InitComponent(manager);

        if (inputManager == null)
        {
            inputManager = GetComponent<InputManager>();
        }
        if (unscaledAudioSource == null)
        {
            unscaledAudioSource = GetComponent<AudioSource>();
        }
        if (velocityManager == null)
        {
            velocityManager = GetComponentInChildren<VelocityManager>();
        }
        unscaledAudioSource.outputAudioMixerGroup.audioMixer.updateMode = UnityEngine.Audio.AudioMixerUpdateMode.UnscaledTime;

        idComponent.InitComponent(manager);
        InitStateMachine(info, manager);
        velocityManager.InitManager(manager);
    }
    protected virtual void InitStateMachine(MatchData.PlayerInfo info, GameManager manager)
    {
        inputManager.InitInputComponent(info); //must do this first for state machine buffers, otherwise they will assume kb 1 speaker controls
        fsm.CreateSkills(info);
        fsm.InitMachine(manager);
        init = true;
    }
  
    private void Update()
    {
        if (inputManager.ActionPerformedThisFrame("Pause"))
        {
            requestedPause.Invoke(this);
        }
        if (gameManager.hitstopManager.InSpecialStop || !init) { return; }
        fsm.UpdateState();
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

    public virtual void ResetComponents()
    {
        enabled = true;
        ActivatePlayer();


        staminaComponent.ResetComponent(true);
        velocityManager.ResetComponent();
        fsm.ResetComponent();
    }

    public virtual void InitSimulated(SimulationManager simulationManager)
    {
        simulationManager.AddSimulatedObject(this);
    }

    public virtual void SimulateUpdate(int currentTick)
    {
        if (gameManager.hitstopManager.InSpecialStop || !init) { return; }
        fsm.FixedUpdateState();
        if (lookTarget != null)
        {
            playerModel.transform.LookAt(lookTarget);
        }
    }
}
public struct CharacterSnapshot
{
    public bool charEnabled;
    public bool charInit;

    public CharacterSnapshot(bool enable, bool init)
    {
        charEnabled = enable;
        charInit = init;
    }
}