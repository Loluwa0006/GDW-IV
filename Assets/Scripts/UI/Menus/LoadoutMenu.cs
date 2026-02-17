using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadoutMenu : MonoBehaviour
{
    [SerializeField] GameObject skillPicker;
    [SerializeField] GameObject stagePicker;

    [SerializeField] RawImage skillOneIcon;
    [SerializeField] RawImage skillTwoIcon;
    [SerializeField] TMP_Text skillOneName;
    [SerializeField] TMP_Text skillTwoName;

    [SerializeField] RawImage stageIcon;
    [SerializeField] TMP_Text stageName;

    [SerializeField] List<StageButton> stageButtons = new();
    [SerializeField] List<SkillButton> skillButtons = new();

    int skillToChange = 1;

    [System.Serializable]
    class StageButton
    {
        public Button button;
        public MapName mapName;
    }

    [System.Serializable]
    class SkillButton
    {
        public Button button;
        public SkillName skillName;
    }

    private void Awake()
    {
        skillPicker.SetActive(false);
        stagePicker.SetActive(false);
    }


    private void OnEnable()
    {
        foreach (var stage in stageButtons)
        {
            stage.button.onClick.AddListener(() => SetNewPreferredStage(stage.mapName));
        }
        foreach (var skill in skillButtons)
        {
            skill.button.onClick.AddListener(() => OnNewSkillChosen(skill.skillName));
        }

        InitMenu();
    }

    void InitMenu()
    {
        skillOneIcon.texture = MatchData.instance.skillDatabase.prefabDictionary[PlayerLoadout.currentLoadout.skillOne].skillIcon;
        skillTwoIcon.texture = MatchData.instance.skillDatabase.prefabDictionary[PlayerLoadout.currentLoadout.skillTwo].skillIcon;

        skillOneName.text = PlayerLoadout.currentLoadout.skillOne.ToString();
        skillTwoName.text = PlayerLoadout.currentLoadout.skillTwo.ToString();

        var stageName = PlayerLoadout.currentLoadout.preferredMap.ToString();
        stageName = stageName.Replace('_', ' ');
        this.stageName.text = stageName;
        stageIcon.texture = MatchData.instance.stageDatabase.mapDatabase[PlayerLoadout.currentLoadout.preferredMap].mapThumbnail;
    }

    private void OnDestroy()
    {
        foreach (var stage in stageButtons)
        {
            stage.button.onClick.RemoveAllListeners();
        }
        foreach (var skill in skillButtons)
        {
            skill.button.onClick.RemoveAllListeners();
        }
    }
    public void OnNewSkillChosen(SkillName skillName)
    {
        RawImage imageToUpdate;
        TMP_Text skillNameToUpdate;
        if (skillToChange == 1)
        {
            if (PlayerLoadout.currentLoadout.skillTwo == skillName) return;
            imageToUpdate = skillOneIcon;
            skillNameToUpdate = skillOneName;
            PlayerLoadout.currentLoadout.skillOne = skillName;
        }
        else
        {
            if (PlayerLoadout.currentLoadout.skillOne == skillName) return;
            imageToUpdate = skillTwoIcon;
            skillNameToUpdate = skillTwoName;
            PlayerLoadout.currentLoadout.skillTwo = skillName;
        }
        imageToUpdate.texture = MatchData.instance.skillDatabase.prefabDictionary[skillName].skillIcon;
        skillNameToUpdate.text = skillName.ToString();
        skillPicker.SetActive(false);
    }
    public void SetSkillToChange(int index)
    {
        index = Mathf.Clamp(index, 1, 2);
        skillToChange = index;
        skillPicker.SetActive(true);
    }
    public void SetNewPreferredStage(MapName preferredMap)
    {
        PlayerLoadout.currentLoadout.preferredMap = preferredMap;
        stageIcon.texture = MatchData.instance.stageDatabase.mapDatabase[preferredMap].mapThumbnail;
        var stageName = preferredMap.ToString();
        stageName = stageName.Replace('_', ' ');
        this.stageName.text = stageName;
        stagePicker.SetActive(false);
    }
}
