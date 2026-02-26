using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "MatchData", menuName = "Scriptable Objects/MatchData")]
[System.Serializable]
public class MatchData : ScriptableObject
{


    public SkillDatabase skillDatabase;
    public GamemodeDatabase gamemodeDatabase;
    public StageDatabase stageDatabase;
    public enum PlayerType
    {
        Speaker,
        Echo
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
        public bool isNetworkPlayer = false;

    }

    public class TeamInfo
    {
        public List<PlayerInfo> teamMembers = new();
        public string teamName;
        public int handicapLevel = 0;
    }


    [HideInInspector] public List<TeamInfo> gameTeams = new();

    [HideInInspector] public bool initPrefabs = false;

    public int gameLength = 60;

    public bool onlineMatch = false;


    public Dictionary<SkillName, SpeakerBaseSkill> skillPrefabDictionary = new();
    public Dictionary<SkillName, Texture> skillIconDictionary = new();
    public Dictionary<GamemodeDatabase.GameModeName,GamemodeDatabase.GameModeInfo> gameModeDictionary = new();

    public static MatchData instance;

   [HideInInspector] public GamemodeDatabase.GameModeInfo selectedGameMode;
    public void InitData()
    {
        if (skillDatabase.skillPrefabs.Count <= 0)
        {
            Debug.LogError("Missing skill prefabs");
            return;
        }
        foreach (var entry in skillDatabase.skillPrefabs)
        {
            skillPrefabDictionary[entry.skillName] = entry.skillPrefab;
            skillIconDictionary[entry.skillName] = entry.skillIcon;
            skillDatabase.prefabDictionary[entry.skillName] = entry;
        }

        foreach (var entry in gamemodeDatabase.availableGameModes)
        {
            gameModeDictionary[entry.gameType] = entry;
        }

        foreach (var entry in stageDatabase.entries)
        {
            stageDatabase.mapDatabase[entry.mapName] = entry;
        }
        selectedGameMode = gameModeDictionary[GamemodeDatabase.GameModeName.SpeakerDuel];
        initPrefabs = true;
    }

}