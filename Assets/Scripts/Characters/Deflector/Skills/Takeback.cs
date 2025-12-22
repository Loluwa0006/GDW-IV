using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR.Haptics;

public class Takeback : SpeakerBaseSkill
{
    enum TakebackState
    {
        Catching,
        Whiff,
        Holding,
        Throwing,
    }

    TakebackState currentState = TakebackState.Catching;

    UnityAction<BaseEcho> onEchoCollision;
    UnityAction<BaseEcho> onEchoDeflectedDrop;
    UnityAction<Vector3> onEchoWarped;

    [SerializeField] float whiffDuration = 0.9f;
    [SerializeField] int holdStaminaDrainRate = 8;
    [SerializeField] float decelRate = 0.9f;
    [SerializeField] float tacklePushback = 8.0f;
    [SerializeField] int staminaFreeHoldFrames = 12;


    [SerializeField] Transform ballHolder;
    [Header("Particles")]
    [SerializeField] ParticleSystem catchParticle;
    [SerializeField] ParticleSystem throwParticle;
    [SerializeField] ParticleSystem catchAttemptParticle;
    [Header("SFX")]
    [SerializeField] AudioClip catchSFX;
    [SerializeField] AudioClip throwSFX;
    [Header("Other")]
    [SerializeField] Color catchAvailableColor;
    [SerializeField] Color whiffedCatchColor;
   

    float catchDuration; //determined by deflect duration


    float durationTracker = 0.0f;
    float whiffTracker = 0.0f;
    int holdTracker;
    int freeHoldTracker = 0;

    float previousEchoSpeed = 0.0f;

    bool wasCatchingBeforeFreeze;

    BaseEcho heldBall;
    BaseSpeaker enemySpeaker;

    public override void InitState(BaseCharacter cha, CharacterStateMachine s_machine)
    {
        base.InitState(cha, s_machine);
        catchDuration = speaker.deflectManager.GetGoodDeflectDuration();
        StartCoroutine(FindOppositeSpeaker());
        throwParticle.transform.SetParent(null);
        SetCatchAttemptParticleColorAndStopEmitting(catchAvailableColor);
    }

    IEnumerator FindOppositeSpeaker()
    {
        yield return new WaitForFixedUpdate();
        var speakers = FindObjectsByType<BaseSpeaker>(FindObjectsSortMode.None);
        foreach (var speaker in speakers)
        {
            if (speaker == character) { continue; }
            Debug.Log(character.name + " is looking at char " + speaker.name);
            enemySpeaker = speaker;
            break;
        }
    }


    public override void Enter(Dictionary<string, object> msg = null)
    {
        base.Enter(msg);
        EnterCatchState();
        if (!staminaComponent.HasForesight())
        {
            staminaComponent.DamageStamina(staminaCost, 0, false);
        }
        skillBuffer.Consume();
        ballHolder.parent = speaker.playerModel.transform;
        ballHolder.transform.position = speaker.deflectManager.transform.position;
    }


    public override void Process()
    {

        switch (currentState)
        {
            case TakebackState.Catching:
                durationTracker -= Time.deltaTime;
                if (durationTracker <= 0.0f)
                {
                    EnterWhiffState();
                }
                break;
            case TakebackState.Whiff:
                whiffTracker -= Time.deltaTime;
                if (whiffTracker <= 0.0f) ExitState();
                break;
        }

        if (oppositeSkillBuffer != null)
        {
            if (oppositeSkillBuffer.Buffered)
            {
                fsm.TransitionToSkill(oppositeSkillIndex);
                return;
            }
        }
    }

    public override void PhysicsProcess()
    {
        var speed = character.velocityManager.GetInternalSpeed();
        speed *= decelRate;
        character.velocityManager.OverwriteInternalSpeed(speed);
    }


    public override void InactivePhysicsProcess() 
    {
        if (currentState == TakebackState.Holding)
        {
            holdTracker += 1;
            freeHoldTracker -= 1;
            if (freeHoldTracker < 0) freeHoldTracker = 0;
            Debug.Log("Free hold tracker == " + freeHoldTracker);
            Debug.Log("Hold tracker == " + holdTracker);
            if (holdTracker % holdStaminaDrainRate == 0 && freeHoldTracker <= 0)
            {
                if (!staminaComponent.HasForesight()) staminaComponent.DamageStamina(1, 0, false);
                if (staminaComponent.GetStamina() < staminaCost) DropBall();
            }
        }
    }

    public override void InactiveProcess()
    {
        if (!skillAction.IsPressed() && currentState == TakebackState.Holding)
        {
            EnterThrowState();
        }
        Debug.Log("Current state == " + currentState.ToString());
    }

    public override bool OnCharacterHit(DamageInfo info)
    {
        if (currentState == TakebackState.Catching && info.damageSource == DamageSource.Ball)
        {
            EnterHoldState(info);
            return false;
        }
        return base.OnCharacterHit(info);
    }

    void EnterCatchState()
    {
        speaker.healthComponent.AddStatusEffect(new InvulnerabilityEffect(DamageSource.Ball, int.MaxValue, true), "TakebackCatch");   //it is infinite because we need full control over when it leaves
        durationTracker = catchDuration;
        currentState = TakebackState.Catching;
        SetCatchAttemptParticleColorAndStopEmitting(catchAvailableColor);
        catchAttemptParticle.Play();
    }

    void EnterHoldState(DamageInfo info)
    {
        if (!info.attacker.TryGetComponent(out BaseEcho echo))
        {
            Debug.LogWarning(echo.transform.name + " doesn't have ball component ");
            return;
        }
        freeHoldTracker = staminaFreeHoldFrames;
        holdTracker = 0;
        character.unscaledAudioSource.PlayOneShot(catchSFX);
        heldBall = echo;

        previousEchoSpeed = echo.GetSpeed();
        echo.SuspendProjectile(false, true);
        currentState = TakebackState.Holding;
        echo.transform.parent = ballHolder.transform;
        echo.transform.localPosition = Vector3.zero;

        if (catchParticle != null)
        {
            catchParticle.transform.position = character.transform.position;
            catchParticle.Play();
        }
        character.SetLookTarget(enemySpeaker.transform);
        ConnectSignals(echo);
        ExitState();
        RemoveCatchAttemptParticles();
    }
    void EnterThrowState()
    {
        if (heldBall == null) return;
        heldBall.transform.parent = null;
        heldBall.characterStateMachine.TransitionTo<FlyingState>();
        character.unscaledAudioSource.PlayOneShot(throwSFX);
        EnableHeldEcho();
        heldBall.FindNewTarget(speaker.transform);
        
        currentState = TakebackState.Throwing;
        heldBall.UpdateSpeed(previousEchoSpeed);
        Vector3 throwDir = GetMovementDir();
        if (throwDir.magnitude <= MOVE_DEADZONE) throwDir = (enemySpeaker.transform.position - heldBall.transform.position).normalized;

        heldBall.velocityManager.OverwriteInternalSpeed(throwDir * previousEchoSpeed);
        staminaComponent.ConsumeForesight();
        RemoveSignals();
        if (throwParticle != null)
        {
            throwParticle.transform.position = ballHolder.transform.position;
            if (enemySpeaker != null)
            {
                throwParticle.transform.rotation = Quaternion.LookRotation(heldBall._rb.linearVelocity.normalized);
            }
            throwParticle.Play();
        }
        Debug.Log("entered throw state");
    }

    void EnterWhiffState()
    {
        whiffTracker = whiffDuration;
        currentState = TakebackState.Whiff;
        SetCatchAttemptParticleColorAndStopEmitting(whiffedCatchColor);
        catchAttemptParticle.Play();
        speaker.healthComponent.RemoveStatusEffect("TakebackCatch");
    }

    void OnHeldBallCollision(BaseEcho echo)
    {
        if (heldBall == null) return;
        DropBall();
        RemoveSignals();
        speaker.healthComponent.AddStatusEffect(new InvulnerabilityEffect(DamageSource.Ball, 10, false), "TakebackPostSuccessfulTackle");// remove infinite, replace with temp
    }


    public void EnableHeldEcho()
    {
        heldBall.transform.parent = null;
        heldBall.ResumeProjectile();
        character.SetLookTarget(heldBall.transform);
        speaker.healthComponent.RemoveStatusEffect("TakebackCatch");
    }

    void DropBall()
    {
        if (heldBall == null) return;
        EnableHeldEcho();
        currentState = TakebackState.Catching;
        heldBall.SetNewTarget(speaker.transform);
        RemoveSignals();
    }

    void OnHeldBallWarped(Vector3 movement)
    {
        Debug.Log("Warpin");
        StartCoroutine(PostWarpLogic(movement));
    }

    IEnumerator PostWarpLogic(Vector3 movement)
    {
        yield return null;
        character.transform.position += movement;
        if (heldBall != null)
        {
            heldBall.transform.localPosition = Vector3.zero;
        }
    }

    void ConnectSignals(BaseEcho echo)
    {
        onEchoCollision = OnHeldBallCollision;
        onEchoDeflectedDrop = _ => DropBall();
        onEchoWarped = OnHeldBallWarped;

        heldBall.echoCollision.AddListener(onEchoCollision);
        heldBall.echoDeflected.AddListener(onEchoDeflectedDrop);
        heldBall.echoWarped.AddListener(onEchoWarped);

    }
    void RemoveSignals()
    {
        if (heldBall == null) return;
        heldBall.echoCollision.RemoveListener(onEchoCollision);
        heldBall.echoDeflected.RemoveListener(onEchoDeflectedDrop);
        heldBall.echoWarped.RemoveListener(onEchoWarped);
    }


    void ExitState()
    {
        if (!IsGrounded()) fsm.TransitionTo<FallState>();
        
        else
        {
            if (GetMovementDir().magnitude < MOVE_DEADZONE)
            {
                fsm.TransitionTo<IdleState>();
            }
            else
            {
                fsm.TransitionTo<RunState>();
            }
        }
    }
    public override void Exit()
    {
        speaker.healthComponent.RemoveStatusEffect("TakebackCatch"); 
        SetCatchAttemptParticleColorAndStopEmitting(catchAvailableColor);
    }

    public void RemoveCatchAttemptParticles()
    {
        catchAttemptParticle.Clear();
        catchAttemptParticle.Stop();
    }

    public void SetCatchAttemptParticleColorAndStopEmitting(Color color)
    {
        RemoveCatchAttemptParticles();
        var main = catchAttemptParticle.main;
        main.startColor = color;
    }

    public override void ResetSkill()
    {
        currentState = TakebackState.Catching;
        SetCatchAttemptParticleColorAndStopEmitting(catchAvailableColor);
    }

    public override void OnSpecialStopStarted()
    {
        wasCatchingBeforeFreeze = fsm.currentState == this && currentState == TakebackState.Catching;
    }

    public override bool SkillAvailable()
    {
        if (!wasCatchingBeforeFreeze && (GameManager.inSpecialStop || GameManager.frameAfterSpecialStop))
        {
            return false; //can't deflect during freeze
        }
        return base.SkillAvailable();
    }


}
