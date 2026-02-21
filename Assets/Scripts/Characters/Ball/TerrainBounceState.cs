using System.Collections.Generic;
using UnityEngine;

public class TerrainBounceState : BounceState
{

    [SerializeField] ParticleSystem bounceParticles;
    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {
       if (echo.playerControlled)
        {
            echo.staminaComponent.EnableForesight();
        }
        base.EnterSimulated(msg);
    }

    public override void EnterVisuals(Dictionary<string, object> msg = null)
    {
        base.EnterVisuals(msg);
        if (bounceParticles != null) bounceParticles.Play();
    }


    public override void ApplyBounceVelocity()
    {
        echo.velocityManager.OverwriteInternalSpeed((echo.GetTarget().transform.position - transform.position).normalized * echo.GetSpeed());
    }
}