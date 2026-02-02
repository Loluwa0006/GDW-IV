using System.Collections.Generic;
using UnityEngine;

public class Pivot : SpeakerBaseSkill
{

    [SerializeField] LayerMask allowedLayers;

    [Header("Particles")]
    [SerializeField] ParticleSystem bounceParticles;
    [SerializeField] ParticleSystem pivotingParticles;
    [Header("Stamina")]
    [SerializeField] int staminaDrain = 12;
    [SerializeField] int staminaDrainGracePeriod = 8;



    [Header("Air Control")]
    [SerializeField] float bounceControl = 0.65f;
    [SerializeField] float strafeControl = 0.7f;
    [SerializeField] float gravity = 0.2f;
    [SerializeField] float maxFallSpeed = -40;

    [Header("Power")]

    [SerializeField] float redirectPower = 0.1f;

    [Header("QOL")]
    [SerializeField] float redirectRange = 3.0f;

    [Header("Sound")]
    [SerializeField] List<AudioClip> bounceSFX;
    [SerializeField] float bounceVolume = 1.15f;

    [Header("Tackle")]
    [SerializeField] LayerMask tackleMask;
    [SerializeField] HitboxComponent hitbox;


    List<HealthComponent> struckTargets = new();

    int frameTracker = 0;
    bool inGrace = false;
    bool fastfall = false;



    Vector3 moveDir = new();
    Vector3 FAILED_RAYCAST_VALUE = new(-1, -1, -1);
    Vector3 internalVelocity = new();



    public struct RaycastData
    {
        public Vector3 normal;
        public Vector3 point;
    }

    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm)
    {
        base.InitState(cha, fsm);
        maxFallSpeed = Mathf.Abs(maxFallSpeed) * -1; //make sure its negative;
        gravity = Mathf.Abs(gravity);
        pivotingParticles.Clear();
        pivotingParticles.Stop();
    }

    public override void Enter(Dictionary<string, object> msg = null)
    {
        base.Enter(msg);
        struckTargets.Clear();
        inGrace = true;
        frameTracker = 0;
        if (!staminaComponent.HasForesight())
        {
            staminaComponent.DamageStamina(staminaCost, 0, false);
        }     
        pivotingParticles.Play();
        fastfall = false;
    }

    public override void Process()
    {
        moveDir = GetMovementDir();
        if (skillAction.WasPerformedThisFrame())
        {
            fastfall = true;
        }
    }

    public override void PhysicsProcess()
    {
        internalVelocity = character.velocityManager.GetInternalSpeed();
        base.PhysicsProcess();
        frameTracker++;
        if (frameTracker >= staminaDrainGracePeriod && inGrace)
        {
            inGrace = false;
        }

        AddGravity();

        if (!inGrace)
        {
            if (frameTracker % staminaDrain ==0)
            {
                if (!staminaComponent.HasForesight()) staminaComponent.DamageStamina(1, 0, false);
                frameTracker = 0;
                if (staminaComponent.GetStamina() <= staminaCost && !staminaComponent.HasForesight()) 
                {
                    OnSkillOver();
                    return;
                }
            }
        }
        RaycastData data = PerformRaycast();
        if (data.normal != FAILED_RAYCAST_VALUE)
        {
            Debug.Log("BONCING YIPPE");
            PerformRedirect(data.normal);
            CreateParticles(data);
            character.unscaledAudioSource.PlayOneShot(GetRandomBounceSound(), bounceVolume);
            OnSkillOver();
            return;
        }
       

        if (IsGrounded() )
        {
            Debug.Log("Exiting to move states because you let go of the button while grounded ");
            if (moveDir.magnitude > MOVE_DEADZONE)
            {
                fsm.TransitionTo<RunState>();
            }
            else
            {
                fsm.TransitionTo<IdleState>();
            }
            return;
        }
      

        if (moveDir.magnitude > MOVE_DEADZONE)
        {
            AirStrafeLogic();
        }

        if (CancelSkillIfOppositeSkillBuffered()) return;
        HitboxCollisionLogic();
    }

    void HitboxCollisionLogic()
    {
        hitbox.damageInfo.knockbackDir = character.velocityManager.GetInternalSpeed().normalized;
        var overlap = Physics.OverlapBox(hitbox.hitboxCollider.bounds.center, hitbox.hitboxCollider.bounds.size, hitbox.transform.rotation, tackleMask, QueryTriggerInteraction.Collide);
        List<HealthComponent> newVictims = new();
        foreach (var obj in overlap)
        {
            if (!obj.transform.TryGetComponent(out HealthComponent hp)) continue;
            else if (hp == speaker.healthComponent) continue;
            else if (struckTargets.Contains(hp)) continue;
            Debug.Log("Found tackle victim: " + hp.hurtboxOwner.name);
            struckTargets.Add(hp);
            newVictims.Add(hp);
        }
        bool hitEntity = false;
        foreach (var victim in newVictims)
        {
            Debug.Log("Tackling " + victim.name + " with dash");
            victim.Damage(hitbox.damageInfo);
            if (victim.ownedByEntity) hitEntity = true;
        }
        if (hitEntity) GameManager.ApplySpecialStop(hitbox.damageInfo.hitstop);
    }

    void AddGravity()
    {
        if (fastfall)
        {
            internalVelocity.y = maxFallSpeed;
        }
        else if (internalVelocity.y > maxFallSpeed)
        {
            Debug.Log("Applying gravity");
            internalVelocity.y -= gravity;
            if (internalVelocity.y < maxFallSpeed)
            {
                internalVelocity.y = maxFallSpeed;
            }
        }
        character.velocityManager.OverwriteInternalSpeed(internalVelocity);
        Debug.Log("added " + gravity + "to create new speed " + internalVelocity.y);

    }



    protected void AirStrafeLogic()
    {
        if (moveDir.magnitude < MOVE_DEADZONE) { return; }
        Vector3 influenceVector = moveDir ; // current dir
        Vector3 adjustedDir;
        Debug.Log("Current redirect speed is " + internalVelocity);
        if (internalVelocity.magnitude > 0.01f)
        {
            adjustedDir = Vector3.Lerp(internalVelocity.normalized, influenceVector, strafeControl); // current moved towards wanted by % strafe control
            Vector3 adjustedVector = adjustedDir * internalVelocity.magnitude;
            Vector3 finalVector = new Vector3(adjustedVector.x, internalVelocity.y, adjustedVector.z).normalized * internalVelocity.magnitude;
            character.velocityManager.OverwriteInternalSpeed(finalVector); // apply new speed
            Debug.Log("New redirect speed is " + finalVector);

        }


        foreach (var velocity in character.velocityManager.GetAllExternalSpeed()) //now do the same thing to every other velocity velocity 
        {
            if (velocity.Value.magnitude < 0.01f) continue;
            adjustedDir = Vector3.Lerp(velocity.Value.normalized, influenceVector, strafeControl);
            character.velocityManager.OverwriteExternalSpeed(velocity.Key, adjustedDir * velocity.Value.magnitude);
        }
    }

    RaycastData PerformRaycast()
    {
        RaycastData data = new ();
        Vector3 raycastDir = character.velocityManager.GetTotalSpeed().normalized;
        Ray ray = new (_rbCollider.bounds.center, raycastDir);

        Debug.Log("Aiming in direction " + raycastDir.ToString());
       
        if (Physics.Raycast(ray, out RaycastHit hit, redirectRange, allowedLayers, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.gameObject == character) //can't hit self
            {
                data.normal = FAILED_RAYCAST_VALUE;
            }
            else
            {
                Debug.Log("redirected off object " + hit.collider.name + " with normal of " + hit.normal);
                data.normal = hit.normal;
            }
            data.point = hit.point;
        }
        else {
            data.normal = FAILED_RAYCAST_VALUE;
            data.point = FAILED_RAYCAST_VALUE;
        }
        Color rayColor = data.normal == FAILED_RAYCAST_VALUE ? Color.red : Color.green;
        Debug.DrawRay(_rbCollider.bounds.center, raycastDir  * redirectRange, rayColor);
        return data;

    }
    public void PerformRedirect(Vector3 normal)
    {

        Vector3 influenceVector = moveDir; // direction player wants to go
        Vector3 velocityVector = internalVelocity; //direction player is currently going
        float newMagnitude = velocityVector.magnitude + (velocityVector.magnitude * redirectPower); // new speed is current speed gets redirectPower% faster
        Vector3 reflectedDir = Vector3.Reflect(velocityVector, normal).normalized; // get the bounce angle using velocity vector
        if (influenceVector.magnitude > MOVE_DEADZONE)
        {
            reflectedDir = Vector3.Lerp(reflectedDir, influenceVector, bounceControl); //now we get a value inbetween player dir and wanted dir using bounce control as a percent. 
        }                                                                              //i.e. if bounce control is 65%, we're gonna make the velocity vector 65% closer to wanted dir

        character.velocityManager.OverwriteInternalSpeed(reflectedDir * newMagnitude); // then we apply the velocity using the new dir and bonus we calculated

        Debug.Log("Reflected char velocity from " + velocityVector + " to new vector " + reflectedDir * newMagnitude);
        foreach (var velocity in character.velocityManager.GetAllExternalSpeed()) //now do the same thing to every other velocity velocity 
        {
            velocityVector = character.velocityManager.GetExternalSpeed(velocity.Key);
            newMagnitude = velocityVector.magnitude + (velocityVector.magnitude * redirectPower);
            reflectedDir = Vector3.Reflect(velocity.Value, normal).normalized;
           
          if (influenceVector.magnitude > MOVE_DEADZONE)  reflectedDir = Vector3.Lerp(reflectedDir, influenceVector, bounceControl);
            character.velocityManager.OverwriteExternalSpeed(velocity.Key, reflectedDir * newMagnitude);

        }

        staminaComponent.ConsumeForesight();


    
    }

    void CreateParticles(RaycastData data)
    {
        ParticleSystem newParticles = Instantiate(bounceParticles);
        newParticles.transform.position = data.point;
        newParticles.transform.rotation = Quaternion.LookRotation(data.normal);
    }

    public override void Exit()
    {
        pivotingParticles.Clear();
        pivotingParticles.Stop();
        skillBuffer.Consume();
    }
    public override bool SkillAvailable()
    {
        return base.SkillAvailable() && !IsGrounded(); //redirecting
    }


    public AudioClip GetRandomBounceSound()
    {
        int index = Random.Range(0, bounceSFX.Count);
        return bounceSFX[index];
    }
}
