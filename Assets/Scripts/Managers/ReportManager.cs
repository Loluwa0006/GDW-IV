using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime;

public class ReportManager : MonoBehaviour
{

    public const float STAMINA_LINE_WIDTH = 7.5f;
    public const float STAMINA_DOT_SIZE = 4;
    [Header("P1 Data")]
    [SerializeField] TMP_Text p1SkillOneUsage;
    [SerializeField] TMP_Text p1SkillTwoUsage;
    [SerializeField] TMP_Text p1SkillOneName;
    [SerializeField] TMP_Text p1SkillTwoName;
    [SerializeField] TMP_Text p1ForesightUsage;
    [SerializeField] TMP_Text p1PerfectDeflects;
    [SerializeField] TMP_Text p1PartialDeflects;
    [SerializeField] TMP_Text p1AverageDeflectTiming;
    [SerializeField] LineChart p1StaminaChart;
    [SerializeField] GameObject p1Report;

    [Header("P2 Data")]
    [SerializeField] TMP_Text p2SkillOneUsage;
    [SerializeField] TMP_Text p2SkillTwoUsage;
    [SerializeField] TMP_Text p2SkillOneName;
    [SerializeField] TMP_Text p2SkillTwoName;
    [SerializeField] TMP_Text p2ForesightUsage;
    [SerializeField] TMP_Text p2PerfectDeflects;
    [SerializeField] TMP_Text p2PartialDeflects;
    [SerializeField] TMP_Text p2AverageDeflectTiming;
    [SerializeField] LineChart p2StaminaChart;
    [SerializeField] GameObject p2Report;

    [Header("Other")]
    public GameObject reportDisplay;

    public class PlayerData
    {
        public List<StaminaEntry> staminaTracker = new();
        public int skillOneUsage;
        public int skillTwoUsage;
        public int foresightUsage;
        public int perfectDeflects;
        public int partialDeflects;
        public float averageTiming;
        public string skillOneName;
        public string skillTwoName;
        public List<float> deflectTimings = new();
    }

    public class TrackerData
    {
        public BaseSpeaker speaker;
        public MatchData.PlayerInfo speakerInfo;
    }

    public Dictionary<BaseCharacter, PlayerData> speakerDictionary = new();

    public struct StaminaEntry
    {
        public int second;
        public int stamina;
    }

    bool trackTime;
    float timeElapsed = 0;

    private void Awake()
    {
        reportDisplay.SetActive(false);
    }
    public void OnSpeakerDeflect(BaseSpeaker speaker,bool partial, float time)
    {
        if (!speakerDictionary.ContainsKey(speaker))
        {
            return;
        }
        PlayerData data = speakerDictionary[speaker];
        if (partial) data.partialDeflects += 1;
        else data.perfectDeflects += 1;
        data.deflectTimings.Add(time);
    }
    public void InitManager(GamemodeDatabase.GameModeName currentGameMode, params TrackerData[] speakerData)
    {
        switch (currentGameMode)
        {
            case GamemodeDatabase.GameModeName.SpeakerDuel:
            case GamemodeDatabase.GameModeName.EchoSurvival:
            speakerDictionary.Clear();
            int index = 0;
            foreach (var data in speakerData)
            {
                speakerDictionary.Add(data.speaker, new PlayerData());
                speakerDictionary[data.speaker].skillOneName = data.speakerInfo.skillOne.ToString();
                speakerDictionary[data.speaker].skillTwoName = data.speakerInfo.skillTwo.ToString();

                if (index == 0)
                {
                    p1SkillOneName.text = data.speakerInfo.skillOne.ToString() + " Uses";
                    p1SkillTwoName.text = data.speakerInfo.skillTwo.ToString() + " Uses";
                }
                else
                {
                    p2SkillOneName.text = data.speakerInfo.skillOne.ToString() + " Uses";
                    p2SkillTwoName.text = data.speakerInfo.skillTwo.ToString() + " Uses";
                }
                index++;
            }
                break;
    }
    }
    

    public void OnMatchStart()
    {
        timeElapsed = 0;
        trackTime = true;       
    }

    public void OnForesightUsed(BaseCharacter speaker)
    {
        speakerDictionary[speaker].foresightUsage += 1;
    }

    public void OnSkillUsed(BaseCharacter speaker, int index)
    {
        if (index == 1) speakerDictionary[speaker].skillOneUsage += 1;
        else speakerDictionary[speaker].skillTwoUsage += 1;
    }
    public void OnMatchEnd()
    {
        trackTime = false;

        float deflectTotal = 0;
        int index = 0;
        foreach (var data in speakerDictionary)
        {
            deflectTotal = 0;
            foreach (var timing in data.Value.deflectTimings)
            {
                deflectTotal += timing;
            }

            float avgTiming = deflectTotal / data.Value.deflectTimings.Count;
            if (avgTiming == float.PositiveInfinity || avgTiming == float.NaN || deflectTotal == 0) avgTiming = 0;
            speakerDictionary[data.Key].averageTiming = avgTiming;
            if (index == 0)
            {
                InitStaminaChart(p1StaminaChart, data.Key);
                p1SkillOneUsage.text = data.Value.skillOneUsage.ToString();
                p1SkillTwoUsage.text = data.Value.skillTwoUsage.ToString();
                p1ForesightUsage.text = data.Value.foresightUsage.ToString();
                p1PerfectDeflects.text = data.Value.perfectDeflects.ToString();
                p1PartialDeflects.text = data.Value.partialDeflects.ToString();
                p1AverageDeflectTiming.text = data.Value.averageTiming.ToString("F2");
            }
            else
            {
                InitStaminaChart(p2StaminaChart, data.Key);
                p2SkillOneUsage.text = data.Value.skillOneUsage.ToString();
                p2SkillTwoUsage.text = data.Value.skillTwoUsage.ToString();
                p2ForesightUsage.text = data.Value.foresightUsage.ToString();
                p2PerfectDeflects.text = data.Value.perfectDeflects.ToString();
                p2PartialDeflects.text = data.Value.partialDeflects.ToString();
                p2AverageDeflectTiming.text = data.Value.averageTiming.ToString("F2");
            }
            index++;
        }

        p2Report.SetActive(speakerDictionary.Count >= 2);
        
    }

    void InitStaminaChart(LineChart chart, BaseCharacter cha)
    {
        var data = speakerDictionary[cha];

        chart.ClearData();

        if (chart.series.Count == 0)
        {
            Debug.Log("There's no series");
            var series = chart.AddSerie<Line>("StaminaOverTime");
        }


        foreach (var point in data.staminaTracker)
        {
            Debug.Log("At second " + point.second.ToString() + ", " + cha.name + " was at " + point.stamina + " stamina.");
            chart.AddXAxisData(point.second.ToString());
            chart.AddData(0, point.stamina);
        }

        Line line = chart.GetSerie<Line>();
        line.AnimationEnable(false);
    }

    private void Update()
    {
        if (!trackTime) return;
        int prevSecond = Mathf.RoundToInt(timeElapsed);
        timeElapsed += Time.deltaTime;
        int newSecond = Mathf.RoundToInt(timeElapsed);
        if (newSecond > prevSecond)
        {
            foreach (var speaker in speakerDictionary.Keys)
            {
                StaminaEntry newEntry = new();
                newEntry.second = newSecond;
                newEntry.stamina = Mathf.RoundToInt(speaker.staminaComponent.GetStamina());
                speakerDictionary[speaker].staminaTracker.Add(newEntry);
            }
        }
    }
}
