using System.Collections.Generic;
using UnityEngine;

public class RunState : SpeakerMoveState
{

    [SerializeField] ParticleSystem runTrails;

    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm)
    {
        base.InitState(cha, fsm);
        if (runTrails != null) runTrails.Stop();
    }

    public override void Enter(Dictionary<string, object> msg = null)
    {
        base.Enter(msg);
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
