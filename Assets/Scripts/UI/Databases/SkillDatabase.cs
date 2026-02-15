using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Scriptable Objects/Databases/SkillDatabase")]
public class SkillDatabase : ScriptableObject
{
    public List<SkillPrefabs> skillPrefabs = new();

    public Dictionary<SkillName, SkillPrefabs> prefabDictionary = new();
}
public enum SkillName
{
    Advance,
    Rebuttal,
    Precedent,
    Anchor,
    Pivot,
    Takeback,
    Objection,
    Provoke,
    Recall,
    Polarity,
    None,
}

public enum SkillType
{
    Declare,
    Sustain,
    Hybrid
}

public enum SkillTags
{
    //Stats
    Mobility,
    SpaceControl, 
    Tempo,
    Offense,
    Defense,
    AllRounder,
    //

    //Playstyle
    Unorthodox,
    Trickster,
    Fundamentals,
    
    //

    //Difficulty
    Difficult,
    Easy,
    Simplistic,
    Complex,
    //

    //Misc
    Tackle,
    EchoManipulation,
    SpeakerManipulation,
    //

    //Skill Type
    Declare, 
    Sustain,
    //

    Cheap,
    Expensive
    //
}
[System.Serializable]
public class SkillPrefabs
{
    public SkillName skillName;
    public SpeakerBaseSkill skillPrefab;
    public string skillSubtitle;
    public Texture skillIcon;
    [TextArea(3, 10)]
    public string skillDescription;
    [Range(1, 5)]
    public int skillFloor = 1;
    [Range(1,5)]
    public int skillCeiling = 2;
    public ArchetypeSpread archetypeStats;
    public SkillType type;
    public List<SkillTags> skillTags;
}
[System.Serializable]
public struct ArchetypeSpread
{
    [Range(1, 10)]
    public int mobilityScore;
    [Range(1, 10)]
    public int controlScore;
    [Range(1, 10)]
    public int tempoScore;
    [Range(1, 10)]
    public int offenseScore;
    [Range(1, 10)]
    public int defenseScore;
}

