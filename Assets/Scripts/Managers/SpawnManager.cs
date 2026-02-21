using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] Transform speakerDuelSpawns;
    [SerializeField] Transform timeRaceSpawns;
    [SerializeField] Transform echoSpawn;


    public List<Vector3> GetSpawnsFromTransform(Transform spawnHolder)
    {
        List<Vector3> spawns = new();
        foreach (Transform spawn in spawnHolder)
        {
            spawns.Add(spawn.position);
        }
        return spawns;
    }

    public List<Vector3> GetSpeakerDuelSpawns()
    {
        return GetSpawnsFromTransform(speakerDuelSpawns);
    }

    public List<Vector3> GetTimeRaceSpawns()
    {
        return GetSpawnsFromTransform(timeRaceSpawns);
    }

    public Vector3 GetAIEchoSpawn()
    {
        return echoSpawn.position;
    }
}