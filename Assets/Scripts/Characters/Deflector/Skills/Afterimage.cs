using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class Afterimage : SpeakerBaseSkill
{
    [Header("Clone Variables")]

    [SerializeField] AfterimageClone cloneObject;
    [SerializeField] MeshFilter cloneMesh;
    [SerializeField] float maxChargeDuration = 1.5f;
    [SerializeField] float maxClonePlacement = 125.0f;
    [SerializeField] float minDistanceFromWall = 3.0f; //offset from wall to prevent clipping
    [SerializeField] int activeCloneStaminaDrain = 8;

    [SerializeField] float chargeDuration = 4.5f;
    public int chargedDeflectParrystop = 12;



    [Header("Run Variables")]

    [SerializeField] float moveSpeed = 12.0f;
    [SerializeField] float moveAcceleration = 12.0f / 7.0f;



    [Header("Particle Effects")]
    [SerializeField] ParticleSystem warplines;
    [SerializeField] float warplineMoveDuration = 0.4f;

    [Header("Other")]
    [SerializeField] ProgressBar chargeMeter;
    [SerializeField] int numberOfFramesToIdleBeforePositionReset = 15;

    CinemachineTargetGroup targetGroup;

    int idleFrames = 0;
    int timeUntilDrain = 0;

    

    float placementTracker = 0.0f;
    float chargeTracker = 0.0f;

    Vector3 moveDir;

    bool placingClone = true;
    bool wasPlacingBeforeFreeze = false;

    LayerMask wallMask;


    BaseEcho deflectTarget;
    public override void InitState(BaseCharacter cha, CharacterStateMachine s_machine)
    {
        base.InitState(cha, s_machine);
        targetGroup = FindFirstObjectByType<CinemachineTargetGroup>();
        cloneObject.transform.parent = null; // it shouldn't follow the player around
        DestroyClone();
        wallMask = LayerMask.GetMask("Wall");
        warplines.transform.parent = null;

        var echo = FindFirstObjectByType<BaseEcho>();

        if (echo != null)
        {
            deflectTarget = echo;
        }

        cloneObject.Disable();

    }

    public override void Enter(Dictionary<string, object> msg = null)
    {
        placementTracker = 0.0f;
        Debug.Log("Entered afterimage state");
        base.Enter(msg);
        placingClone = !cloneObject.IsActive();
        skillBuffer.Consume();
        if (!placingClone)
        {
            StartCoroutine(SwapEchoWithClone());
        }
        else
        {
            cloneObject.ShowMesh();
        }
        if (!staminaComponent.HasForesight()) staminaComponent.DamageStamina(staminaCost, 0, false);
     }


    public override void Process()
    {
        moveDir = GetMovementDir();
        if (placingClone)
        {
            placementTracker += Time.deltaTime;
            if (placementTracker > maxChargeDuration) { placementTracker = maxChargeDuration; }


            float maxDistance = maxClonePlacement;
            Ray wallRay = new(character.transform.position, moveDir);
            if (Physics.Raycast(wallRay, out RaycastHit hit, maxDistance, wallMask))
            {
                maxDistance = hit.distance - minDistanceFromWall;
            }

            float t = placementTracker / maxChargeDuration;
            Vector3 spawnPos = character.transform.position;
        
            if (idleFrames > numberOfFramesToIdleBeforePositionReset)
            {
                spawnPos = character.transform.position;
                idleFrames = 0;
            }
            
            else spawnPos = Vector3.Lerp(_rbCollider.bounds.center, character.transform.position + (moveDir * maxDistance), t);

            cloneObject.transform.position = spawnPos;
            cloneObject.transform.forward = moveDir;

            if (!skillAction.IsPressed())
            {
                PlaceClone();
            }
        }
      
    }
    void PlaceClone()
    {
        placingClone = false;
        cloneObject.Enable();
        chargeTracker = 0;
        OnSkillOver();
        cloneObject.transform.rotation = character.transform.rotation;
        idleFrames = 0;
        
    }
    public override void PhysicsProcess()
    {
        base.PhysicsProcess();
        Vector3 newSpeed = character.velocityManager.GetInternalSpeed();
        newSpeed += moveDir.normalized * moveAcceleration;

        newSpeed = Vector3.ClampMagnitude(newSpeed, moveSpeed);
        if (placingClone)
        {
            newSpeed.y = 0;
        }
        character.velocityManager.OverwriteInternalSpeed(newSpeed);

        if (CancelSkillIfOppositeSkillBuffered()) return;
        DrainStamina();

        if (moveDir.magnitude < MOVE_DEADZONE) idleFrames += 1;
        else idleFrames = 0;

    }
    public IEnumerator SwapEchoWithClone()
    {
        if (deflectTarget == null) { yield break; }
        DestroyClone();
        yield return null;
        Vector3 oldPos = deflectTarget.transform.position;
        deflectTarget.WarpToLocation(cloneObject.transform.position);
        deflectTarget.velocityManager.OverwriteInternalSpeed((deflectTarget.GetTarget().transform.position - deflectTarget.transform.position).normalized * deflectTarget.GetSpeed());

        warplines.transform.position = oldPos;
        yield return null;
        staminaComponent.ConsumeForesight();
        OnSkillOver();
        warplines.transform.LookAt(deflectTarget.transform.position);
        warplines.transform.DOMove(deflectTarget.transform.position, warplineMoveDuration);
    }
    public void DestroyClone()
    {
        chargeTracker = 0;
        cloneObject.Disable();
        if (targetGroup != null)
        {
            targetGroup.RemoveMember(cloneObject.transform);
        }
    }
    protected override void OnSkillOver()
    {
        timeUntilDrain = activeCloneStaminaDrain;
        if (targetGroup != null && cloneObject.IsActive())
        {
            targetGroup.AddMember(cloneObject.transform, 1.0f, 5.0f);
        }
        base.OnSkillOver();
    }
     
    public override void InactivePhysicsProcess()
    {
        if (!cloneObject.IsActive()) { return;  }

        DrainStamina();
    }

    public override void InactiveProcess()
    {
        ChargeMeterLogic();
    }

    void ChargeMeterLogic()
    {
        if (placingClone) return;
        chargeTracker += Time.deltaTime;
        if (chargeTracker > chargeDuration)
        {
            chargeTracker = chargeDuration;
            Debug.Log("fully charged!");
        }

        float chargeAsPercent = chargeTracker / chargeDuration;
        chargeMeter.SetProgress(chargeAsPercent);
    }

    void DrainStamina()
    {
        timeUntilDrain -= 1;
        if (timeUntilDrain <= 0)
        {
            timeUntilDrain = activeCloneStaminaDrain;
           if (!staminaComponent.HasForesight()) staminaComponent.DamageStamina(1, 0, false);
            if (staminaComponent.GetStamina() <= staminaCost && !staminaComponent.HasForesight())
            {
                Debug.Log("Destroying clone, ran outta stamina ");
                DestroyClone();
            }
        }
    }

    public override void OnSpecialStopStarted()
    {
        wasPlacingBeforeFreeze = fsm.currentState == this;
    }
    public override void ResetSkill()
    {
        DestroyClone();
    }

    public bool DeflectFullyCharged()
    {
        return (chargeTracker / chargeDuration) > 0.999f;
    }

    public override bool SkillAvailable()
    {
        if (!wasPlacingBeforeFreeze && (GameManager.inSpecialStop || GameManager.frameAfterSpecialStop))
        {
            return false;
        }
        return base.SkillAvailable();
    }
}
