using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class BasePlayerUI : MonoBehaviour
{
    BaseCharacter speakerOwner;
    [SerializeField] TMP_Text staminaDisplay;
    [SerializeField] TMP_Text maxStaminaDisplay;
    [SerializeField] RawImage UIBackdrop;

   [SerializeField] List<Color> UIColors = new();

    [SerializeField] Image maxStaminaImage;
    [SerializeField] Image usableStaminaImage;
    [SerializeField] Image grayStaminaImage;

    [Header("Skill Icons")]

    [SerializeField] RawImage skillOneIcon;
    [SerializeField] RawImage skillTwoIcon;
    [Header("Colors")]
    [SerializeField] Color healthyStamina;
    [SerializeField] Color dangerStamina;
    [SerializeField] Color foresightStamina = Color.lightBlue;
    [SerializeField] Color skillAvailable;
    [SerializeField] Color skillUnavailable;
    private void Awake()
    {
        if (staminaDisplay == null)
        {
            staminaDisplay = GetComponent<TMP_Text>();
        }
    }
    public void InitDisplay(BaseSpeaker cha, MatchData.PlayerInfo info)
    {
        speakerOwner = cha;
        if (UIBackdrop != null) UIBackdrop.color = UIColors[cha.teamIndex - 1];
        if (info != null)  SetSkillIcons(info.skillOne, info.skillTwo);
        cha.fsm.updatedSkills.AddListener(SetSkillIcons);

        if (info.teamIndex == 2)
        {
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.localScale = new Vector3(rectTransform.localScale.x * -1.0f, 1.0f, 1.0f);
            skillOneIcon.rectTransform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
            skillTwoIcon.rectTransform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
        }
    }
    private void Update()
    {
        SetStaminaUIValues();
        SetSkillIconColors();
    }

    void SetSkillIconColors()
    {
        if (speakerOwner == null) return;
        var skillOne = speakerOwner.fsm.TryGetSkill(1);
        var skillTwo = speakerOwner.fsm.TryGetSkill(2);
        if (skillOneIcon.gameObject.activeSelf && skillOne != null)
        {
            skillOneIcon.color = skillOne.SkillAvailable()? skillAvailable : skillUnavailable;
        }
        if (skillTwoIcon.gameObject.activeSelf && skillTwo != null)
        {
            skillTwoIcon.color = skillTwo.SkillAvailable() ? skillAvailable : skillUnavailable;
        }

    }

    void SetStaminaUIValues()
    {
        if (speakerOwner == null) { return; }
        var staminaComponent = speakerOwner.staminaComponent;
        float usableStamina = staminaComponent.Stamina;
        maxStaminaImage.fillAmount = staminaComponent.MaxStamina / StaminaComponent.DEFAULT_MAX_STAMINA;
        usableStaminaImage.fillAmount = usableStamina / StaminaComponent.DEFAULT_MAX_STAMINA;
        grayStaminaImage.fillAmount = (usableStamina + staminaComponent.GrayStamina) / StaminaComponent.DEFAULT_MAX_STAMINA;
        if (grayStaminaImage.fillAmount > maxStaminaImage.fillAmount) { grayStaminaImage.fillAmount = maxStaminaImage.fillAmount; }
        if (staminaComponent.ForesightEnabled) usableStaminaImage.color = foresightStamina;
        else usableStaminaImage.color = staminaComponent.InDangerZone ? dangerStamina : healthyStamina;
     }
    public void SetSkillIcons(SkillName skillOne,SkillName skillTwo)
    {
        if (skillOne != SkillName.None)
        {
            skillOneIcon.gameObject.SetActive(true);
            skillOneIcon.texture = MatchData.instance.skillIconDictionary[skillOne];
        }
        else skillOneIcon.gameObject.SetActive(false);
        if (skillTwo != SkillName.None)
        {
            skillTwoIcon.gameObject.SetActive(true);
            skillTwoIcon.texture = MatchData.instance.skillIconDictionary[skillTwo];
        }
        else skillTwoIcon.gameObject.SetActive(false);
    }
}