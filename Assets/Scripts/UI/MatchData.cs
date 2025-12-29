using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "MatchData", menuName = "Scriptable Objects/MatchData")]
[System.Serializable]
public class MatchData : ScriptableObject
{

    [SerializeField] SkillDatabase skillDatabase;
    public enum PlayerType
    {
        Speaker,
        Echo
    }

    public enum GameType
    {
        SpeakerDuel, // 1v1 no echo players
        Classic, // 2v2 
        FFA, // multiple speaker players, 1 ball
        OneShotRumble //2v2, permanent danger zone

            //these game modes probably won't make it into the game on release but idk 100%, maybe
    }
    [System.Serializable]
    public class PlayerInfo
    {
        public SkillName skillOne = SkillName.Advance;
        public SkillName skillTwo = SkillName.Rebuttal;
        public PlayerType playerType = PlayerType.Speaker;
        public InputDevice device;
        public string controlScheme = "Combat";
        public int teamIndex = 0;
        public bool isAI = false;

    }

    public class TeamInfo
    {
        public HashSet<PlayerInfo> teamMembers = new();
        public string teamName;
        public int handicapLevel = 0;
    }
   
    [HideInInspector] public int numberOfTeams = 2;

    [HideInInspector] public List<TeamInfo> gameTeams = new();

    [HideInInspector] public int numberOfRounds = 2;


    [HideInInspector] public bool initPrefabs = false;

    public int gameLength = 60;


    public Dictionary<SkillName, SpeakerBaseSkill> skillPrefabDictionary = new();
    public Dictionary<SkillName, Texture> skillIconDictionary = new();

    public static MatchData instance;


    public void InitSkillPrefabs()
    {
        foreach (var kvp in skillDatabase.skillPrefabs)
        {
            skillPrefabDictionary[kvp.skillName] = kvp.skillPrefab;
            skillIconDictionary[kvp.skillName] = kvp.skillIcon;
            skillDatabase.prefabDictionary[kvp.skillName] = kvp;
        }
        initPrefabs = true;
    }

}


