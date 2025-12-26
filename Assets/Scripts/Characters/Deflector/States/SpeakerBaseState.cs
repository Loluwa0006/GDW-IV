using UnityEngine;

public class SpeakerBaseState : BaseState
{

    [HideInInspector] public BaseSpeaker speaker;

    public override void InitState(BaseCharacter cha, CharacterStateMachine s_machine)
    {
        base.InitState(cha, s_machine);
        speaker = cha.GetComponent<BaseSpeaker>();
    }

 

}
