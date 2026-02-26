using FishNet.Object;
using UnityEngine;

public class PlayerSync : NetworkBehaviour
{
    [ObserversRpc]
    public void UpdateMatchInfoOnClients(PlayerLoadout loadout)
    {
        MatchData.instance.onlineMatch = true;

        MatchData.instance.gameTeams[0].teamMembers.Clear();
        MatchData.instance.gameTeams[0].teamMembers.Add(new MatchData.PlayerInfo()
        {
            skillOne = loadout.skillOne,
            skillTwo = loadout.skillTwo,
            isNetworkPlayer = true,
        });

        MatchData.instance.gameTeams[0].teamMembers[0].isNetworkPlayer = true;

        Debug.Log("Client received match info from host");
    }
}
