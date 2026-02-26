using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RoomManager : MonoBehaviour
{
    NetworkManager networkManager;

    [SerializeField] PlayerSync syncManager;
    [SerializeField] TMP_InputField roomNameField;
    [SerializeField] Color activeRoomColor;
    [SerializeField] Color inactiveRoomColor;
    [SerializeField] Button hostButton;

    [SerializeField] GameObject roomJoinedNotification;
    bool connectionActive = false;

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();
        ChangeHostButtonColor(inactiveRoomColor);
        networkManager.ServerManager.OnRemoteConnectionState += OnServerConnectionStatusChanged;
        networkManager.SceneManager.OnLoadEnd += OnGameLoaded;
        roomJoinedNotification.SetActive(false);
    }
    private void OnDestroy()
    {
        networkManager.ServerManager.OnRemoteConnectionState -= OnServerConnectionStatusChanged;
    }
    public void OnHostPressed()
    {
        if (!connectionActive)
        {
            networkManager.ServerManager.StartConnection();
            ChangeHostButtonColor(activeRoomColor);
        }
        else
        {
            networkManager.ServerManager.StopConnection(false);
            ChangeHostButtonColor(inactiveRoomColor);
        }
        connectionActive = !connectionActive;
    }

    public void OnJoinPressed()
    {
        networkManager.ClientManager.StartConnection();
    }

    public void OnAddressChanged(string address)
    {
        networkManager.TransportManager.Transport.SetClientAddress(address);
    }
    void ChangeHostButtonColor(Color color)
    {
        var hostColors = hostButton.colors;
        hostColors.normalColor = color;
        hostButton.colors = hostColors;
    }

    private void OnServerConnectionStatusChanged(NetworkConnection networkConnection, RemoteConnectionStateArgs connectionArgs)
    {
        if (connectionArgs.ConnectionState == RemoteConnectionState.Started)
        {
            Debug.Log("Connected to server");
            StartSpeakerDuel();
            roomJoinedNotification.SetActive(true);
        }
        else if (connectionArgs.ConnectionState == RemoteConnectionState.Stopped)
        {
            Debug.Log("Disconnected from server");
            roomJoinedNotification.SetActive(false);
        }
    }

    void StartSpeakerDuel()
    {
        MatchData.instance.selectedGameMode = MatchData.instance.gameModeDictionary[GamemodeDatabase.GameModeName.SpeakerDuel];
        SetUpMatchData();
        LoadStageScene();
    }

    void OnGameLoaded(SceneLoadEndEventArgs args)
    {
        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("OnlineMenu");
    }


    void LoadStageScene()
    {
        string mapName = LoadoutHelper.currentLoadout.preferredMap.ToString();
        mapName = mapName.Replace("_", "");
        SceneLoadData scene = new(mapName)
        {
            ReplaceScenes = ReplaceOption.None,

        };

        SceneUnloadData sceneUnloadData = new("OnlineMenu");
        Debug.Log("Scene name is " + mapName);
        networkManager.SceneManager.LoadGlobalScenes(scene);
        networkManager.SceneManager.UnloadGlobalScenes(sceneUnloadData);
    }

    void SetUpMatchData()
    {
        if (MatchData.instance == null)
        {
            Debug.LogWarning("No match data found, cannot initialize speakers");
            return;
        }
        MatchData.PlayerInfo hostSpeakerInfo = new()
        {
            teamIndex = 1,
            skillOne = LoadoutHelper.currentLoadout.skillOne,
            skillTwo = LoadoutHelper.currentLoadout.skillTwo,
            device = Gamepad.all.Count > 0 ? Gamepad.all[0] : Keyboard.current,
            isAI = false,
            playerType = MatchData.PlayerType.Speaker,

        };
        MatchData.PlayerInfo clientSpeakerInfo = new()
        {
            teamIndex = 1,
            skillOne = LoadoutHelper.currentLoadout.skillOne,
            skillTwo = LoadoutHelper.currentLoadout.skillTwo,
            device = Gamepad.all.Count > 0 ? Gamepad.all[0] : Keyboard.current,
            isAI = false,
            playerType = MatchData.PlayerType.Speaker,
            isNetworkPlayer = true

        };
        MatchData.instance.onlineMatch = true;

        MatchData.instance.gameTeams.Clear();
        MatchData.TeamInfo teamA = new()
        {
            teamName = "TeamA"
        };
        MatchData.TeamInfo teamB = new()
        {
            teamName = "TeamB"
        };
        MatchData.instance.gameTeams.Add(teamA);
        MatchData.instance.gameTeams.Add(teamB);
        MatchData.instance.gameTeams[0].teamMembers.Add(hostSpeakerInfo);
        MatchData.instance.gameTeams[1].teamMembers.Add(clientSpeakerInfo); //temporary solution since advance/rebuttal is the only playable combination right now

        Debug.Log("Match data init");

        PlayerLoadout clientLoadout = new();
        syncManager.UpdateMatchInfoOnClients(clientLoadout);

    }

}