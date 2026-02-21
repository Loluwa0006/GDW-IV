using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{

    [SerializeField] AudioSource winBGMPlayer;
    [SerializeField] AudioClip winSFX;

    [SerializeField] AudioSource bgmSource;

    [SerializeField] List<AudioClip> bgmTracks = new();

    AudioClip lastTrack;
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

        List<AudioClip> tracksToPickFrom = new(bgmTracks);
        if (lastTrack != null) tracksToPickFrom.Remove(lastTrack);
        var index = Random.Range(0, tracksToPickFrom.Count);
        bgmSource.clip = tracksToPickFrom[index];
        
        lastTrack = bgmSource.clip;
        bgmSource.Play();
    }

    public void ResetComponent()
    {
        winBGMPlayer.Stop();
        PlayNewTrack();
    }


    public void OnGameOver()
    {
        bgmSource.Stop();
    }

    public void OnGameWon()
    {
        winBGMPlayer.Play();
    }
}
