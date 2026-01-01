using System.Collections.Generic;
using UnityEngine;

public class TerrainBounceState : BounceState
{

    [SerializeField] ParticleSystem bounceParticles;
    public override void Enter(Dictionary<string, object> msg = null)
    {
       if (echo.playerControlled)
        {
            echo.staminaComponent.EnableForesight();
        }
        if (bounceParticles != null) bounceParticles.Play();
        base.Enter(msg);
    }
 
}