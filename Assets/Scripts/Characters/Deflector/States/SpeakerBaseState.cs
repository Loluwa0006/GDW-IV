using UnityEngine;

public class SpeakerBaseState : BaseState
{

    [HideInInspector] public BaseSpeaker speaker;

    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        speaker = cha.GetComponent<BaseSpeaker>();
    }

 

}
