using System.Collections.Generic;
using UnityEngine;

public class SpeakerBaseSkill : BaseSkill
{
    [HideInInspector] public BaseSpeaker speaker;
    public override void InitState(BaseCharacter cha, CharacterStateMachine s_machine)
    {
        base.InitState(cha, s_machine);
        speaker = cha.GetComponent<BaseSpeaker>();
    }
    public override bool OnCharacterHit(DamageInfo info)
    {
        Dictionary<string, object> msg = new()
        {
            ["Data"] = info,
            ["CounterHit"] = true
        };
        fsm.TransitionTo<GetHitState>(msg);
        return true;
    }
}

