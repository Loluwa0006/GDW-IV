using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class HUDAnimator : MonoBehaviour
{
    int deflectStreak = 0;
    [SerializeField] Animator animator;
    [SerializeField] TMP_Text streakDisplay;
    [SerializeField] TMP_Text streakReaction;

    public Dictionary<int, string> reactionDictionary = new()
    {
        {5, "Good" },
        {10, "Great" },
        {15, "Awesome"},
        {25, "Incredible" },
        {50, "Insane" },
        {100, "Godlike" },
        {250, "Cheating" },
        {500, "???"}
    };

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        streakReaction.gameObject.SetActive(false);
    }
    public void OnEchoDeflected()
    {
        deflectStreak++;
        streakDisplay.text = deflectStreak.ToString();
        animator.Play("IncrementDeflectStreak", 0, 0.0f);
        if (reactionDictionary.Keys.Contains(deflectStreak))
        {
            streakReaction.gameObject.SetActive(true);
            streakReaction.text = reactionDictionary[deflectStreak];
        }
    }

    public void OnSpeakerStruck(DamageInfo info)
    {
        if (info.damageSource != DamageSource.Ball || deflectStreak == 0) { return; }
        deflectStreak = 0;
        streakDisplay.text = deflectStreak.ToString();
        animator.Play("EndDeflectStreak", 0, 0.0f);
        streakReaction.text = string.Empty;
        
    }

  
}
