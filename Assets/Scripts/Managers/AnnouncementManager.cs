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

    List<AnnouncementData> queuedAnnouncements = new();
    AnnouncementData currentAnnouncement;

    [HideInInspector] public bool announcementPlaying = false;

    int tweenTracker = 0;
    int announcementTracker = 0;
    float originalTimescale = 1f;


    public void QueueNewAnnouncement(params AnnouncementData[]  data)
    {
        foreach (var item in data)
        {
            queuedAnnouncements.Add(item);
        }
        queuedAnnouncements = queuedAnnouncements.OrderByDescending(a => a.priority).ToList();
        if (currentAnnouncement == null)
        {
            DisplayAnnouncement(queuedAnnouncements[0]);
        }
    }


    public void DisplayAnnouncement(AnnouncementData data)
    {
        announcementPlaying = true;
        currentAnnouncement = data;
        UIPanel.SetActive(true);
        announcementDisplay.text = data.announcementText;
        Time.timeScale = data.customTimescale;

       
    }
    private void Update()
    {
        if (currentAnnouncement != null)
        {
            currentAnnouncement.announcementDuration--;
            if (currentAnnouncement.announcementDuration == 0)
            {
                OnAnnouncementOver();
            }
        }

        if (tweenTracker < TWEEN_TO_REGULAR_SPEED_DURATION)
        {
            Time.timeScale = Mathf.Lerp(originalTimescale, 1.0f, TWEEN_TO_REGULAR_SPEED_DURATION);
            tweenTracker++;
        }
    }

    public void ResetManager()
    {
        queuedAnnouncements.Clear();
        OnAnnouncementOver();
    }
 
    void OnAnnouncementOver()
    {
    
        if (queuedAnnouncements.Count <= 0)
        {
            UIPanel.SetActive(false);
            originalTimescale = Time.timeScale;
            tweenTracker = 0;
            
           
            currentAnnouncement = null;
            announcementPlaying = false;
        }
        else
        {
            var next = queuedAnnouncements[0];
            queuedAnnouncements.RemoveAt(0);
            DisplayAnnouncement(next);
        }
        
    }

}
public class AnnouncementData
{
    public float customTimescale = 1.0f;
    public string announcementText = string.Empty;
    public int announcementDuration = 60;
    public int priority = 1;


    public AnnouncementData() { }

    public  AnnouncementData(AnnouncementData data)
    {
        customTimescale = data.customTimescale;
        announcementText = data.announcementText;
        announcementDuration = data.announcementDuration;
        priority = data.priority;
    }
}



