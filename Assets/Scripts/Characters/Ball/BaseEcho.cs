using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
[RequireComponent(typeof(Rigidbody))]

public class BaseEcho : BaseCharacter
{

    public HashSet<Transform> characterList = new();
    public UnityEvent<BaseEcho> echoCollision = new();
    public UnityEvent<BaseEcho> echoDeflected = new();
    public UnityEvent<Vector3> echoWarped = new();
    public UnityEvent<Transform> echoTargetChanged = new();

  

    [HideInInspector] public bool ballActive = false;
    [HideInInspector] public bool isIgnited = false;
    [SerializeField] TrailRenderer echoTrail;
    [SerializeField] EchoParticleManager particleManager;
   
    public bool playerControlled = false;
    public Rigidbody _rb;



    [SerializeField]  EchoDataResource echoData;

    protected Transform currentTarget;

    protected Vector2 startingPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        startingPos = transform.position;
    }
    public override void InitPlayer(MatchData.PlayerInfo info, int index)
    {
        teamIndex = index;
        name = "Echo " + index;
        groundIndicator.Init(playerColors[index - 1], index);


     
        StartCoroutine(InitStateMachine(info));
    }

    private void Start()
    {
        SuspendProjectile();
    }


    protected override IEnumerator InitStateMachine(MatchData.PlayerInfo info)
    {
        yield return new WaitForFixedUpdate();
        if (playerControlled) inputManager.InitInputComponent(info);        //must do this first for state machine buffers, otherwise they will assume kb 1 speaker controls
        characterStateMachine.CreateSkills(info);
        characterStateMachine.InitMachine();
        init = true;
    }

   public virtual void InitProjectile(HashSet<Transform> charList)
    {
        if (charList.Count < 2) { return; }

        if (inputManager == null)
        {
            inputManager = GetComponentInChildren<InputManager>();
        }
        if (unscaledAudioSource == null)
        {
            unscaledAudioSource = GetComponent<AudioSource>();
        }

        characterList = charList;
        currentTarget = characterList.ElementAt(0);
        transform.position = startingPos;

        unscaledAudioSource.outputAudioMixerGroup.audioMixer.updateMode = UnityEngine.Audio.AudioMixerUpdateMode.UnscaledTime;


        echoData.InitData();
      
        if (!characterStateMachine.initMachine) characterStateMachine.InitMachine();

    }
    public void EnableProjectile()
    {
        transform.position = startingPos;
        UpdateSpeed(echoData.activeMinSpeed);
        ResumeProjectile();
    }

    public void ResumeProjectile()
    {
        playerModel.enabled = true;
        ballActive = true;
        characterStateMachine.TransitionTo<FlyingState>();
        velocityManager.freeze = false;
    }



    public override void ActivatePlayer()
    {
        if (!playerControlled)
        {
            playerModel.enabled = true;
            ballActive = true;

            echoData.InitData();
        }
        else
        {
            base.ActivatePlayer();
        }
    }

    public override void DeactivatePlayer()
    {
        if (playerControlled) base.DeactivatePlayer();

    }

    public void SuspendProjectile(bool hide = true, bool hitboxActive = false)
    {
        playerModel.enabled = !hide;
        ballActive = hitboxActive;
        velocityManager.freeze = true;
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (GameManager.inSpecialStop || !ballActive || currentTarget == null) { return; }
        playerModel.transform.LookAt(currentTarget.transform.position);
        characterStateMachine.FixedUpdateState();
    }

    private void Update()
    {
        if (GameManager.inSpecialStop || !ballActive || currentTarget == null) { return; }
        characterStateMachine.UpdateState();
    }


    public virtual void FindNewTarget(Transform lastHitCharacter)
    {
        HashSet<Transform> targetList = new (characterList);
        targetList.Remove(lastHitCharacter);
        int randomIndex = Random.Range(0, targetList.Count);
        currentTarget = targetList.ElementAt(randomIndex);
        echoTargetChanged.Invoke(currentTarget);
    }

    public void SetNewTarget(Transform target)
    {
        if (target == currentTarget) return;
        currentTarget = target;
        echoTargetChanged.Invoke(target);
    }

    public Transform GetTarget()
    {
        return currentTarget;
    }

    public float GetSpeed()
    {
        return echoData.currentSpeed;
    }


    public void EnterSuddenDeath()
    {
        echoData.activeMinSpeed = echoData.igniteSpeed;
       if ( characterStateMachine.currentState.TryGetComponent(out EchoBaseState state) )
        {
            state.OnBallIgnited();
        }
    }

    public void WarpToLocation(Vector3 pos)
    {
        Vector3 previousPos = transform.position;
        if (transform.parent == null) transform.position = pos;
        echoWarped.Invoke(pos - previousPos);
        echoTrail.Clear();
    }

    public virtual void UpdateSpeed(float newSpeed)
    {
        echoData.currentSpeed = Mathf.Clamp(newSpeed, echoData.activeMinSpeed, echoData.activeMaxSpeed);
        isIgnited = (echoData.currentSpeed >= echoData.igniteSpeed);
        particleManager.OnSpeedUpdated(echoData.currentSpeed, isIgnited);
    }

    public void ForceDeflect(BaseSpeaker speaker)
    {
        var msg = new Dictionary<string, object>()
        {
            ["deflector"] = speaker,
            ["usedSkill"] = true,  
        };
        characterStateMachine.TransitionTo<DeflectionBounceState>(msg);
        Debug.Log("Forcing deflect of echo " + name + " by speaker " + speaker.name);
    }

}



