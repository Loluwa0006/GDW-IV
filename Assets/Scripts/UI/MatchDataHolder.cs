using UnityEngine;

public class MatchDataHolder : MonoBehaviour
{
    [SerializeField] MatchData matchData;

    private void Awake()
    {
        if (MatchData.instance != null && MatchData.instance != matchData)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
        matchData.InitData();
        MatchData.instance = matchData;
    }
    public MatchData GetMatchData()
    {
        return matchData;
    }
}
