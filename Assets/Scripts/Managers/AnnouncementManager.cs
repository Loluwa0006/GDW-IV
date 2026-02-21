using System.Collections.Generic;
using System.Linq;
using TMPro;
using Tweens;
using UnityEngine;

public class AnnouncementManager : MonoBehaviour
{
    public const int TWEEN_TO_REGULAR_SPEED_DURATION = 60;


    [SerializeField] GameObject UIPanel;
    [SerializeField] TMP_Text announcementDisplay;

    Queue<AnnouncementData> queuedAnnouncements = new();
    AnnouncementData currentAnnouncement;

    [HideInInspector] public bool announcementPlaying = false;

    float timeRemaining;
    public void QueueNewAnnouncement(params AnnouncementData[]  data)
    {
        foreach (var item in data)
        {
            queuedAnnouncements.Enqueue(item);
        }
        if (!announcementPlaying)
        {
            DisplayAnnouncement();
        }
    }


    public void DisplayAnnouncement()
    {
        AnnouncementData data = queuedAnnouncements.Dequeue();
        announcementPlaying = true;
        currentAnnouncement = data;
        UIPanel.SetActive(true);
        announcementDisplay.text = data.announcementText;
        Time.timeScale = data.customTimescale;
        timeRemaining = data.announcementDuration / 60;       
    }
    private void Update()
    {
        if (announcementPlaying)
        {
            timeRemaining -= Time.unscaledDeltaTime;
            if (timeRemaining <= 0)
            {
                OnAnnouncementOver();
            }
        }

        
    }

    public void ResetManager()
    {
        queuedAnnouncements.Clear();
        UIPanel.SetActive(false);
        gameObject.CancelTweens();
        announcementPlaying = false;
        Time.timeScale = 1.0f;
    }
 
    void OnAnnouncementOver()
    {
    
        if (queuedAnnouncements.Count <= 0)
        {
            UIPanel.SetActive(false);
            FloatTween timescaleTween = new()
            {
                duration = TWEEN_TO_REGULAR_SPEED_DURATION,
                easeType = EaseType.QuadOut,
                useUnscaledTime = true,
                from = Time.timeScale,
                to = 1.0f,
                onUpdate = (trans, value) => Time.timeScale = value
            };
            gameObject.AddTween(timescaleTween);

            announcementPlaying = false;
        }
        else
        {
            DisplayAnnouncement();
        }
        
    }

}
public struct AnnouncementData
{

    public static readonly AnnouncementData winData = new ()
    {
       customTimescale = 0.1f,
       announcementDuration = 150,
       priority = 99999,
       announcementText = "VERDICT"
    };

    public float customTimescale;
    public string announcementText;
    public int announcementDuration;
    public int priority;


    
}



