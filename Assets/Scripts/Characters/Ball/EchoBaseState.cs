using UnityEngine;

public class EchoBaseState : BaseState
{
    [HideInInspector] public BaseEcho echo;
    public bool lightenAllowed = false;
    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm)
    {
        base.InitState(cha, fsm);
        echo = cha.GetComponent<BaseEcho>();
    }

    public virtual void OnBallIgnited()
    {

    }
}
