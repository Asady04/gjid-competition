using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Start()
    {
        musicSlider.value = AudioManager.GetMusicVolume();
        sfxSlider.value = AudioManager.GetSFXVolume();

        musicSlider.onValueChanged.AddListener(AudioManager.SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(AudioManager.SetSFXVolume);
    }
}
