using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageDatabase", menuName = "Scriptable Objects/Databases/StageDatabase")]
public class StageDatabase : ScriptableObject
{
    //[System.Serializable]
    //public class MapThumbnails
    //{
    //    public MapRegistry map;
    //    public Sprite thumbnail;
    //}

   public List<MapDatabaseEntry> entries = new();

    [System.Serializable]
    public class MapDatabaseEntry 
    {
        public Texture mapThumbnail;
        public MapName mapName;
        public string mapDescription;
    }

   public Dictionary<MapName, MapDatabaseEntry> mapDatabase = new();
}
