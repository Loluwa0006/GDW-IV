using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class Afterimage : SpeakerBaseSkill
{
    [Header("Clone Variables")]

    [SerializeField] AfterimageClone cloneObject;
    [SerializeField] MeshFilter cloneMesh;
    [SerializeField] int maxChargeDuration = 90;
    [SerializeField] float maxClonePlacement = 125.0f;
    [SerializeField] float minDistanceFromWall = 3.0f; //offset from wall to prevent clipping
    [SerializeField] int activeCloneStaminaDrain = 8;

    [SerializeField] int chargeDuration = 270;
    public int chargedDeflectParrystop = 12;

    [Header("Run Variables")]

    [SerializeField] float maxSpeed = 12.0f;
    [SerializeField] int accelerationFrames = 7;

    [Header("Particle Effects")]
    [SerializeField] ParticleSystem warplines;
    [SerializeField] float warplineMoveDuration = 0.4f;

    [Header("Other")]
    [SerializeField] ProgressBar chargeMeter;
    [SerializeField] int numberOfFramesToIdleBeforePositionReset = 15;

    CinemachineTargetGroup targetGroup;

    float moveSpeed;

    int idleFrames = 0;
    int timeUntilDrain = 0;

    

    int placementTracker = 0;
    int chargeTracker = 0;

    Vector3 moveDir;

    bool placingClone = true;
    bool wasPlacingBeforeFreeze = false;

    LayerMask wallMask;


    BaseEcho deflectTarget;
    GameManager gameManager;

    [HideInInspector] public short cloneID;
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        gameManager = manager;
        base.InitState(cha, fsm, manager);
        DestroyClone();
        wallMask = LayerMask.GetMask("Wall");
        warplines.transform.parent = null;
        var echo = FindObjectsByType<BaseEcho>(FindObjectsSortMode.InstanceID);
        if (echo.Length > 0)
        {
            deflectTarget = echo[0];
        }
        targetGroup = gameManager.cameraManager.GetTargetGroup();
        moveSpeed = maxSpeed / (float) accelerationFrames;

        cloneObject.InitClone(manager);
    }

    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {

        placementTracker = 0;
        base.EnterSimulated(msg);
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
        if (!staminaComponent.ForesightEnabled) staminaComponent.DamageStamina(staminaCost, 0, false);
     }


    public override void Process()
    {
        moveDir = GetMovementDir();
        if (placingClone)
        {
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
        newSpeed += moveDir.normalized * moveSpeed;

        newSpeed = Vector3.ClampMagnitude(newSpeed, maxSpeed);
        if (placingClone)
        {
            newSpeed.y = 0;
            placementTracker++;
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
        //Vector3 oldPos = deflectTarget.transform.position;
        deflectTarget.WarpToLocation(cloneObject.transform.position);
        deflectTarget.velocityManager.OverwriteInternalSpeed((deflectTarget.GetTarget().transform.position - deflectTarget.transform.position).normalized * deflectTarget.CurrentSpeed);

       // warplines.transform.position = oldPos;
        staminaComponent.ConsumeForesight();
        OnSkillOver();
        //warplines.transform.LookAt(deflectTarget.transform.position);
      //  warplines.transform.DOMove(deflectTarget.transform.position, warplineMoveDuration);
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
        ChargeMeterLogic();
    }


    void ChargeMeterLogic()
    {
        if (placingClone) return;
        chargeTracker++;
        if (chargeTracker > chargeDuration)
        {
            chargeTracker = chargeDuration;
        }

        float chargeAsPercent = chargeTracker / chargeDuration;
        chargeMeter.SetProgress(chargeAsPercent);
    }

    void DrainStamina()
    {
        timeUntilDrain--;
        if (timeUntilDrain <= 0)
        {
            timeUntilDrain = activeCloneStaminaDrain;
            if (!staminaComponent.ForesightEnabled) staminaComponent.DamageStamina(1, 0, false);
            if (staminaComponent.Stamina <= staminaCost && !staminaComponent.ForesightEnabled)
            {
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
        if (!wasPlacingBeforeFreeze && (gameManager.hitstopManager.InSpecialStop || gameManager.hitstopManager.FrameAfterSpecialStop))
        {
            return false;
        }
        return base.SkillAvailable();
    }
}
