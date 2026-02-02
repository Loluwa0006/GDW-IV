using UnityEngine;

public class SpeakerBaseState : BaseState
{

    [HideInInspector] public BaseSpeaker speaker;

    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm)
    {
        base.InitState(cha, fsm);
        speaker = cha.GetComponent<BaseSpeaker>();
    }

 

}
