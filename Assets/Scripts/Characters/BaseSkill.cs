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
    public InputAction oppositeSkillAction {  get; private set; }
    protected int oppositeSkillIndex;

    protected BufferHelper oppositeSkillBuffer;
    protected BufferHelper skillBuffer;

    int skillIndex;

   protected SimulationManager simulationManager;

    protected SkillSnapshot[] skillSnapshots = new SkillSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];

    public override void InitState(BaseCharacter cha, CharacterStateMachine fsm, GameManager manager)
    {
        base.InitState(cha, fsm, manager);
        staminaComponent = character.staminaComponent;
        simulationManager = manager.simulationManager;
        InitSkill();
    }

    public override void EnterSimulated(Dictionary<string, object> msg = null)
    {
        base.EnterSimulated(msg);
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
                oppositeSkillAction = character.inputManager.GetAction("SkillTwo");
                oppositeSkillIndex = 2;
                oppositeSkillBuffer = fsm.TryGetBuffer("SkillTwoBuffer");
                skillBuffer = fsm.TryGetBuffer("SkillOneBuffer");
                break;
            case 2:
                skillAction = character.inputManager.GetAction("SkillTwo");
                oppositeSkillAction = character.inputManager.GetAction("SkillOne");
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
        if (!staminaComponent.ForesightEnabled)
        {
            staminaComponent.DamageStamina(staminaCost, 0, false);
        }
        else
        {
            staminaComponent.ConsumeForesight();
        }
    }
    public virtual bool SkillAvailable()
    {
        return staminaComponent.Stamina > staminaCost || staminaComponent.ForesightEnabled;
    }
    public virtual void ResetSkill()
    {

    }
    protected virtual void OnSkillOver()
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

    protected SkillSnapshot GetSkillSnapshot()
    {
        return new SkillSnapshot()
        {

        };
    }
}
public struct SkillSnapshot
{

}
