using UnityEngine;
using System.Collections.Generic;

public class VelocityManager : MonoBehaviour
{
    public static Vector3 MISSING_VELOCITY_VALUE = new (-1.0f, -1.0f, -1.0f);

    [SerializeField] Rigidbody _rb;

    Vector3 intervalVelocity;

    Dictionary<string, Vector3> externalVelocities = new();

   [HideInInspector] public bool freeze = false;
    public void AddInternalVelocity(Vector3 speed)
    {
        intervalVelocity += speed;
    }

    public void AddExternalSpeed(Vector3 speed, string source)
    {
        externalVelocities[source] = speed;
    }

    public void EditExternalSpeed(string source, Vector3 newSpeed)
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
    public Vector3 GetExternalSpeed(string source)
    {
        if (externalVelocities.ContainsKey(source))
        {
            return externalVelocities[source];
        }
        return MISSING_VELOCITY_VALUE;
    }

    public Dictionary<string, Vector3> GetAllExternalSpeed()
    {
        return externalVelocities;
    }

    public void RemoveExternalSpeedSource(string source)
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

    public void OverwriteExternalSpeed(string source, Vector3 newSpeed)
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

    private void FixedUpdate()
    {
        if (GameManager.inSpecialStop || freeze || GameManager.gamePaused)
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

    public void ResetComponent()
    {
        intervalVelocity = Vector3.zero;
        externalVelocities.Clear();
    }


}
