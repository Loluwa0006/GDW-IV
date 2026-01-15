using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeflectionBounceState : BounceState
{
    [Header("Deflection Settings")]
    public int deflectStopAmount;

    public override void Enter(Dictionary<string, object> msg = null)
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
        base.Enter(msg);
        echo.FindNewTarget(deflector.transform);
        echo.echoDeflected.Invoke(echo);
        StartCoroutine(deflector.deflectManager.OnSuccessfulDeflect(echo, usedSkill));
    }
    public override void ApplyBounceVelocity()
    {
        float previousSpeed = echo.GetSpeed();
        Debug.Log("Dividing " + echoData.deflectStreak + " by " + echoData.deflectsUntilMaxSpeed);
        float t = echoData.deflectStreak / (float)echoData.deflectsUntilMaxSpeed;
        echoData.deflectStreak += 1;
        echo.UpdateSpeed(Mathf.Lerp(echoData.minSpeed, echoData.maxSpeed, t));
        Debug.Log("Deflection bounce speed change: " + previousSpeed + " -> " + echo.GetSpeed());
        GameManager.ApplySpecialStop(deflectStopAmount);
    }
}
