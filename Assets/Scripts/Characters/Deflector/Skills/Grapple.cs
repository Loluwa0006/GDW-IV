using System.Collections.Generic;
using UnityEngine;

public class Grapple : SpeakerBaseSkill
{
    AirStateResource.JumpInfo currentJumpInfo;

    [Header("Jump Attributes")]
    [SerializeField] float doubleJumpPower = 0.7f;
    [SerializeField] float doubleJumpFloatiness = 0.2f;
    [SerializeField] int jumpDuration = 20;
    [Header("Grapple Variables")]
    [SerializeField] Rigidbody grappleRB;
    [SerializeField] Collider grappleCollider;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] LayerMask terrainMask;
    [Header("Grapple Attributes")]
    [SerializeField] float grappleSpeed = 20f;
    [SerializeField] int staminaDrainRate = 9;
    [SerializeField] float grapplePull = 13.5f;
    [SerializeField] float decelRate = 0.975f;

    int drainTracker = 0;

    int jumpTracker = 0;

    Vector3 previousHookPos = Vector3.zero;

    BaseEcho targetEcho;

    public enum HookState
    {
        Travelling,
        Hooked,
        Holstered,
    }
    public HookState hookState = HookState.Holstered;
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm)
    {
        base.InitState(cha, fsm);
        JumpState jumpState = (JumpState)fsm.TryGetState<JumpState>();
        if (jumpState != null)
        {
            currentJumpInfo = jumpState.currentJumpInfo;
            Debug.Log("Found jump state, using that jump info ");
        }
        grappleRB.transform.parent = null; //shouldn't follow player  
        lineRenderer.enabled = false;
        targetEcho = null;
    }
    public override void Enter(Dictionary<string, object> msg = null)
    {
        base.Enter(msg);
        skillBuffer.Consume();
        if (hookState == HookState.Holstered)
        {
            PerformJump();
            jumpTracker = 0;
            FireGrapple();
        }
        else
        {
            DestroyGrapple();
            OnSkillOver();
        }
    }

    void PerformJump()
    {
        Vector3 currentSpeed = speaker.velocityManager.GetInternalSpeed();
        currentSpeed.y = currentJumpInfo.jumpVelocity * doubleJumpPower;
        speaker.velocityManager.OverwriteInternalSpeed(currentSpeed);
    }
    void FireGrapple()
    {
        lineRenderer.enabled = true;
        grappleRB.gameObject.SetActive(true);
        grappleRB.transform.position = speaker.transform.position;
        UpdateGrappleLine();
        Vector3 hookDirection = GetMovementDir().normalized;
        if (hookDirection == Vector3.zero)
        {
            hookDirection = speaker.transform.forward;
        }
        Vector3 additionalSpeedFromSpeaker = speaker.velocityManager.GetTotalSpeed();
        additionalSpeedFromSpeaker.y = 0; //HORIZONTAL ONLY
        float alignment = Vector3.Dot(hookDirection, additionalSpeedFromSpeaker.normalized);
        alignment = Mathf.Max(0, alignment);
        grappleRB.linearVelocity = hookDirection * grappleSpeed + additionalSpeedFromSpeaker * alignment;

        if (previousHookPos == Vector3.zero) previousHookPos = speaker.transform.position;
        hookState = HookState.Travelling;
        OnSkillUsed();
    }
    public override void PhysicsProcess()
    {
        base.PhysicsProcess();
        jumpTracker += 1;
        if (jumpTracker >= jumpDuration)
        {
            OnSkillOver();
            return;
        }
        GravityLogic();
    }

    public override void InactivePhysicsProcess()
    {
        switch (hookState)
        {
            case HookState.Travelling:
                HookTravelDetectionLogic();
                break;
            case HookState.Hooked:
                if (staminaComponent.HasForesight()) return; 
                drainTracker -= 1;
                if (drainTracker <= 0)
                {
                    drainTracker = staminaDrainRate;
                    staminaComponent.DamageStamina(1, 0, false);
                    if (staminaComponent.GetStamina() <= staminaCost) DestroyGrapple();
                }
                GrappleMotorPullLogic();
                break;

        }
        if (speaker.velocityManager.GetExternalSpeed("GrapplePull") != VelocityManager.MISSING_VELOCITY_VALUE)
        {
            RemoveGrapplePull();
        }
    }
    public override void InactiveProcess()
    {
        UpdateGrappleLine();
    }

    public override void Process()
    {
        UpdateGrappleLine();
    }
    void UpdateGrappleLine()
    {
        if (!lineRenderer.enabled) return;
        lineRenderer.SetPosition(0, speaker.transform.position);
        lineRenderer.SetPosition(1, grappleRB.transform.position);
    }

    void RemoveGrapplePull()
    {
        Vector3 speed = speaker.velocityManager.GetExternalSpeed("GrapplePull");
        speed *= decelRate;
        speaker.velocityManager.OverwriteExternalSpeed("GrapplePull", speed);
        if (speed.magnitude <= 0.1f)
        {
            speaker.velocityManager.RemoveExternalSpeedSource("GrapplePull");
        }

    }
    void GrappleMotorPullLogic()
    {
        if (hookState != HookState.Hooked) return;
        Vector3 pullDir = grappleRB.transform.position - speaker.transform.position;
        if (IsGrounded()) pullDir.y = 0;
       if (speaker.velocityManager.GetExternalSpeed("GrapplePull") == VelocityManager.MISSING_VELOCITY_VALUE)
        {
            speaker.velocityManager.AddExternalSpeed(grapplePull * Time.fixedDeltaTime * pullDir, "GrapplePull");
        }
        else speaker.velocityManager.OverwriteExternalSpeed("GrapplePull", grapplePull * Time.fixedDeltaTime * pullDir);
    }

    void HookTravelDetectionLogic()
    {
        Vector3 travelVector = grappleRB.transform.position - previousHookPos;
        float checkerDistance = travelVector.magnitude;
        if (checkerDistance < 0.001f) return;

        Ray ray = new (previousHookPos, travelVector.normalized);
        if( Physics.Raycast(ray, out RaycastHit hitInfo, checkerDistance, terrainMask))
        {
            Debug.Log("Locking hook since it hit " + hitInfo.transform.name);
            ConnectHookToObject(hitInfo);
        }
        previousHookPos = grappleRB.transform.position;
        UpdateGrappleLine();
    }

    void ConnectHookToObject(RaycastHit hitInfo)
    {
        grappleRB.linearVelocity = Vector3.zero;
        grappleRB.transform.parent = hitInfo.transform;
        grappleRB.transform.position = hitInfo.point;
        hookState = HookState.Hooked;
    }

    void GravityLogic()
    {
        Vector3 currentSpeed = speaker.velocityManager.GetInternalSpeed();
        currentSpeed.y -= GetGravity() * Time.fixedDeltaTime;
        speaker.velocityManager.OverwriteInternalSpeed(currentSpeed);
    }
    public void DestroyGrapple()
    {
        grappleRB.gameObject.SetActive(false);
        lineRenderer.enabled = false;
        hookState = HookState.Holstered;
    }

    protected override void OnSkillOver()
    {
        previousHookPos = Vector3.zero;
        base.OnSkillOver();
    }
    float GetGravity()
    {
        if (character.velocityManager.GetInternalSpeed().y > 0)
        {
            return currentJumpInfo.jumpGravity * doubleJumpFloatiness;
        }
        else
        {
            return currentJumpInfo.fallGravity * doubleJumpFloatiness;
        }
    }

    public override bool SkillAvailable()
    {
        if (hookState == HookState.Hooked)
        {
            return true;
        }
        return (staminaComponent.GetStamina() > staminaCost && hookState == HookState.Holstered);
    }
}
