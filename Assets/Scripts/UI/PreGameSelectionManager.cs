using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Windows;

public class PreGameSelectionManager : MonoBehaviour
{

    [System.Serializable]
    public class MapThumbnails
    {
        public MapRegistry map;
        public Sprite thumbnail;
    }

    public Dictionary<UISelector, MatchData.PlayerInfo> playerInfo = new();

    [HideInInspector] public SelectionScreen selectionScreen = SelectionScreen.TeamSelect;
    [HideInInspector] public MapRegistry selectedMap = MapRegistry.The_Forum;

    [Header("Team Select Data")]
    [SerializeField] float verticalSpacing = -200;
    [SerializeField] float horizontalSpacing = 400;

    [Header("MenuScreens")]

    [SerializeField] GameObject teamSelectScreen;
    [SerializeField] GameObject skillSelectScreen;
    [SerializeField] GameObject mapSelectScreen;



    [SerializeField] GameObject mapButtonHolder;

    [SerializeField] PlayerInputManager inputManager;

    [SerializeField] TMP_Text mapDisplay;

    [Header("Map Thumbnails")]
    [SerializeField] List<MapThumbnails> mapThumbnails = new();
    [SerializeField] Image thumbnailDisplay;

    bool hasExtraKeyboardPlayer = false;


    Dictionary<MapRegistry, Sprite> mapThumbnailDict = new();
    MatchData matchData;

    private void Start()
    {
        matchData = FindFirstObjectByType<MatchDataHolder>().GetMatchData();
        if (inputManager == null )
        {
            inputManager = GetComponent<PlayerInputManager>();
        }
        verticalSpacing = Mathf.Abs(verticalSpacing) * -1;
        InitSelectionManager();

        InitMatchData();

      
    }


    void InitSelectionManager()
    {
        skillSelectScreen.SetActive(false);
        mapSelectScreen.SetActive(false);
        teamSelectScreen.SetActive(true);


        selectedMap = MapRegistry.The_Forum;


        int index = 0;
        foreach (Transform t in mapButtonHolder.transform)
        {
            if (!t.TryGetComponent(out Button button)) { continue; }
            MapRegistry currentMap = ((MapRegistry)(index));

            string formattedName = currentMap.ToString().Replace("_", " ");
            button.GetComponentInChildren<TMP_Text>().text = formattedName;

            button.onClick.AddListener(() => SetSelectedMap(currentMap));
            index++;
        }

        foreach (var thumbnail in mapThumbnails)
        {
            mapThumbnailDict[thumbnail.map] = thumbnail.thumbnail;
        }
        SetSelectedMap(MapRegistry.The_Forum);
    }

    public void SetSelectedMap(MapRegistry newMap)
    {
        selectedMap = newMap;
        mapDisplay.text = selectedMap.ToString().Replace("_", " ");
        thumbnailDisplay.sprite = mapThumbnailDict[newMap];
    }

    public void StartGame()
    {
      string formattedString = selectedMap.ToString().Replace("_", "");
      SceneManager.LoadScene(formattedString);
    }
    void InitMatchData()
    {
        matchData.gameTeams.Clear();
        for (int i = 0; i < matchData.numberOfTeams; i++)
        {
            matchData.gameTeams.Add(new MatchData.TeamInfo());
        }
    }
    public void OnPlayerJoined(PlayerInput newPlayer)
    {
        if (!newPlayer.gameObject.TryGetComponent(out UISelector selector)) return;

        selector.Init(this, playerInfo.Count + 1);

        StartCoroutine(InitSelector(selector, newPlayer));
    }
    public void AddNewKeyboardPlayer()
    {
        if (hasExtraKeyboardPlayer) return;
        hasExtraKeyboardPlayer = true;

        var manager = GetComponent<PlayerInputManager>();

         PlayerInput input = manager.JoinPlayer(pairWithDevice: Keyboard.current);
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
            Debug.Log("Setting player " + pInput.playerIndex + " to keyboard two control scheme");
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
        Debug.Log("New index is " + selector.teamIndex + ", new dir is " + dir);

    }

    public void OnSkillPressed(UISelector selector, int index)
    {
        if (selector.locked) { return; }
        MatchData.SkillName previousSkill;
        MatchData.SkillName nextSkill;
        int totalSkills = Enum.GetValues(typeof(MatchData.SkillName)).Length;
        if (index == 1)
        {
            previousSkill = playerInfo[selector].skillOne;
            nextSkill = (MatchData.SkillName)(((int)previousSkill + 1) % totalSkills);

            while (nextSkill == playerInfo[selector].skillTwo || nextSkill == MatchData.SkillName.None)
            {
                nextSkill = (MatchData.SkillName)(((int)nextSkill + 1) % totalSkills);
            }

            playerInfo[selector].skillOne = nextSkill;
        }
        else if (index == 2)
        {
            previousSkill = playerInfo[selector].skillTwo;
            nextSkill = (MatchData.SkillName)(((int)previousSkill + 1) % totalSkills);

            while (nextSkill == playerInfo[selector].skillOne || nextSkill == MatchData.SkillName.None)
            {
                nextSkill = (MatchData.SkillName)(((int)nextSkill + 1) % totalSkills);
            }

            playerInfo[selector].skillTwo = nextSkill;
        }

        selector.skillOneDisplay.text = playerInfo[selector].skillOne.ToString();
        selector.skillTwoDisplay.text = playerInfo[selector].skillTwo.ToString();

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
        if (locked != null) SetPlayerData(locked);
        if (playerInfo.Keys.Count < 2)
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
                foreach (var selector in playerInfo.Keys) { selector.ToggleSkillDisplay(true); }
               
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
            case SelectionScreen.TeamSelect:
                ReturnToMainMenu();
                break;
            case SelectionScreen.SkillSelect:

                inputManager.EnableJoining();
                skillSelectScreen.SetActive(false);
                teamSelectScreen.SetActive(true);
                foreach (var selector in playerInfo.Keys) { selector.ToggleSkillDisplay(false); }
                StartCoroutine(ResetSelectors(SelectionScreen.TeamSelect));
                break;
            case SelectionScreen.MapSelect:
                mapSelectScreen.SetActive(false);
                skillSelectScreen.SetActive(true);
                foreach (var selector in playerInfo.Keys)
                {
                    selector.Show();
                    selector.ToggleSkillDisplay(true);

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

    void SetPlayerData(UISelector selector)
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
                Debug.Log("Number of teams: " + matchData.gameTeams.Count + " Team index: " + selector.teamIndex);

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
    MapSelect
}
