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

    // ── Unity Lifecycle ────────────────────────────────────────────────────

    protected override void Start()
    {
        base.Start(); // Handles pivot adjustment to pin position — MUST be called first!

        // Wire up slider callbacks
        if (MasterSlider != null) MasterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (MusicSlider  != null) MusicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (SFXSlider    != null) SFXSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    /// <summary>Called every time the settings panel opens — sync sliders and switch BGM.</summary>
    public override void OpenPanel()
    {
        base.OpenPanel();
        SyncSlidersToAudioManager();

        // Switch to settings BGM
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySettingsBGM();
    }

    /// <summary>Called every time the settings panel closes — revert to scene BGM.</summary>
    public override void ClosePanel()
    {
        base.ClosePanel();

        // Revert BGM based on current scene
        if (AudioManager.Instance != null)
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "MainMenu")
                AudioManager.Instance.PlayMainMenuBGM();
            else
                AudioManager.Instance.PlayIngameBGM();
        }
    }

    // ── Slider Callbacks ──────────────────────────────────────────────────

    private void OnMasterChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.MasterVolume = value;
    }

    private void OnMusicChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.MusicVolume = value;
    }

    private void OnSFXChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SFXVolume = value;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Push saved AudioManager values back to sliders (without triggering callbacks).</summary>
    private void SyncSlidersToAudioManager()
    {
        if (AudioManager.Instance == null) return;

        if (MasterSlider != null) { MasterSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume); }
        if (MusicSlider  != null) { MusicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume); }
        if (SFXSlider    != null) { SFXSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume); }
    }

    // ── Backwards-compatibility wrappers ──────────────────────────────────

    /// <summary>Kept for existing Editor event bindings.</summary>
    public void OpenSettings()  => OpenPanel();

    /// <summary>Kept for existing Editor event bindings.</summary>
    public void CloseSettings() => ClosePanel();

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
