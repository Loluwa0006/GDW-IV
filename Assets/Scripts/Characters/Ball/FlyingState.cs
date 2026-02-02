using System.Collections.Generic;
using UnityEngine;

public class FlyingState : EchoBaseState
{


    [SerializeField] protected HitboxComponent hitbox;
    [SerializeField] LayerMask speakerMask;
    [SerializeField] LayerMask terrainMask;
    [SerializeField] EchoDataResource echoData;
    public Rigidbody _rb;

    [Header("Steer Settings")]
    [SerializeField] protected float minSteerForce;
    [SerializeField] protected float maxSteerForce;
    [SerializeField] protected float promixityMultiplier;
    [SerializeField] protected float minProximityDistance;
    [SerializeField] protected float maxProximityDistance;
    [SerializeField] protected float minSpeedFactor = 0.7f;

    [Header("Colors")]
    [SerializeField] Material normalColor;
    [SerializeField] Material igniteColor;

    [Header("Particles")]
    [SerializeField] protected TrailRenderer echoTrail;
    [SerializeField] protected Gradient regularGradient;
    [SerializeField] protected Gradient ignitionGradient;
    [SerializeField] protected ParticleSystem ignitionTravelParticles;

    [Header("Other")]
    [SerializeField] int bounceCooldown = 7;


    Vector3 previousPos = Vector3.zero;

    int cooldownTracker = 0;

    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm)
    {
        base.InitState(cha, fsm);
        _rb = echo.GetComponent<Rigidbody>();
        _rbCollider = hitbox.hitboxCollider;
        groundMask = LayerMask.GetMask("Ground");
    }

    public override void Enter(Dictionary<string, object> msg = null)
    {
        base.Enter(msg);

        if (echo.isIgnited)
        {
            echoTrail.colorGradient = ignitionGradient;
        }
        else
        {
            echoTrail.colorGradient = regularGradient;
        }

        cooldownTracker = bounceCooldown;
    }

    protected bool HitboxCollisionLogic()
    {
        var overlap = Physics.OverlapBox(hitbox.hitboxCollider.bounds.center, hitbox.hitboxCollider.bounds.size, hitbox.transform.rotation, speakerMask, QueryTriggerInteraction.Collide);
        HealthComponent victim = null;
        foreach (var obj in overlap)
        {
            if (!obj.transform.TryGetComponent(out HealthComponent hp)) continue;
            victim = hp;
        }
        if (victim != null)
        {
            if (victim.hurtboxOwner.transform != echo.GetTarget())
            {
                return false;
            }
            Dictionary<string, object> msg = new Dictionary<string, object>();
            bool hitTarget = true;
            if (victim.hurtboxOwner.transform.TryGetComponent(out BaseSpeaker victimSpeaker))
            {
                if (victimSpeaker.deflectManager.IsDeflecting())
                {
                    if (victimSpeaker.deflectManager.IsPartialDeflect() && echo.isIgnited)
                    {
                        victimSpeaker.deflectManager.OnDeflectBroken();
                    }
                    else
                    {
                        msg["deflector"] = victimSpeaker;
                        hitTarget = false;
                        fsm.TransitionTo<DeflectionBounceState>(msg);
                    }
                }
            }

            if (hitTarget)
            {
                msg["victim"] = victim;
                fsm.TransitionTo<StrikeBounceState>(msg);
            }
        }

        return victim != null; //true means hit, false means no hit
    }

    override public void PhysicsProcess()
    {
        base.PhysicsProcess();
        cooldownTracker -= 1;
        if (cooldownTracker< 0) cooldownTracker = 0;
        if (GameManager.inSpecialStop || !echo.ballActive || echo.GetTarget() == null || cooldownTracker > 0) { return; }
        if (HitboxCollisionLogic()) return;

        TerrainCollisionLogic();

        CurveLogic();
    }

    void CurveLogic()
    {
        var currentDir = echo.velocityManager.GetTotalSpeed().normalized;
        var desiredDir = (echo.GetTarget().transform.position - echo.transform.position).normalized;

        float speedAsPercent = Mathf.Clamp01( (echo.GetSpeed() - echoData.minSpeed) / (echoData.maxSpeed - echoData.minSpeed));

        float steerForce = Mathf.Lerp(minSteerForce, maxSteerForce, speedAsPercent) * Time.fixedDeltaTime;

        float distanceToTarget = Vector3.Distance(echo.transform.position, echo.GetTarget().transform.position);
        float proximityFactor;
        if (distanceToTarget > maxProximityDistance || distanceToTarget < minProximityDistance) proximityFactor = 0;
        else proximityFactor = 1.0f - Mathf.InverseLerp(minProximityDistance, maxProximityDistance, distanceToTarget);
        steerForce += proximityFactor * promixityMultiplier * Time.fixedDeltaTime;

        var newDir = Vector3.Slerp(currentDir, desiredDir, steerForce);

        if (IsGrounded()) newDir.y = Mathf.Max(0, newDir.y); //don't go down if grounded

        float currentSpeed = echo.GetSpeed();

        float speedFactor = 1 - proximityFactor;
        if (speedFactor < minSpeedFactor) speedFactor = minSpeedFactor;
        currentSpeed *= speedFactor;

        Vector3 externalSpeeds = Vector3.zero;
         foreach (var speed in echo.velocityManager.GetAllExternalSpeed())
        {
            externalSpeeds += speed.Value;
        }

        echo.velocityManager.OverwriteInternalSpeed((newDir * currentSpeed) + externalSpeeds);

       // Debug.Log("Distance to target is " + distanceToTarget + " with proximity factor of " + proximityFactor);
    }



    void TerrainCollisionLogic()
    {
        Vector3 terrainVector = echo.transform.position - previousPos;
        Ray terrainRay = new(previousPos, terrainVector.normalized);
        float rayDistance = terrainVector.magnitude;
        if (Physics.Raycast(terrainRay, out RaycastHit hit, rayDistance, terrainMask))
        {
            echo.transform.position = hit.point;
            fsm.TransitionTo<TerrainBounceState>();
        }
        previousPos = echo.transform.position;
    }


    public override void Exit()
    {
        base.Exit();
    }

    public override void OnBallIgnited()
    {
        echo.velocityManager.OverwriteInternalSpeed((echo.GetTarget().transform.position - transform.position).normalized * echo.GetSpeed());
    }







}
