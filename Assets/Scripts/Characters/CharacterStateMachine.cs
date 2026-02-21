using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static MatchData;
public class CharacterStateMachine : MonoBehaviour, ISimulated, ISimulationSnapshot<FSMSnapshot>, ISnapshotable
{

    public BaseState currentState;
    public UnityEvent<StateTransitionInfo> transitionedStates = new(); //order is previous state, current state;
    public UnityEvent<SkillName, SkillName> updatedSkills = new();

    [SerializeField] BaseCharacter character;
    [SerializeField] GameObject bufferHolder;
    [SerializeField] MatchData matchData;

    List<BaseState> statesWithInactiveProcess = new();
    List<BaseState> statesWithInactivePhysicsProcess = new();
    List<BufferHelper> bufferList = new();
    Dictionary<System.Type, BaseState> stateLookup = new();
    Dictionary<int,  BaseSkill> skillLookup = new();
    BaseState previousState;


    [HideInInspector] public bool initMachine = false;

    [System.Serializable]
    public class StateTransitionInfo
    {
        public BaseState prevState;
        public BaseState currentState;

        public bool usingSkill = false;

        public StateTransitionInfo(BaseState prev, BaseState current, bool usingSkill)
        {
            prevState = prev;
            currentState = current;
            this.usingSkill = usingSkill;
        }
    }

    GameManager gameManager;

    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.Default; set { } }
    public bool UpdateDuringHitstop { get => false;  set {} }

    FSMSnapshot[] FSMSnapshots = new FSMSnapshot[SimulationManager.MAX_ROLLBACK_FRAMES];
    public void CreateSkills(PlayerInfo playerInfo)
    {
        ReportManager manager = FindFirstObjectByType<ReportManager>();
        if (playerInfo.skillOne != SkillName.None)
        {
            BaseSkill skillOne = Instantiate(matchData.skillPrefabDictionary[playerInfo.skillOne], transform).GetComponent<BaseSkill>();
            skillOne.SetSkillIndex(1);
            if (manager != null)
            {
                skillOne.skillUsed.AddListener(manager.OnSkillUsed);
            }
        }
        if (playerInfo.skillTwo != SkillName.None)
        {
            BaseSkill skillTwo = Instantiate(matchData.skillPrefabDictionary[playerInfo.skillTwo], transform).GetComponent<BaseSkill>();
            skillTwo.SetSkillIndex(2);
            if (manager != null)
            {
                skillTwo.skillUsed.AddListener(manager.OnSkillUsed);
            }
        }
        updatedSkills.Invoke(playerInfo.skillOne, playerInfo.skillTwo);
    }

    public void AddNewSkill(int index, SkillName name, GameManager manager)
    {
        if (!matchData.skillPrefabDictionary.ContainsKey(name)) matchData.InitData();
        if (!matchData.skillPrefabDictionary.ContainsKey(name)) return;
        if (skillLookup.ContainsKey(index))
        {
            Debug.LogWarning("Skill at index already exists, replacing it ");
            var skillObject = skillLookup[index].gameObject;
            skillLookup.Remove(index);
            Destroy(skillObject);

        }

        BaseSkill newSkill = Instantiate(matchData.skillPrefabDictionary[name], transform).GetComponent<BaseSkill>();
        newSkill.InitState(character, this, manager);
        newSkill.SetSkillIndex(index);
        newSkill.InitSkill();
        skillLookup[index] = newSkill;
        if (newSkill.hasInactivePhysicsProcess)
        {
            statesWithInactivePhysicsProcess.Add(newSkill);
        }
        if (newSkill.hasInactiveProcess)
        {
            statesWithInactiveProcess.Add(newSkill);
        }
        SkillName skillOne = SkillName.None;
        SkillName skillTwo = SkillName.None;
        if (skillLookup.ContainsKey(1))
        {
            skillOne = skillLookup[1].skillName;
        }
        if (skillLookup.ContainsKey(2))
        {
            skillTwo = skillLookup[2].skillName;
        }
        updatedSkills.Invoke(skillOne, skillTwo);

    }
    public void InitMachine(GameManager manager)
    {

        if (currentState == null)
        {
            Debug.LogError("Initial state not set in editor for character");
            return;
        }
        else if (initMachine)
        {
            Debug.LogWarning("State machine already initialized.");
            return;
        }
        gameManager = manager;
        for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!child.TryGetComponent(out BaseState state)) continue; 
                state.InitState(character, this, manager);
                stateLookup[state.GetType()] = state;

                if (state.hasInactiveProcess) statesWithInactiveProcess.Add(state);
                if (state.hasInactivePhysicsProcess) statesWithInactivePhysicsProcess.Add(state);

                if (state is BaseSkill skill)
                {
                    if (!state.gameObject.activeSelf)  continue;  //for testing purposes, makes it easier to toggle current skills without ui
                    skillLookup.Add(skillLookup.Count + 1, skill);
                }
            }
        foreach (Transform t in bufferHolder.transform)
        {
            if (!t.TryGetComponent<BufferHelper>(out var bufferHelper)) { continue; }
            bufferHelper.InitBuffer(character.inputManager, manager);
            bufferList.Add(bufferHelper);
        }
        initMachine = true;
        currentState.EnterSimulated();
        currentState.EnterVisuals();

        InitSimulated(gameManager.simulationManager);   
    }
    public void UpdateState()
    {
        if (!initMachine || gameManager.pauseManager.GamePaused()) { return; }
        currentState.Process();

        foreach (var state in statesWithInactiveProcess)
        {
            if (state == currentState) { continue; }
            state.InactiveProcess();
        }
    }


    public void FixedUpdateState()
    {
        
    }

    public void TransitionTo<T>(Dictionary<string, object> msg = null) where T : BaseState
    {
        if (!initMachine) return; 
        if (!stateLookup.ContainsKey(typeof(T)))
        {
            Debug.LogError("Could not find state of type " +  typeof(T));
            return;
        }
        BaseState newState = stateLookup[typeof(T)];
        if (newState == currentState)
        {
            return;
        }

        if (currentState != null)
        {
            previousState = currentState;
            currentState.Exit();
        }
        newState.EnterSimulated(msg);
        newState.EnterVisuals(msg);
        currentState = newState;

        transitionedStates.Invoke(new StateTransitionInfo(previousState, currentState, false)); ;
    }
    public void TransitionToSkill(int index, Dictionary<string, object> msg = null) 
    {
        if (!initMachine) { return; }
        if (!skillLookup.ContainsKey(index))
        {
            Debug.LogWarning("Could not find skill " + index);
            return;
        }
       var skill = skillLookup[index];
        if (!skill.SkillAvailable())
        {
            return;
        }
        if (currentState == skill)
        {
            return;
        }
        if (currentState != null)
        {
            previousState = currentState;
            currentState.Exit();
        }
        currentState = skillLookup[index];
        currentState.EnterSimulated(msg);
        currentState.EnterVisuals(msg);

        transitionedStates.Invoke(new StateTransitionInfo(previousState, currentState, true));
    }
    public BaseState TryGetState<T>() where T : BaseState
    {
        if (!stateLookup.ContainsKey(typeof(T)))
        {
            Debug.LogError("Could not find state of type " + typeof (T));
            return null;
        }
        return stateLookup[typeof(T)];
    }

    public BufferHelper TryGetBuffer(string bufferName)
    {
        foreach (Transform t in bufferHolder.transform)
        {
            if (t.name == bufferName)
            {
                return t.GetComponent<BufferHelper>();
            }
        }
        return null;
    }

    public BaseSkill TryGetSkill(int index)
    {
        if (skillLookup.ContainsKey(index))
        {
            return skillLookup[index];
        }
        return null;
    }


    public void ResetComponent()
    {
        foreach (var skill in skillLookup.Values)
        {
            skill.ResetSkill();
        }
        foreach (var buffer in bufferList)
        {
            buffer.Consume();
        }
    }

    public FSMSnapshot CaptureState()
    {
        int index = 0;
        for (int i = 0; i < stateLookup.Count; i++)
        {
            if (stateLookup.ElementAt(i).Value == currentState)
            {
                index = i;
                break;
            }
        }
        return new FSMSnapshot()
        {
            activeStateIndex = index
        };
    }

    public void RestoreState(FSMSnapshot state)
    {
        currentState = stateLookup.ElementAt(state.activeStateIndex).Value;
    }

    public void InitSimulated(SimulationManager simulationManager)
    {
        simulationManager.AddSimulatedObject(this);
    }

    public void SimulateUpdate(int currentTick)
    {
        if (!initMachine) return;
        currentState.PhysicsProcess();

        foreach (var state in statesWithInactivePhysicsProcess)
        {
            if (state == currentState) { continue; }
            state.InactivePhysicsProcess();
        }
    }

    public void CaptureCurrentState(int tick)
    {
        FSMSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES] = CaptureState();
    }

    public void RestorePreviousState(int tick)
    {
        RestoreState(FSMSnapshots[tick % SimulationManager.MAX_ROLLBACK_FRAMES]);
    }
}

public struct FSMSnapshot
{
    public int activeStateIndex;
}