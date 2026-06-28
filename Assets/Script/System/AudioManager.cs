using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Singleton AudioManager that persists across all scenes.
/// Saves and loads volume settings via PlayerPrefs so settings
/// are remembered between play sessions.
///
/// HOW TO USE:
///   1. Create an empty GameObject in your first/boot scene.
///   2. Attach this script to it.
///   3. (Optional) Assign an AudioMixer to the 'Mixer' field in the Inspector
///      and make sure it has exposed parameters named "MasterVolume",
///      "MusicVolume", and "SFXVolume" (in dB, range -80 to 0).
///   4. If you skip the AudioMixer, volumes are applied directly to
///      AudioSource components via AudioListener.volume (master only).
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static AudioManager Instance { get; private set; }

    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Audio Mixer (optional)")]
    [Tooltip("Assign your project's AudioMixer asset here.")]
    public AudioMixer Mixer;

    [Header("Mixer Exposed Parameter Names")]
    public string MasterParam = "MasterVolume";
    public string MusicParam  = "MusicVolume";
    public string SFXParam    = "SFXVolume";

    // ── PlayerPrefs Keys ───────────────────────────────────────────────────
    private const string KEY_MASTER = "Vol_Master";
    private const string KEY_MUSIC  = "Vol_Music";
    private const string KEY_SFX    = "Vol_SFX";

    // ── Cached values (0–1 linear) ─────────────────────────────────────────
    private float _master;
    private float _music;
    private float _sfx;

    // ── Properties ────────────────────────────────────────────────────────
    public float MasterVolume
    {
        get => _master;
        set { _master = Mathf.Clamp01(value); ApplyMaster(); PlayerPrefs.SetFloat(KEY_MASTER, _master); PlayerPrefs.Save(); }
    }

    public float MusicVolume
    {
        get => _music;
        set { _music = Mathf.Clamp01(value); ApplyMusic(); PlayerPrefs.SetFloat(KEY_MUSIC, _music); PlayerPrefs.Save(); }
    }

    public float SFXVolume
    {
        get => _sfx;
        set { _sfx = Mathf.Clamp01(value); ApplySFX(); PlayerPrefs.SetFloat(KEY_SFX, _sfx); PlayerPrefs.Save(); }
    }

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // survive scene transitions

        LoadSettings();
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    /// <summary>Load saved volumes from PlayerPrefs (defaults to 1.0 if not set).</summary>
    private void LoadSettings()
    {
        _master = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        _music  = PlayerPrefs.GetFloat(KEY_MUSIC,  1f);
        _sfx    = PlayerPrefs.GetFloat(KEY_SFX,    1f);

        ApplyMaster();
        ApplyMusic();
        ApplySFX();
    }

    /// <summary>Apply master volume. Uses AudioListener if no Mixer is assigned.</summary>
    private void ApplyMaster()
    {
        if (Mixer != null)
            Mixer.SetFloat(MasterParam, LinearToDecibel(_master));
        else
            AudioListener.volume = _master;
    }

    private void ApplyMusic()
    {
        if (Mixer != null)
            Mixer.SetFloat(MusicParam, LinearToDecibel(_music));
        // Without mixer, music volume is handled by the master AudioListener
    }

    private void ApplySFX()
    {
        if (Mixer != null)
            Mixer.SetFloat(SFXParam, LinearToDecibel(_sfx));
    }

    /// <summary>Convert 0-1 linear to decibels for AudioMixer (-80 dB floor).</summary>
    private static float LinearToDecibel(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }
}
