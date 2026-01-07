using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{

    [SerializeField] AudioSource bgmSource;

    [SerializeField] List<AudioClip> bgmTracks = new();
    private void Awake()
    {
        PlayNewTrack();
    }

    public void PlayNewTrack()
    {
        if (bgmSource == null) bgmSource = GetComponentInChildren<AudioSource>();
        if (bgmTracks.Count == 0 || bgmSource == null)
        {
            Debug.LogWarning("BGMManager: No BGM tracks assigned or AudioSource is null.");
            return;
        }
        var index = Random.Range(0, bgmTracks.Count);
        bgmSource.clip = bgmTracks[index];
        bgmSource.Play();
    }
}
