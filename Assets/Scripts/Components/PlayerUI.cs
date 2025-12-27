using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerUI : MonoBehaviour
{
    BaseSpeaker speakerOwner;
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
        UIBackdrop.color = UIColors[cha.teamIndex - 1];
        if (info != null)  SetSkillIcons(info.skillOne, info.skillTwo);
        cha.characterStateMachine.updatedSkills.AddListener(SetSkillIcons);
    }

    private void Update()
    {
        
        SetStaminaWheelValues();

        SetSkillIconColors();
    }

    void SetSkillIconColors()
    {
        if (speakerOwner == null) return;
        if (skillOneIcon.gameObject.activeSelf)
        {
            skillOneIcon.color = speakerOwner.characterStateMachine.TryGetSkill(1).SkillAvailable()? skillAvailable : skillUnavailable;
        }
        if (skillTwoIcon.gameObject.activeSelf)
        {
            skillTwoIcon.color = speakerOwner.characterStateMachine.TryGetSkill(2).SkillAvailable() ? skillAvailable : skillUnavailable;
        }

    }

    void SetStaminaWheelValues()
    {
        if (speakerOwner == null) { return; }
        var staminaComponent = speakerOwner.staminaComponent;
        float usableStamina = staminaComponent.GetStamina();
        maxStaminaImage.fillAmount = staminaComponent.GetMaxStamina() / StaminaComponent.DEFAULT_MAX_STAMINA;
        usableStaminaImage.fillAmount = usableStamina / StaminaComponent.DEFAULT_MAX_STAMINA;
        grayStaminaImage.fillAmount = (usableStamina + staminaComponent.GetGrayStamina()) / StaminaComponent.DEFAULT_MAX_STAMINA;
        if (grayStaminaImage.fillAmount > maxStaminaImage.fillAmount) { grayStaminaImage.fillAmount = maxStaminaImage.fillAmount; }
        if (staminaComponent.HasForesight()) usableStaminaImage.color = foresightStamina;
        else usableStaminaImage.color = staminaComponent.InDangerZone() ? dangerStamina : healthyStamina;
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