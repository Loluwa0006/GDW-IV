using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "MatchData", menuName = "Scriptable Objects/MatchData")]
[System.Serializable]
public class MatchData : ScriptableObject
{

    public SkillDatabase skillDatabase;
    public enum PlayerType
    {
        Speaker,
        Echo
    }

    [System.Serializable]
    public enum GameModeName
    {
        SpeakerDuel, // 1v1 no echo players
        Classic, // 2v2 
        ScoreRace, // Get as many points as possible completing objectives
        SkillDraft, //Speaker duel, but with draft system
        EchoSurvival // Survive against waves of echoes

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

    [System.Serializable]
    public class GameModeInfo
    {
        public GameModeName gameType = GameModeName.SpeakerDuel;
        public BaseGameMode gameModePrefab;
        public string modeDescription;
        public int minimumTeams = 2;
        public int maximumTeams = 2;
        public int numberOfSpeakers = 2;
        public int numberOfEchoes = 0;
        public int numberOfRounds = 1;
    }

    [HideInInspector] public List<TeamInfo> gameTeams = new();

    [HideInInspector] public bool initPrefabs = false;

    public int gameLength = 60;


    public Dictionary<SkillName, SpeakerBaseSkill> skillPrefabDictionary = new();
    public Dictionary<SkillName, Texture> skillIconDictionary = new();
    public Dictionary<GameModeName, GameModeInfo> gameModeDictionary = new();

    public static MatchData instance;

   [HideInInspector] public GameModeInfo selectedGameMode;


    [SerializeField] List<GameModeInfo> availableGameModes = new();


    public void InitData()
    {
        if (skillDatabase.skillPrefabs.Count <= 0)
        {
            Debug.LogError("Missing skill prefabs");
            return;
        }
        foreach (var kvp in skillDatabase.skillPrefabs)
        {
            skillPrefabDictionary[kvp.skillName] = kvp.skillPrefab;
            skillIconDictionary[kvp.skillName] = kvp.skillIcon;
            skillDatabase.prefabDictionary[kvp.skillName] = kvp;
        }

        foreach (var kvp in availableGameModes)
        {
            gameModeDictionary[kvp.gameType] = kvp;
        }
         selectedGameMode = gameModeDictionary[GameModeName.SpeakerDuel];
        initPrefabs = true;
    }

}


