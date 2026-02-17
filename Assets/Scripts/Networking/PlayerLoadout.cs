using UnityEngine;

class PlayerLoadout
{
    public SkillName skillOne = SkillName.Advance;
    public SkillName skillTwo = SkillName.Rebuttal;
    public MapName preferredMap = MapName.The_Forum;

    public static PlayerLoadout currentLoadout;
}