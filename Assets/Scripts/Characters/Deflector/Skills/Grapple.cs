using System.Collections.Generic;
using UnityEngine;
public class Grapple : SpeakerBaseSkill, ISimulated, ISimulationSnapshot<GrappleSnapshot>
{
    AirStateResource.JumpInfo currentJumpInfo;
    [SerializeField] IDComponent iDComponent;
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

    int timeSinceLastDrain = 0;

    int timeSinceJumpStarted = 0;

    Vector3 previousHookPos = Vector3.zero;
    public enum HookState
    {
        Travelling,
        Hooked,
        Holstered,
    }
    public HookState hookState = HookState.Holstered;

    int grappleID;
    int ownerID;

    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Position; set {} }
    public EntityDatabaseID EntityType { get =>  EntityDatabaseID.PrecedentClone; set { } }
    public int ID { get => grappleID; set => grappleID = value; }
    public bool UpdateDuringHitstop { get => false; set { } }

    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        JumpState jumpState = (JumpState)fsm.TryGetState<JumpState>();
        if (jumpState != null)
        {
            currentJumpInfo = jumpState.currentJumpInfo;
        }
        grappleRB.transform.parent = null; //shouldn't follow player  
        lineRenderer.enabled = false;
        iDComponent.InitComponent(manager);
        manager.entityManager.SetOwnerForEntity(iDComponent.ID, speaker.idComponent.ID);
    }
    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {
        base.EnterSimulated(msg);
        skillBuffer.Consume();
        if (hookState == HookState.Holstered)
        {
            PerformJump();
            timeSinceJumpStarted = 0;
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
        timeSinceJumpStarted += 1;
        if (timeSinceJumpStarted >= jumpDuration)
        {
            OnSkillOver();
            return;
        }
        GravityLogic();
    }

    public  void InactiveSimulate(int currentTick)
    {
        switch (hookState)
        {
            case HookState.Travelling:
                HookTravelDetectionLogic();
                break;
            case HookState.Hooked:
                if (staminaComponent.ForesightEnabled) return; 
                if (currentTick - timeSinceLastDrain >= staminaDrainRate)
                {
                    timeSinceLastDrain = currentTick;
                    staminaComponent.DamageStamina(1, 0, false);
                    if (staminaComponent.Stamina <= staminaCost) DestroyGrapple();
                }
                GrappleMotorPullLogic();
                break;

        }
        if (speaker.velocityManager.GetExternalSpeed(VelocityIDRegistry.GrapplePull) != VelocityManager.MISSING_VELOCITY_VALUE)
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
        Vector3 speed = speaker.velocityManager.GetExternalSpeed(VelocityIDRegistry.GrapplePull);
        speed *= decelRate;
        speaker.velocityManager.OverwriteExternalSpeed(VelocityIDRegistry.GrapplePull, speed);
        if (speed.magnitude <= 0.1f)
        {
            speaker.velocityManager.RemoveExternalSpeedSource(VelocityIDRegistry.GrapplePull);
        }

    }
    void GrappleMotorPullLogic()
    {
       if (hookState != HookState.Hooked) return;
       Vector3 pullDir = grappleRB.transform.position - speaker.transform.position;
       if (IsGrounded()) pullDir.y = 0;
       if (speaker.velocityManager.GetExternalSpeed(VelocityIDRegistry.GrapplePull) == VelocityManager.MISSING_VELOCITY_VALUE)
       {
            speaker.velocityManager.AddExternalSpeed(grapplePull * Time.fixedDeltaTime * pullDir, VelocityIDRegistry.GrapplePull);
       }
       else speaker.velocityManager.OverwriteExternalSpeed(VelocityIDRegistry.GrapplePull, grapplePull * Time.fixedDeltaTime * pullDir);
    }

    void HookTravelDetectionLogic()
    {
        Vector3 travelVector = grappleRB.transform.position - previousHookPos;
        float checkerDistance = travelVector.magnitude;
        if (checkerDistance < 0.001f) return;

        Ray ray = new (previousHookPos, travelVector.normalized);
        if( Physics.Raycast(ray, out RaycastHit hitInfo, checkerDistance, terrainMask))
        {
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
        if (hitInfo.collider.TryGetComponent(out IDComponent idComponent))
        {
            ownerID = idComponent.ID;
        }
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
        ownerID = EntityManager.MISSING_OWNER_ID;
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
        return (staminaComponent.Stamina > staminaCost && hookState == HookState.Holstered);
    }

    public GrappleSnapshot CaptureState()
    {
        return new GrappleSnapshot()
        {
            snapshotPosition = grappleRB.position,
            snapshotRotation = grappleRB.rotation,
            snapshotState = hookState,
            jumpTrack = timeSinceJumpStarted,
            drainTrack = timeSinceLastDrain,
            currentOwner = ownerID,
            
        };
    }

    public void RestoreState(GrappleSnapshot snapshot)
    {
        throw new System.NotImplementedException();
    }

    public void InitSimulated(SimulationManager simulationManager)
    {
        throw new System.NotImplementedException();
    }

    public void SimulateUpdate(int currentTick)
    {
        base.PhysicsProcess();
        timeSinceJumpStarted += 1;
        if (timeSinceJumpStarted >= jumpDuration)
        {
            OnSkillOver();
            return;
        }
        GravityLogic();
    }
}

public struct GrappleSnapshot
{
    public Grapple.HookState snapshotState;
    public Vector3 snapshotPosition;
    public Quaternion snapshotRotation;
    public int jumpTrack;

    public int drainTrack;

    public int currentOwner;

}