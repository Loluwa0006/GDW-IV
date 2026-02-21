using System.Collections.Generic;
using UnityEngine;

public class RunState : SpeakerMoveState
{
    [SerializeField] ParticleSystem runTrails;

    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        if (runTrails != null) runTrails.Stop();
    }

    public override void EnterVisuals(Dictionary<string, object> msg = null)
    {
        base.EnterVisuals(msg);
        if (runTrails != null) runTrails.Play();
    }

    public override void Process()
    {
        base.Process();
        runTrails.transform.rotation = Quaternion.LookRotation(-character.velocityManager.GetInternalSpeed());
    }
    public override void Exit()
    {
        base.Exit();
        if (runTrails != null) runTrails.Stop();
    }
}
