using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class BaseSkill : BaseState
{
    public UnityEvent<BaseCharacter, int> skillUsed = new(); //int is skill index
    [SerializeField] public SkillName skillName = SkillName.Advance;
    public int staminaCost = 15;

    protected StaminaComponent staminaComponent;
    protected InputAction skillAction;
    protected int oppositeSkillIndex;

    protected BufferHelper oppositeSkillBuffer;
    protected BufferHelper skillBuffer;

    int skillIndex;

    private void Awake()
    {
        character = GetComponentInParent<BaseSpeaker>();
    }

    public override void InitState(BaseCharacter cha, CharacterStateMachine s_machine)
    {
        base.InitState(cha, s_machine);
        staminaComponent = character.staminaComponent;
        InitSkill();
    }

    public override void Enter(Dictionary<string, object> msg = null)
    {
        base.Enter(msg);
        skillUsed.Invoke(character, skillIndex);
    }
    public void SetSkillIndex(int index)
    {
        skillIndex = index;
    }
    public void InitSkill()
    {
        switch (skillIndex)
        {
            case 1:
                skillAction = character.inputManager.GetAction("SkillOne");
                oppositeSkillIndex = 2;
                oppositeSkillBuffer = fsm.TryGetBuffer("SkillTwoBuffer");
                skillBuffer = fsm.TryGetBuffer("SkillOneBuffer");
                break;
            case 2:
                skillAction = character.inputManager.GetAction("SkillTwo");
                oppositeSkillIndex = 1;
                oppositeSkillBuffer = fsm.TryGetBuffer("SkillOneBuffer");
                skillBuffer = fsm.TryGetBuffer("SkillTwoBuffer");
                break;
            case 3:
                skillAction = character.inputManager.GetAction("SkillThree");
                break;
            default:
                skillAction = character.inputManager.GetAction("SkillFour");
                break;
        }
    }

    protected virtual void OnSkillUsed()
    {
        if (!staminaComponent.HasForesight())
        {
            staminaComponent.DamageStamina(staminaCost, 0, false);
        }
        else
        {
            staminaComponent.ConsumeForesight();
        }
    }

    protected virtual void OnSkillOver()
    {
        if (IsGrounded())
        {
            if (GetMovementDir().magnitude < MOVE_DEADZONE)
            {
                fsm.TransitionTo<IdleState>();
            }
            else
            {
                fsm.TransitionTo<RunState>();
            }
        }
        else fsm.TransitionTo<FallState>();
    }


    public virtual bool SkillAvailable()
    {
        return staminaComponent.GetStamina() > staminaCost || staminaComponent.HasForesight();
    }

    public virtual void ResetSkill()
    {

    }
    
    protected bool CancelSkillIfOppositeSkillBuffered()
    {
        if (oppositeSkillBuffer != null)
        {
            if (oppositeSkillBuffer.Buffered)
            {
                oppositeSkillBuffer.Consume();
                fsm.TransitionToSkill(oppositeSkillIndex);
                return true;
            }
        }
        return false;
    }
}
