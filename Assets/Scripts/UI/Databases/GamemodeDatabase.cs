using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GamemodeDatabase", menuName = "Scriptable Objects/Databases/GamemodeDatabase")]
public class GamemodeDatabase : ScriptableObject
{
    [System.Serializable]
    public enum GameModeName
    {
        SpeakerDuel, // 1v1 no echo players
        // RIP 2v2 unlucky
        ScoreRace, // Get as many points as possible completing objectives
        SkillDraft, //Speaker duel, but with draft system
        EchoSurvival, // Survive against waves of echoes
        Training

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

    public List<GameModeInfo> availableGameModes = new();

}
