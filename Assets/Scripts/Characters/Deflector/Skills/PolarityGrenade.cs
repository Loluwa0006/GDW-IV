using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.ProBuilder.MeshOperations;
public class PolarityGrenade : MonoBehaviour
{
    [SerializeField] MeshRenderer grenadeModel;
    [SerializeField] LayerMask grenadeMask;
    [SerializeField] Collider pullCollider;
    [SerializeField] Collider grenadeCollider;
    [SerializeField] Polarity stateOwner;
    [SerializeField] VelocityManager velocityManager;

    [Header("Grenade Attributes")]
    [SerializeField] float grenadePower = 20.0f; //strength of grenade's pull
    [SerializeField] float collisionSafeMargin = 0.1f;
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

    string grenadeSpeedSource;


    public void InitProjectile()
    {
        speakerMask = LayerMask.GetMask("Speaker");
        grenadeSpeedSource = "PolarityGrenade" + stateOwner.speaker.name;
    }

    public void OnGrenadeThrown()
    {
        effectDisplay.enabled = false;
        state = GrenadeState.Travelling;
        grenadeModel.enabled = true;
    }
    public void UpdateProjectile()
    {
        switch (state)
        {
            case GrenadeState.Travelling:
                var info = ProjectileHelper.CollisionLogic(previousGrenadePosition, transform.position, grenadeMask, grenadeCollider);
                previousGrenadePosition = transform.position;
                if (info.collider != null)
                {
                    Debug.Log("Polarity grenade hit collider " + info.collider.name);
                    if (info.collider == grenadeCollider) return;

                    var safeMarginAdjustment = info.point + info.normal * collisionSafeMargin;
                    transform.position = safeMarginAdjustment;
                    ActivateGrenade();
                }
                break;
            case GrenadeState.Attracting:
            case GrenadeState.Repulsing:
                ActiveGrenadeLogic();
                var speakerCheck = ProjectileHelper.GetOverlappingEntities<BaseSpeaker>(grenadeCollider, speakerMask, false);
                if (speakerCheck.Contains(stateOwner.speaker))
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
                entity.RemoveExternalSpeedSource(grenadeSpeedSource);
        }

        float pull = state == GrenadeState.Attracting ? grenadePower : -grenadePower;

        foreach (var entity in pulledEntities)
        {
            Debug.Log("Polarity grenade affecting entity " + entity.name);
            if (entity == velocityManager) continue;
            Vector3 prevSpeed = entity.GetTotalSpeed();
            if (entity.GetExternalSpeed(grenadeSpeedSource) == VelocityManager.MISSING_VELOCITY_VALUE)
            {
                entity.AddExternalSpeed((transform.position - entity.transform.position).normalized * pull, grenadeSpeedSource);
            }
            else
            {
                entity.EditExternalSpeed(grenadeSpeedSource, (entity.transform.position - transform.position).normalized * pull);
            }
            Debug.Log("Entity " + entity.name + " speed changed from " + prevSpeed + " to " + entity.GetTotalSpeed());

        }
    }

    public void HolsterGrenade()
    {
        foreach (var entity in pulledEntities)
        {
            entity.RemoveExternalSpeedSource(grenadeSpeedSource);
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
        state = GrenadeState.Attracting;
        velocityManager.OverwriteInternalSpeed(Vector3.zero);
        effectDisplay.enabled = true;
        effectDisplay.material = attractMaterial;
        grenadeModel.enabled = true;
    }

}
