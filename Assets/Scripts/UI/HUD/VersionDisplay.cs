using TMPro;
using UnityEngine;

public class VersionDisplay : MonoBehaviour
{
    [SerializeField] VersionTracker tracker;
    [SerializeField] TMP_Text display;

    private void Awake()
    {
        if (display == null)
        {
            display = GetComponent<TMP_Text>();
        }
        if (!tracker.version.Contains("v"))
        {
            display.text = "v" + tracker.version;
        }
        else
        {
            display.text = tracker.version;
        }
    }
}
