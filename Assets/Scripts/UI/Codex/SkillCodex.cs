using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using XCharts.Runtime;

public class SkillCodex : MonoBehaviour
{
    SkillName highlightedSkill = SkillName.Advance;

    [SerializeField] TMP_Text skillName;
    [SerializeField] TMP_Text skillSubtitle;
    [SerializeField] TMP_Text skillDescription;
    [SerializeField] TMP_Text skillAccessibility;
    [SerializeField] TMP_Text skillMastery;
    [SerializeField] TMP_Text skillType;
    [SerializeField] RawImage skillIcon;
    [SerializeField] RadarChart attributesChart;

    [SerializeField] SkillDatabase skillDatabase;


    int totalSkills = 0;

    private void Awake()
    {
        totalSkills = Enum.GetValues(typeof(SkillName)).Length - 1; //gets rid of none
        UpdateSkillDisplay();
    }

 
    public void ChangeHighlightedSkill(int dir)
    {
        int currentIndex = (int)highlightedSkill;
        int newIndex = currentIndex + dir;

        newIndex = (newIndex % totalSkills + totalSkills) % totalSkills;

        highlightedSkill = (SkillName)newIndex;

        UpdateSkillDisplay();
    }

    public void UpdateSkillDisplay()
    {
        var skillInfo = skillDatabase.prefabDictionary[highlightedSkill];

        if (skillInfo == null)
        {
            Debug.LogWarning("Couldn't find skill " + highlightedSkill.ToString());
            return;
        }

        skillName.text = highlightedSkill.ToString();
        skillSubtitle.text = skillInfo.skillSubtitle;
        skillDescription.text = skillInfo.skillDescription;
        skillAccessibility.text = skillInfo.skillFloor.ToString();
        skillMastery.text = skillInfo.skillCeiling.ToString();
        skillType.text = skillInfo.type.ToString();
        skillIcon.texture = skillInfo.skillIcon;

        var serie = attributesChart.GetSerie(0);


        serie.data[0].data[0] = skillInfo.archetypeStats.mobilityScore;
        serie.data[0].data[1] = skillInfo.archetypeStats.controlScore;
        serie.data[0].data[2] = skillInfo.archetypeStats.tempoScore;
        serie.data[0].data[3] = skillInfo.archetypeStats.offenseScore;
        serie.data[0].data[4] = skillInfo.archetypeStats.defenseScore;

      
        attributesChart.RefreshChart();
        attributesChart.AnimationFadeIn(true);

        // VERIFY what actually got set
        Debug.Log($"=== {highlightedSkill} Chart Values ===");
        Debug.Log($"Mobility (0): {serie.GetData(0, 0)} (expected: {skillInfo.archetypeStats.mobilityScore})");
        Debug.Log($"Control (1): {serie.GetData(0, 1)} (expected: {skillInfo.archetypeStats.controlScore})");
        Debug.Log($"Tempo (2): {serie.GetData(0, 2)} (expected: {skillInfo.archetypeStats.tempoScore})");
        Debug.Log($"Offense (3): {serie.GetData(0, 3)} (expected: {skillInfo.archetypeStats.offenseScore})");
        Debug.Log($"Defense (4): {serie.GetData(0, 4)} (expected: {skillInfo.archetypeStats.defenseScore})");

    }
}

