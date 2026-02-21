using System.Collections.Generic;
using UnityEngine;
public class PolarityGrenade : MonoBehaviour
{
    [SerializeField] MeshRenderer grenadeModel;
    [SerializeField] LayerMask grenadeMask;
    [SerializeField] Collider pullCollider;
    [SerializeField] Collider grenadeCollider;
    [SerializeField] Polarity polarityManager;
    [SerializeField] VelocityManager velocityManager;
    [SerializeField] Rigidbody rb;

    [Header("Grenade Attributes")]
    [SerializeField] float grenadePower = 20.0f; //strength of grenade's pull
    [SerializeField] float collisionSafeMargin = 0.1f;
    [SerializeField] int framesUntilGrenadeReturns = 60 * 5;
    [SerializeField] LayerMask pullMask;
    [Header("VFX")]
    [SerializeField] MeshRenderer effectDisplay;
    [SerializeField] Material attractMaterial;
    [SerializeField] Material repulseMaterial;


    List<VelocityManager> pulledEntities = new();
    public enum GrenadeState
    {
        Repulsing,
        Attracting, 
        Travelling,
        Holstered,
    }

   [HideInInspector] public GrenadeState state = GrenadeState.Holstered;


    Vector3 previousGrenadePosition;

    LayerMask speakerMask;

    int grenadeReturnTracker = 0;


    public void InitProjectile()
    {
        speakerMask = LayerMask.GetMask("Speaker");
    }

    public void OnGrenadeThrown()
    {
        rb.MovePosition(polarityManager.character.transform.position);
        effectDisplay.enabled = false;
        state = GrenadeState.Travelling;
        grenadeModel.enabled = true;
    }
    public void UpdateProjectile()
    {
        switch (state)
        {
            case GrenadeState.Travelling:
                var info = ProjectileHelper.CollisionLogic(previousGrenadePosition, transform.position, grenadeMask, grenadeCollider, QueryTriggerInteraction.Ignore);
                previousGrenadePosition = transform.position;
                if (info.collider != null)
                {
                  
                    if (info.collider == grenadeCollider) return;

                    var safeMarginAdjustment = info.point + info.normal * collisionSafeMargin;
                    transform.position = safeMarginAdjustment;
                    ActivateGrenade();
                }
                else if (!polarityManager.IsActionPressed())
                {
                    ActivateGrenade();
                }
                    break;
            case GrenadeState.Attracting:
            case GrenadeState.Repulsing:
                ActiveGrenadeLogic();
                var speakerCheck = ProjectileHelper.GetOverlappingEntities<BaseSpeaker>(grenadeCollider, speakerMask, false);
                if (speakerCheck.Contains(polarityManager.speaker))
                {
                    HolsterGrenade();
                }
                break;
        }
    }

    void ActiveGrenadeLogic()
    {
        List<VelocityManager> previousEntityList = new(pulledEntities);
        pulledEntities = ProjectileHelper.GetOverlappingEntities<VelocityManager>(pullCollider, pullMask, true);
        HashSet<int> currentIds = new();
        foreach (var e in pulledEntities)
            currentIds.Add(e.GetInstanceID());

        foreach (var entity in previousEntityList)
        {
            if (!currentIds.Contains(entity.GetInstanceID()))
                entity.RemoveExternalSpeedSource(VelocityIDRegistry.PolarityGrenade);
        }

        float pull = state == GrenadeState.Attracting ? grenadePower : -grenadePower;

        foreach (var entity in pulledEntities)
        {
            if (entity == velocityManager) continue;
            if (entity.GetExternalSpeed(VelocityIDRegistry.PolarityGrenade) == VelocityManager.MISSING_VELOCITY_VALUE)
            {
                entity.AddExternalSpeed((transform.position - entity.transform.position).normalized * pull, VelocityIDRegistry.PolarityGrenade);
            }
            else
            {
                entity.EditExternalSpeed(VelocityIDRegistry.PolarityGrenade, (entity.transform.position - transform.position).normalized * pull);
            }
        }

        grenadeReturnTracker += 1;
        if (grenadeReturnTracker > framesUntilGrenadeReturns)
        {
            HolsterGrenade();
        }

    }

    public void HolsterGrenade()
    {
        foreach (var entity in pulledEntities)
        {
            entity.RemoveExternalSpeedSource(VelocityIDRegistry.PolarityGrenade);
        }
        state = GrenadeState.Holstered;
        effectDisplay.enabled = false;
        grenadeModel.enabled = false;
    }


    public void SwapMode()
    {
        if (state == GrenadeState.Attracting)
        {
            state = GrenadeState.Repulsing;
            effectDisplay.material = repulseMaterial;
        }
        else
        {
            state = GrenadeState.Attracting;
            effectDisplay.material = attractMaterial;
        }
    }

    public void ActivateGrenade()
    {
        grenadeReturnTracker = 0;
        state = GrenadeState.Attracting;
        velocityManager.OverwriteInternalSpeed(Vector3.zero);
        effectDisplay.enabled = true;
        effectDisplay.material = attractMaterial;
        grenadeModel.enabled = true;
    }



}
