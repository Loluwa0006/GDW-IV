using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreGameSelectionManager : MonoBehaviour
{

    [System.Serializable]
    public class MapThumbnails
    {
        public MapName map;
        public Sprite thumbnail;
    }

    public Dictionary<UISelector, MatchData.PlayerInfo> playerInfo = new();

    [HideInInspector] public SelectionScreen selectionScreen = SelectionScreen.TeamSelect;
    [HideInInspector] public MapName selectedMap = MapName.The_Forum;

    [Header("Team Select Data")]
    [SerializeField] float verticalSpacing = -200;
    [SerializeField] float horizontalSpacing = 400;

    [Header("MenuScreens")]

    [SerializeField] GameObject modeSelectScreen;
    [SerializeField] GameObject teamSelectScreen;
    [SerializeField] GameObject skillSelectScreen;
    [SerializeField] GameObject mapSelectScreen;

    [SerializeField] GameObject mapButtonHolder;

    [SerializeField] PlayerInputManager inputManager;

    [SerializeField] TMP_Text mapDisplay;

    [Header("Map Thumbnails")]
    [SerializeField] List<MapThumbnails> mapThumbnails = new();
    [SerializeField] Image thumbnailDisplay;

    [Header("Skill Displays")]
    [SerializeField] RawImage p1SkillOneDisplay;
    [SerializeField] RawImage p1SkillTwoDisplay;
    [SerializeField] RawImage p2SkillOneDisplay;
    [SerializeField] RawImage p2SkillTwoDisplay;
    [SerializeField] TMP_Text p1SkillOneDescription;
    [SerializeField] TMP_Text p1SkillTwoDescription;
    [SerializeField] TMP_Text p2SkillOneDescription;
    [SerializeField] TMP_Text p2SkillTwoDescription;
    [SerializeField] TMP_Text p1SkillOneTitle;
    [SerializeField] TMP_Text p1SkillTwoTitle;
    [SerializeField] TMP_Text p2SkillOneTitle;
    [SerializeField] TMP_Text p2SkillTwoTitle;

    [Header("Mode Selection")]
    [SerializeField] TMP_Text modeDescription;

    bool hasExtraKeyboardPlayer = false;

    Dictionary<MapName, Sprite> mapThumbnailDict = new();
    MatchData matchData;

    private void Start()
    {
        matchData = MatchData.instance;
        if (inputManager == null )
        {
            inputManager = GetComponent<PlayerInputManager>();
        }
        verticalSpacing = Mathf.Abs(verticalSpacing) * -1;
        InitSelectionManager();
    }


    void InitSelectionManager()
    {
        skillSelectScreen.SetActive(false);
        mapSelectScreen.SetActive(false);
        teamSelectScreen.SetActive(false);
        modeSelectScreen.SetActive(true);

        selectedMap = MapName.The_Forum;

        int index = 0;
        foreach (Transform t in mapButtonHolder.transform)
        {
            if (!t.TryGetComponent(out Button button)) { continue; }
            MapName currentMap = (MapName) index;

            string formattedName = currentMap.ToString().Replace("_", " ");
            button.GetComponentInChildren<TMP_Text>().text = formattedName;

            button.onClick.AddListener(() => SetSelectedMap(currentMap));
            index++;  
        }

        foreach (var thumbnail in mapThumbnails)
        {
            mapThumbnailDict[thumbnail.map] = thumbnail.thumbnail;
        }
        SetSelectedMap(MapName.The_Forum);
        SetSelectedGameMode("SpeakerDuel");
        modeDescription.text = matchData.selectedGameMode.modeDescription;

        inputManager.DisableJoining();
    }
    public void SetSelectedGameMode(string modeString)
    {
        GamemodeDatabase.GameModeName gameModeName = (GamemodeDatabase.GameModeName) Enum.Parse(typeof(GamemodeDatabase.GameModeName), modeString);
        matchData.selectedGameMode = matchData.gameModeDictionary[gameModeName];
        modeDescription.text = matchData.selectedGameMode.modeDescription;
        InitMatchData();
    }
    public void ContinueToGameSelect()
    {
        inputManager.EnableJoining();
        modeSelectScreen.SetActive(false);
        teamSelectScreen.SetActive(true);
        selectionScreen = SelectionScreen.TeamSelect;
    }
    public void SetSelectedMap(MapName newMap)
    {
        selectedMap = newMap;
        mapDisplay.text = selectedMap.ToString().Replace("_", " ");
        thumbnailDisplay.sprite = mapThumbnailDict[newMap];
    }
    public void StartGame()
    {
        MatchData.instance.onlineMatch = false;
      string formattedString = selectedMap.ToString().Replace("_", "");
      SceneManager.LoadScene(formattedString);
    }
    void InitMatchData()
    {
        matchData.gameTeams.Clear();
        for (int i = 0; i < matchData.selectedGameMode.maximumTeams; i++)
        {
            matchData.gameTeams.Add(new MatchData.TeamInfo());
        }
    }
    public void OnPlayerJoined(PlayerInput newPlayer)
    {
        if (!newPlayer.gameObject.TryGetComponent(out UISelector selector)) return;

        selector.Init(this, playerInfo.Count + 1);

        StartCoroutine(InitSelector(selector, newPlayer));
        if (inputManager.playerCount == matchData.selectedGameMode.maximumTeams * (matchData.selectedGameMode.numberOfSpeakers + matchData.selectedGameMode.numberOfEchoes))
        {
            inputManager.DisableJoining();
        }
    }
    public void AddNewKeyboardPlayer()
    {
        if (hasExtraKeyboardPlayer || playerInfo.Count == 0 || playerInfo.Count + 1 > matchData.selectedGameMode.maximumTeams) return;
        hasExtraKeyboardPlayer = true;

        var manager = GetComponent<PlayerInputManager>();
        manager.JoinPlayer(pairWithDevice: Keyboard.current);
    }
    public void TogglePlayerAI(bool toggle)
    {
        foreach (var player in playerInfo.Reverse().ToList())
        {
            if (player.Value.isAI == toggle) continue;
            player.Value.isAI = !player.Value.isAI ;
            player.Key.aiDisplay.SetActive(player.Value.isAI);
            break;
        }
    }
    IEnumerator InitSelector(UISelector selector,PlayerInput pInput)
    {
        selector.transform.SetParent(transform, false);
        yield return null;
        SetNewTeamPos(selector, 0.0f);
        selector.teamIndex = 0;
        Vector3 spawnPos = Vector3.zero;
        spawnPos.y = verticalSpacing * playerInfo.Count;
        selector.rectTransform.anchoredPosition = spawnPos;
        if (!playerInfo.ContainsKey(selector))
        {
            playerInfo.Add(selector, new MatchData.PlayerInfo());
        }
        selector.selectorLocked.AddListener(ContinueToNextScreen);
        bool keyboardTwo = false;
        foreach (var keys in playerInfo.Keys)
        {
            if (playerInfo[keys].device == Keyboard.current && pInput.devices[0] == Keyboard.current)
            {
                keyboardTwo = true;
            }
        }
        selector.gameObject.name = "Player" + pInput.playerIndex + "Selector";

        if (keyboardTwo)
        {
            //Debug.Log("Setting player " + pInput.playerIndex + " to keyboard two control scheme");
            playerInfo[selector].controlScheme = "CombatKeyboardTwo";
            pInput.SwitchCurrentActionMap("UIKeyboardTwo");
            playerInfo[selector].device = Keyboard.current;
        }
        else
        {
            playerInfo[selector].device = pInput.devices[0];
            pInput.SwitchCurrentActionMap("UI");
        }
    }
    public void OnSelectionMoved(UISelector selector, int dir)
    {
        if (selector.teamIndex == 0)
        {
            if (dir > 0)
            {
                SetNewTeamPos(selector, horizontalSpacing);
                selector.teamIndex = 2;
            }
            else if (dir < 0)
            {
                SetNewTeamPos(selector, -horizontalSpacing);
                selector.teamIndex = 1;
            }
        }
        else
        {
            if (dir > 0 && selector.teamIndex == 1 || dir < 0 && selector.teamIndex == 2)
            {
                SetNewTeamPos(selector, 0.0f);
                selector.teamIndex = 0;
            }
        }
      //  Debug.Log("New index is " + selector.teamIndex + ", new dir is " + dir);
    }

    public void OnSkillPressed(UISelector selector, int index)
    {
        if (selector.locked) { return; }
        SkillName previousSkill;
        SkillName nextSkill;
        int totalSkills = Enum.GetValues(typeof(SkillName)).Length;
        if (index == 1)
        {
            previousSkill = playerInfo[selector].skillOne;
            nextSkill = (SkillName)(((int)previousSkill + 1) % totalSkills);

            while (nextSkill == playerInfo[selector].skillTwo || nextSkill == SkillName.None)
            {
                nextSkill = (SkillName)(((int)nextSkill + 1) % totalSkills);
            }

            playerInfo[selector].skillOne = nextSkill;
        }
        else if (index == 2)
        {
            previousSkill = playerInfo[selector].skillTwo;
            nextSkill = (SkillName)(((int)previousSkill + 1) % totalSkills);

            while (nextSkill == playerInfo[selector].skillOne || nextSkill == SkillName.None)
            {
                nextSkill = (SkillName)(((int)nextSkill + 1) % totalSkills);
            }

            playerInfo[selector].skillTwo = nextSkill;
        }

        selector.skillOneDisplay.text = playerInfo[selector].skillOne.ToString();
        selector.skillTwoDisplay.text = playerInfo[selector].skillTwo.ToString();

        UpdateSkillDisplays();
    }

    void UpdateSkillDisplays ()
    {
        var infoKeys = playerInfo.Values.ToArray();

        var p1SkillOneName = infoKeys[0].skillOne;
        var p1SkillTwoName = infoKeys[0].skillTwo;

        p1SkillOneDisplay.texture = matchData.skillIconDictionary[p1SkillOneName];
        p1SkillTwoDisplay.texture = matchData.skillIconDictionary[p1SkillTwoName];

        p1SkillOneDescription.text = matchData.skillDatabase.prefabDictionary[p1SkillOneName].skillDescription;
        p1SkillTwoDescription.text = matchData.skillDatabase.prefabDictionary[p1SkillTwoName].skillDescription;

        p1SkillOneTitle.text = p1SkillOneName.ToString();
        p1SkillTwoTitle.text = p1SkillTwoName.ToString();

        if (infoKeys.Count() < 2) return;

        var p2SkillOneName = infoKeys[1].skillOne;
        var p2SkillTwoName = infoKeys[1].skillTwo;

        p2SkillOneDisplay.texture = matchData.skillIconDictionary[p2SkillOneName];
        p2SkillTwoDisplay.texture = matchData.skillIconDictionary[p2SkillTwoName];


        p2SkillOneDescription.text = matchData.skillDatabase.prefabDictionary[p2SkillOneName].skillDescription;
        p2SkillTwoDescription.text = matchData.skillDatabase.prefabDictionary[p2SkillTwoName].skillDescription;


        p2SkillOneTitle.text = p2SkillOneName.ToString();
        p2SkillTwoTitle.text = p2SkillTwoName.ToString();
    }
    public void SwapScheme(UISelector selector)
    {
        var info = playerInfo[selector];
        if (info.controlScheme.Contains("KeyboardTwo")) return; // no alt scheme for player two on kb, you deserve nothing, get a gamepad

        if (info.controlScheme == "Combat")
        {
            info.controlScheme = "CombatAlternate";
            selector.alternateControlSchemeDisplay.SetActive(true);
        }
        else
        {
            info.controlScheme = "Combat";
            selector.alternateControlSchemeDisplay.SetActive(false);

        }
    }

    public void SetNewTeamPos(UISelector selector, float xPos)
    {
        Vector3 newPos = selector.rectTransform.anchoredPosition;
        newPos.x = xPos;
        selector.rectTransform.anchoredPosition = newPos;
        
    }

    public void ContinueToNextScreen(UISelector locked)
    {
        if (locked != null) SetPlayerTeam(locked);
        if (playerInfo.Keys.Count < matchData.selectedGameMode.minimumTeams)
        {
            return;
        }
        foreach (var selector in playerInfo.Keys)
        {
            if (!selector.locked) return;
        }
        
        switch (selectionScreen)
        {
            case SelectionScreen.TeamSelect:
                inputManager.DisableJoining();
                foreach (var team in matchData.gameTeams)
                {
                    if (team.teamMembers.Count == 0)
                    {
                        return;
                    }
                }
                skillSelectScreen.SetActive(true);
                teamSelectScreen.SetActive(false);
                foreach (var selector in playerInfo.Keys)
                { 
                    selector.ToggleExternalDisplays(true, playerInfo[selector].isAI); 
                }
                
               UpdateSkillDisplays();
                StartCoroutine(ResetSelectors(SelectionScreen.SkillSelect));
                break;
            case SelectionScreen.SkillSelect:
                inputManager.DisableJoining();
                foreach (var selector in playerInfo.Keys)
                {
                    selector.Hide(); 
                }
                skillSelectScreen.SetActive(false);
                mapSelectScreen.SetActive(true);
                StartCoroutine(ResetSelectors(SelectionScreen.MapSelect, true));
                break;

        }
       
    }

    public void ReturnToPreviousScreen()
    {
        switch (selectionScreen)
        {
            case SelectionScreen.ModeSelect: // returns to main menu
                ReturnToMainMenu();
                break;
            case SelectionScreen.TeamSelect: // goes to mode select
                modeSelectScreen.SetActive(true);
                teamSelectScreen.SetActive(false);
                foreach(var selector in playerInfo.Keys.ToList())
                {
                    Destroy(selector.gameObject);
                }
                playerInfo.Clear();
                inputManager.DisableJoining();
                selectionScreen = SelectionScreen.ModeSelect;
                break;
            case SelectionScreen.SkillSelect: // goes to team select

                inputManager.EnableJoining();
                skillSelectScreen.SetActive(false);
                teamSelectScreen.SetActive(true);
                foreach (var selector in playerInfo.Keys)
                {
                    selector.ToggleExternalDisplays(false, playerInfo[selector].isAI);

                }
                StartCoroutine(ResetSelectors(SelectionScreen.TeamSelect));
                break;
            case SelectionScreen.MapSelect: //goes to skill select
                mapSelectScreen.SetActive(false);
                skillSelectScreen.SetActive(true);
                foreach (var selector in playerInfo.Keys)
                {
                    selector.Show();
                    selector.ToggleExternalDisplays(true, playerInfo[selector].isAI);
                }
                StartCoroutine(ResetSelectors(SelectionScreen.SkillSelect));
                break;   
        }
    }

    IEnumerator ResetSelectors(SelectionScreen newScreen, bool hideAfter = false)
    {
        yield return null;
        foreach (var selector in playerInfo.Keys)
        {
            selector.ResetSelection();
            if (hideAfter)
            {
                selector.Hide();
            }
        }
      
        selectionScreen = newScreen;
    }

    void SetPlayerTeam(UISelector selector)
    {
        if (selector.teamIndex == 0) { return; }
        switch (selectionScreen)
        {
            case SelectionScreen.TeamSelect:
                foreach (var teams in matchData.gameTeams)
                {
                    if (teams.teamMembers.Contains(playerInfo[selector]))
                    {
                        teams.teamMembers.Remove(playerInfo[selector]);
                    }
                }
                //Debug.Log("Number of teams: " + matchData.gameTeams.Count + " Team index: " + selector.teamIndex);

                matchData.gameTeams[selector.teamIndex - 1].teamMembers.Add(playerInfo[selector]);
                break;
        }
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(SceneRegistry.MainMenu.ToString());
    }
}
public enum SelectionScreen
{
    TeamSelect,
    RoleSelect,
    SkillSelect,
    MapSelect,
    ModeSelect,
}
