using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class SettingsMenuController : SwingingMenuPanel
{
    [Header("Volume Sliders (assign in Inspector)")]
    [Tooltip("Slider for overall game volume (0–1).")]
    public Slider MasterSlider;

    [Tooltip("Slider for background music volume (0–1). Requires AudioMixer.")]
    public Slider MusicSlider;

    [Tooltip("Slider for sound effects volume (0–1). Requires AudioMixer.")]
    public Slider SFXSlider;

    [SerializeField] private AudioClip clickSFX; // Reference to the AudioClip for click sound effects


    public void OpenSettings() => OpenPanel();

    /// <summary>Kept for existing Editor event bindings.</summary>
    public void CloseSettings() => ClosePanel();

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void Play()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
