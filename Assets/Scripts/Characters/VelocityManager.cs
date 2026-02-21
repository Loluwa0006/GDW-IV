using UnityEngine;
using System.Collections.Generic;

public class VelocityManager : MonoBehaviour, ISimulated, ISimulationSnapshot<VelocitySnapshot>, ISnapshotable
{
    public static Vector3 MISSING_VELOCITY_VALUE = new (-1.0f, -1.0f, -1.0f);

    [SerializeField] Rigidbody _rb;

    Vector3 intervalVelocity;

    SortedDictionary<VelocityIDRegistry, Vector3> externalVelocities = new();

   [HideInInspector] public bool freeze = false;

    GameManager gameManager;

    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Collision; set { } }
    public bool UpdateDuringHitstop { get => true; set { } }

    VelocitySnapshot[] velocitySnapshots = new VelocitySnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];
    public void InitManager(GameManager manager)
    {
        gameManager = manager;
        InitSimulated(manager.simulationManager);
    }
    public void AddInternalVelocity(Vector3 speed)
    {
        intervalVelocity += speed;
    }
    public void AddExternalSpeed(Vector3 speed, VelocityIDRegistry source)
    {
        externalVelocities[source] = speed;
    }
    public void EditExternalSpeed(VelocityIDRegistry source, Vector3 newSpeed)
    {
        if (externalVelocities.ContainsKey(source))
        {
            if (newSpeed == Vector3.zero)
            {
                externalVelocities.Remove(source);
            }
            else 
            {
                externalVelocities[source] = newSpeed;
            }
        }
    }
    public Vector3 GetInternalSpeed()
    {
        return intervalVelocity;
    }
    public Vector3 GetExternalSpeed(VelocityIDRegistry source)
    {
        if (externalVelocities.ContainsKey(source))
        {
            return externalVelocities[source];
        }
        return MISSING_VELOCITY_VALUE;
    }
    public SortedDictionary<VelocityIDRegistry, Vector3> GetAllExternalSpeed()
    {
        return externalVelocities;
    }
    public void RemoveExternalSpeedSource(VelocityIDRegistry source)
    {
        if (externalVelocities.ContainsKey(source))
        {
            externalVelocities.Remove(source);
        }
    }
    public void ClearExternalSpeed()
    {
        externalVelocities.Clear();
    }
    public void ClearInternalSpeed()
    {
        intervalVelocity = Vector3.zero;
    }
    public void OverwriteInternalSpeed(Vector3 newSpeed)
    {
        intervalVelocity = newSpeed;
    }
    public void OverwriteExternalSpeed(VelocityIDRegistry source, Vector3 newSpeed)
    {
        if (externalVelocities.ContainsKey(source))
        {
            externalVelocities[source] = newSpeed;
        }
    }
    public void ClampInternalVelocity(float length)
    {
        intervalVelocity = Vector3.ClampMagnitude(intervalVelocity, length);
    }
    public Vector3 GetTotalSpeed()
    {
        Vector3 finalVelocity = intervalVelocity;
        foreach (var v in externalVelocities.Keys)
        {
            finalVelocity += externalVelocities[v];
        }
        return finalVelocity;
    }
    public void ResetComponent()
    {
        intervalVelocity = Vector3.zero;
        externalVelocities.Clear();
    }
    public void InitSimulated(SimulationManager simulationManager)
    {
        simulationManager.AddSimulatedObject(this);
    }
    public VelocitySnapshot CaptureState()
    {
        VelocitySnapshot snapshot = new ();
        int index = 0;
        foreach (var kvp in externalVelocities)
        {
            switch (index)
            {
                case 0: snapshot.e1.ID = kvp.Key; snapshot.e1.velocity = kvp.Value; break;
                case 1: snapshot.e2.ID = kvp.Key; snapshot.e2.velocity = kvp.Value; break;
                case 2: snapshot.e3.ID = kvp.Key; snapshot.e3.velocity = kvp.Value; break;
                case 3: snapshot.e4.ID = kvp.Key; snapshot.e4.velocity = kvp.Value; break;
                case 4: snapshot.e5.ID = kvp.Key; snapshot.e5.velocity = kvp.Value; break;
                case 5: snapshot.e6.ID = kvp.Key; snapshot.e6.velocity = kvp.Value; break;
                case 6: snapshot.e7.ID = kvp.Key; snapshot.e7.velocity = kvp.Value; break;
                case 7: snapshot.e8.ID = kvp.Key; snapshot.e8.velocity = kvp.Value; break;
                case 8: snapshot.e9.ID = kvp.Key; snapshot.e9.velocity = kvp.Value; break;
                case 9: snapshot.e10.ID = kvp.Key; snapshot.e10.velocity = kvp.Value; break;
            }
        }
        snapshot.frozen = freeze;
        snapshot.currentVelocity = intervalVelocity;
        snapshot.numberOfExternalVelocities = externalVelocities.Count;
        snapshot.currentPosition = _rb.position;
        snapshot.currentRotation = _rb.rotation;
        return snapshot;
    }
    public void RestoreState(VelocitySnapshot snapshot)
    {
        externalVelocities.Clear();
        if (snapshot.numberOfExternalVelocities > 0)
        {
            externalVelocities[snapshot.e1.ID] = snapshot.e1.velocity;
            if (snapshot.numberOfExternalVelocities > 1)
            {
                externalVelocities[snapshot.e2.ID] = snapshot.e2.velocity;
            }
            if (snapshot.numberOfExternalVelocities > 2)
            {
                externalVelocities[snapshot.e3.ID] = snapshot.e3.velocity;
            }
            if (snapshot.numberOfExternalVelocities > 3)
            {
                externalVelocities[snapshot.e4.ID] = snapshot.e4.velocity;
            }
            if (snapshot.numberOfExternalVelocities > 4)
            {
                externalVelocities[snapshot.e5.ID] = snapshot.e5.velocity;
            }
            if (snapshot.numberOfExternalVelocities > 5)
            {
                externalVelocities[snapshot.e6.ID] = snapshot.e6.velocity;
            }
            if (snapshot.numberOfExternalVelocities > 6)
            {
                externalVelocities[snapshot.e7.ID] = snapshot.e7.velocity;
            }
            if (snapshot.numberOfExternalVelocities > 7)
            {
                externalVelocities[snapshot.e8.ID] = snapshot.e8.velocity;
            }
            if (snapshot.numberOfExternalVelocities > 8)
            {
                externalVelocities[snapshot.e9.ID] = snapshot.e9.velocity;
            }
            if (snapshot.numberOfExternalVelocities > 9)
            {
                externalVelocities[snapshot.e10.ID] = snapshot.e10.velocity;
            }
        }
        intervalVelocity = snapshot.currentVelocity;
        _rb.position = snapshot.currentPosition;
        _rb.rotation = snapshot.currentRotation;
        _rb.linearVelocity = Vector3.zero;
        freeze = snapshot.frozen;
    }
    public void SimulateUpdate(int currentTick)
    {
        if (gameManager.hitstopManager.InSpecialStop || freeze || gameManager.pauseManager.GamePaused())
        {
            _rb.linearVelocity = Vector3.zero;
        }
        else
        {
            Vector3 finalVelocity = intervalVelocity;
            foreach (var v in externalVelocities.Keys)
            {
                finalVelocity += externalVelocities[v];
            }
            _rb.linearVelocity = finalVelocity;
        }
    }

    public void CaptureCurrentState(int tick)
    {
        velocitySnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(velocitySnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }
}
public struct VelocitySnapshot
{
    public Vector3 currentVelocity;
    public Vector3 currentPosition;
    public Quaternion currentRotation;

    public ExternalVelocitySnapshot e1;
    public ExternalVelocitySnapshot e2;
    public ExternalVelocitySnapshot e3;
    public ExternalVelocitySnapshot e4;
    public ExternalVelocitySnapshot e5;
    public ExternalVelocitySnapshot e6;
    public ExternalVelocitySnapshot e7;
    public ExternalVelocitySnapshot e8;
    public ExternalVelocitySnapshot e9;
    public ExternalVelocitySnapshot e10;

    public bool frozen;
    public int numberOfExternalVelocities;
}
public struct ExternalVelocitySnapshot
{
    public Vector3 velocity;
    public VelocityIDRegistry ID;
}
public enum VelocityIDRegistry
{
    GrapplePull,
    PolarityGrenade
}