using UnityEngine;

public struct PlayerLoadout
{
    public SkillName skillOne;
    public SkillName skillTwo;
    public MapName preferredMap;

    public PlayerLoadout(SkillName one, SkillName two, MapName map)
    {
        preferredMap = MapName.The_Forum;
        skillOne = one;
        skillTwo = two;
    }
}

public class LoadoutHelper
{
    public static PlayerLoadout currentLoadout;
}