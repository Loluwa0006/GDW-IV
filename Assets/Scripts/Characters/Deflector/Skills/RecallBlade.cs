using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.Rendering;
using UnityEngine.TerrainUtils;
using static UnityEngine.EventSystems.EventTrigger;

public class RecallBlade : MonoBehaviour
{
    public enum BladeState
    {
        Flying,
        Attached, 
        Holstered,
        Deactivated
    }

    public BladeState status;
    [SerializeField] Recall recalState;
    [SerializeField] VelocityManager velocityManager;
    [SerializeField] Collider rbCollider;
    [SerializeField] MeshRenderer model;
    [Header("Throw Attributes")]
    [SerializeField] float flySpeed = 20.0f;
    [SerializeField] float safeMargin = 0.6f;
    [SerializeField] float steerForce = 0.4f;
    [SerializeField] LayerMask terrainMask;
    [Header("Attached Attributes")]
    [SerializeField] LayerMask holsterMask;


    Vector3 previousBladePos;

    public void PhysicsUpdate()
    {
        switch (status)
        {
            case BladeState.Flying:
                CheckForCollisions();
                break;
        }
    }

    public void Process()
    {
        if (model.enabled) model.transform.rotation = Quaternion.LookRotation(velocityManager.GetTotalSpeed().normalized);
    }
    public void ThrowBlade(Vector3 dir)
    {
        transform.position = recalState.speaker.transform.position;
        transform.parent = null;
        status = BladeState.Flying;
        model.enabled = true;
        velocityManager.freeze = false;
        velocityManager.OverwriteInternalSpeed(dir * flySpeed);
    }


    public void SteerFlight(Vector3 desired)
    {
        Vector3 speedVector = velocityManager.GetInternalSpeed();

        Vector3 dir = speedVector.normalized;
        float speed = speedVector.magnitude;

        Vector3 final = Vector3.Slerp(dir, desired, steerForce * Time.fixedDeltaTime); 
        final *= speed;

        velocityManager.OverwriteInternalSpeed(final);

    }

    public void CheckForCollisions()
    {
        Vector3 travelVector = transform.position - previousBladePos;
        float checkerDistance = travelVector.magnitude + + rbCollider.bounds.size.x + safeMargin;
        if (checkerDistance < 0.001f) return;

        Ray ray = new(previousBladePos, travelVector.normalized);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, checkerDistance, terrainMask))
        {
            Debug.Log("Locking hook since it hit " + hitInfo.transform.name);
            //  ConnectHookToObject(hitInfo);
            velocityManager.freeze = true;
            status = BladeState.Attached;
        }
    }

    public void Holster()
    {
        status = BladeState.Holstered;
        model.enabled = false;
        transform.parent = recalState.transform;
        velocityManager.ClearExternalSpeed();
        velocityManager.ClearInternalSpeed();
        transform.localPosition = Vector3.zero;
    }
}