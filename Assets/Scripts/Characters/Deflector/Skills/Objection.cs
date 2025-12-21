using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class Objection : SpeakerBaseSkill
{

    const int SLASH_EFFECT_POOL_SIZE = 100;

    [Header("Movement Attributes")]
    [SerializeField] AirStateResource.JumpInfo currentJumpInfo;
    [SerializeField] float runSpeed = 45f;
    [SerializeField] int runAccelerationFrames = 4;
    [SerializeField] float decelRate = 0.975f;


    [Header("Stamina Attributes")]
    [SerializeField] int staminaDrainRate = 7;

    [Header("Slash Attributes")]
    [SerializeField] int slashActiveFrames = 60;
    [SerializeField] int slashLineUpdateRate = 9;
    [SerializeField] float slashSize = 2.5f;
    [SerializeField] float distanceBetweenSlashEffects = 2.5f;
    [SerializeField] LayerMask slashMask;
    [SerializeField] DamageInfo damageInfo;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] ParticleSystem slashEffect;


    ParticleSystem[] slashEffectPool = new ParticleSystem[SLASH_EFFECT_POOL_SIZE];

    List<Vector3> slashPoints = new();
    List<HealthComponent> entitiesStruck = new();

    BufferHelper jumpBuffer;
    float runAccel = 0;
    int drainTracker = 0;
    int slashTracker = 0;
    int lineTracker = 0;
    int slashPoolIndex = 0;

    bool wasGrounded = false;

    public override void InitState(BaseCharacter cha, CharacterStateMachine s_machine)
    {
        base.InitState(cha, s_machine);
        currentJumpInfo.InitJumpInfo();
        runAccel = runSpeed / (float) runAccelerationFrames;
        jumpBuffer = fsm.TryGetBuffer("JumpBuffer");
        InitSlashParticles();
    }

    void InitSlashParticles()
    {
        for (int i = 0; i < SLASH_EFFECT_POOL_SIZE; i++)
        {
            var effect = Instantiate(slashEffect);
            effect.gameObject.SetActive(false);
            slashEffectPool[i] = effect;
        }
    }

    ParticleSystem GetSlashParticle()
    {
        slashPoolIndex = (slashPoolIndex + 1) % SLASH_EFFECT_POOL_SIZE;
        return slashEffectPool[slashPoolIndex];
    }

    public override void Enter(Dictionary<string, object> msg = null)
    {
        base.Enter(msg);
        skillBuffer.Consume();
        drainTracker = staminaDrainRate;
        lineTracker = slashLineUpdateRate;
        slashPoints.Clear();
        entitiesStruck.Clear();
        wasGrounded = IsGrounded();
        lineRenderer.positionCount = 0;
    }
    void PerformJump()
    {
        Vector3 currentSpeed = speaker.velocityManager.GetInternalSpeed();
        currentSpeed.y = currentJumpInfo.jumpVelocity;
        speaker.velocityManager.OverwriteInternalSpeed(currentSpeed);
    }

    float GetGravity()
    {
        if (speaker.velocityManager.GetInternalSpeed().y > 0) return currentJumpInfo.jumpGravity;

        return currentJumpInfo.fallGravity;
    }

    public override void PhysicsProcess()
    {
        Vector3 moveDir = GetMovementDir();
        Vector3 currentRunSpeed = speaker.velocityManager.GetInternalSpeed();
        float fallSpeed = currentRunSpeed.y;
        bool isGrounded = IsGrounded();
        currentRunSpeed.y = 0;
        if (!isGrounded) fallSpeed -= GetGravity() * Time.fixedDeltaTime;
        else if (!wasGrounded) fallSpeed = 0;

        if (moveDir.magnitude > MOVE_DEADZONE)
        {
            currentRunSpeed += moveDir * runAccel;
        }
        else
        {
            currentRunSpeed *= decelRate;
        }
            currentRunSpeed = Vector3.ClampMagnitude(currentRunSpeed, runSpeed);

        Vector3 finalSpeed = new (currentRunSpeed.x, fallSpeed, currentRunSpeed.z);

        speaker.velocityManager.OverwriteInternalSpeed(finalSpeed);

        DrainStamina();

        wasGrounded = isGrounded;
      }

    public override void Process()
    {
        if (jumpBuffer.Buffered && IsGrounded())
        {
            jumpBuffer.Consume();
            PerformJump();
        }

        if (skillBuffer.Buffered)
        {
            skillBuffer.Consume();
            PerformSlash();
            ExitState();
        }
        lineTracker -= 1;
        if (lineTracker == 0)
        {
            lineTracker = slashLineUpdateRate;
            lineRenderer.positionCount = lineRenderer.positionCount + 1;
            lineRenderer.SetPosition(slashPoints.Count, speaker.transform.position);
            slashPoints.Add(speaker.transform.position);
        }
    }

    void DrainStamina()
    {
        if (staminaComponent.HasForesight()) return;
        drainTracker -= 1;
        if (drainTracker == 0)
        {
            staminaComponent.DamageStamina(1, 0, false);
            if (staminaComponent.GetStamina() < staminaCost) ExitState();
            drainTracker = staminaDrainRate;
        }
    } 

    void ExitState()
    {
        if (!IsGrounded())
        {
            fsm.TransitionTo<FallState>();
        }
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

    void PerformSlash()
    {
        slashTracker = slashActiveFrames;
        Debug.Log("Performing slash");

        var pathPoints = SamplePathPoints(slashPoints, distanceBetweenSlashEffects);
        foreach (var point in pathPoints)
        {
            var slash = GetSlashParticle();
            slash.gameObject.SetActive(true);
            slash.transform.position = point;
            slash.Play();
        }
    }

    //code by Claude
    List<Vector3> SamplePathPoints(List<Vector3> path, float spacing)
    {
        List<Vector3> sampledPoints = new();
        if (path.Count < 2) return sampledPoints;

        sampledPoints.Add(path[0]); // Always include first point
        float accumulatedDistance = 0f;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 segmentStart = path[i - 1];
            Vector3 segmentEnd = path[i];
            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);

            accumulatedDistance += segmentLength;

            // Add point if we've traveled far enough
            if (accumulatedDistance >= spacing)
            {
                sampledPoints.Add(segmentEnd);
                accumulatedDistance = 0f;
            }
        }

        return sampledPoints;
    }

    //end of claude code

    public override void InactivePhysicsProcess()
    {
        if (slashTracker > 0) slashTracker--;
        if (slashTracker <= 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }
        Vector3 halfExtents = new (slashSize, slashSize, slashSize);
        foreach (var point  in slashPoints)
        {
            var overlap = Physics.OverlapBox(point, halfExtents, Quaternion.identity, slashMask, QueryTriggerInteraction.Collide);

            foreach (var entity in overlap)
            {
                Debug.Log("Found entity " + entity.name);
                if (entity.TryGetComponent(out HealthComponent healthComponent))
                {

                    if (healthComponent == speaker.healthComponent) continue;
                    if (entitiesStruck.Contains(healthComponent)) continue;
                    damageInfo.knockbackDir = (point - entity.transform.position).normalized;
                    healthComponent.Damage(damageInfo);
                    entitiesStruck.Add(healthComponent);
                }
                else if (entity.transform.parent.TryGetComponent(out BaseEcho echo))
                {
                    Debug.Log("Found echo " + echo.name);
                    if (echo.GetTarget() != speaker.transform) continue;
                    echo.ForceDeflect(speaker);
                }
               
            }
        }
    }
 }
