using System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] Slider BGMSlider;
    [SerializeField] Slider SFXSlider;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] TMP_Dropdown resolutionOptions;

    [SerializeField] Button saveButton;

    [SerializeField] AudioMixer gameAudio;

    public void InitSettings()
    {
        InitAudioSettings();
        InitVideoSettings();
        saveButton.gameObject.SetActive(false);
    }

    void InitAudioSettings()
    {
        if (PlayerPrefs.HasKey("BGMVolume"))
        {
            gameAudio.GetFloat("BGMVolume", out float bgmLOG);
            Debug.Log("BGM Value before: " + bgmLOG );

            float bgmVol = PlayerPrefs.GetFloat("BGMVolume");
            Debug.Log("BGM = " + bgmVol);
            gameAudio.SetFloat("BGMVolume", bgmVol);
            gameAudio.GetFloat("BGMVolume", out float bgmNEWLOG);
            Debug.Log("BGM Value after: " + bgmNEWLOG );
        }
        if (PlayerPrefs.HasKey("SFXVolume"))
        {
            gameAudio.SetFloat("SFXVolume", PlayerPrefs.GetFloat("SFXVolume"));
        }
        gameAudio.GetFloat("BGMVolume", out float bgm);
        gameAudio.GetFloat("SFXVolume", out float sfx);

        BGMSlider.onValueChanged.RemoveAllListeners();
        SFXSlider.onValueChanged.RemoveAllListeners();

        BGMSlider.value = Mathf.Pow(10, bgm / 20);
        SFXSlider.value = Mathf.Pow(10, sfx / 20);

        BGMSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        SFXSlider.onValueChanged.AddListener(OnSFXSliderChanged);

    }

    void InitVideoSettings()
    {
        bool fullscreen = Screen.fullScreen;
        if (PlayerPrefs.HasKey("Fullscreen"))
        {
            fullscreen = PlayerPrefs.GetInt("Fullscreen") != 0;
            fullscreenToggle.isOn = fullscreen;
        }

        if (PlayerPrefs.HasKey("ResolutionX"))
        {
            int x = PlayerPrefs.GetInt("ResolutionX");
            int y = PlayerPrefs.GetInt("ResolutionY");

            Screen.SetResolution(x, y, fullscreen);
        }
    }

    public void OnBGMSliderChanged(float Value)
    {
        float newVolume = Mathf.Log10(Value) * 20;
        PlayerPrefs.SetFloat("BGMVolume", newVolume);
        gameAudio.SetFloat("BGMVolume", newVolume);
        OnChangeMade();
        Debug.Log("BGM = " + newVolume);

    }

    public void OnSFXSliderChanged(float Value)
    {
        float newVolume = Mathf.Log10(Value) * 20;
        PlayerPrefs.SetFloat("SFXVolume", newVolume);
        gameAudio.SetFloat("SFXVolume", newVolume);
        OnChangeMade();
    }

    public void OnFullscreenToggled(bool isOn)
    {
        int fullscreen = isOn ? 1 : 0;
        PlayerPrefs.SetInt("Fullscreen", fullscreen);
        fullscreenToggle.isOn = isOn;
        Screen.SetResolution(Screen.width, Screen.height, isOn);
        OnChangeMade();
    }

    public void OnResolutionChanged(int index)
    {
        string resolution = resolutionOptions.options[index].text;
        int xPosition = resolution.IndexOf('x');
        int x = int.Parse(resolution.Substring(0, xPosition));
        int y = int.Parse(resolution.Substring(xPosition + 1));

        Screen.SetResolution(x, y, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionX", x);
        PlayerPrefs.SetInt("ResolutionY", y);
        OnChangeMade();
    }

    public void OnChangeMade()
    {
        saveButton.gameObject.SetActive(true);
    }

    public void OnChangesSaved()
    {
        PlayerPrefs.Save();
        saveButton.gameObject.SetActive(false);
    }

}