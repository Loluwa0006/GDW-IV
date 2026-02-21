using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeflectionBounceState : BounceState
{
    [Header("Deflection Settings")]
    public int deflectStopAmount;

    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {
        BaseSpeaker deflector;
        bool usedSkill = false;
        if (msg == null)
        {
            Debug.LogWarning("Entered deflection state with no message");
            fsm.TransitionTo<FlyingState>();
            return;
        }
        if (msg.ContainsKey("deflector"))
        {
            deflector = (BaseSpeaker)msg["deflector"];
        }
        else
        {
            Debug.LogWarning("Entered deflection state but there's no deflector");
            fsm.TransitionTo<FlyingState>();
            return;
        }
        if (msg.ContainsKey("usedSkill"))
        {
            usedSkill = (bool)msg["usedSkill"];
        }
        base.EnterSimulated(msg);
        echo.FindNewTarget(deflector.transform);
        echo.echoDeflected.Invoke(echo);
        deflector.deflectManager.OnSuccessfulDeflect(echo, usedSkill);
    }
    public override void ApplyBounceVelocity()
    {
        float t = echoData.deflectStreak / (float)echoData.deflectsUntilMaxSpeed;
        echoData.deflectStreak += 1;
        echo.UpdateSpeed(Mathf.Lerp(echoData.minSpeed, echoData.maxSpeed, t));
        gameManager.hitstopManager.ApplySpecialStop(deflectStopAmount);
        base.ApplyBounceVelocity();
    }
}
