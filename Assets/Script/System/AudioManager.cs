using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Singleton AudioManager — persists across all scenes.
/// Manages looping BGM (with cross-fade) and one-shot SFX.
/// All volume settings are saved to PlayerPrefs.
///
/// HOW TO USE IN INSPECTOR:
///   1. Place this on a persistent GameObject in your Boot/first scene.
///   2. (Optional) Assign an AudioMixer with exposed params:
///      "MasterVolume", "MusicVolume", "SFXVolume" (dB range -80..0).
///   3. Assign BGM clips: bgmMainMenu, bgmIngame, bgmSettings.
///   4. Assign SFX clips: sfxButtonHover, sfxButtonClick,
///      sfxCardHover, sfxCardClick, sfxCardDraw, sfxCardVanish.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static AudioManager Instance { get; private set; }

    // ── Audio Mixer (optional) ─────────────────────────────────────────────
    [Header("Audio Mixer (optional)")]
    public AudioMixer Mixer;

    [Header("Mixer Exposed Parameter Names")]
    public string MasterParam = "MasterVolume";
    public string MusicParam  = "MusicVolume";
    public string SFXParam    = "SFXVolume";

    // ── BGM Clips ──────────────────────────────────────────────────────────
    [Header("BGM Clips")]
    public AudioClip bgmMainMenu;
    public AudioClip bgmIngame;
    public AudioClip bgmSettings;

    // ── SFX Clips ──────────────────────────────────────────────────────────
    [Header("SFX Clips — Buttons")]
    public AudioClip sfxButtonHover;
    public AudioClip sfxButtonClick;

    [Header("SFX Clips — Cards")]
    public AudioClip sfxCardHover;
    public AudioClip sfxCardClick;
    public AudioClip sfxCardDraw;
    public AudioClip sfxCardVanish;

    // ── PlayerPrefs Keys ───────────────────────────────────────────────────
    private const string KEY_MASTER = "Vol_Master";
    private const string KEY_MUSIC  = "Vol_Music";
    private const string KEY_SFX    = "Vol_SFX";

    // ── Cached volume values (0–1 linear) ──────────────────────────────────
    private float _master;
    private float _music;
    private float _sfx;

    // ── Internal AudioSources ──────────────────────────────────────────────
    private AudioSource _bgmSource;
    private AudioSource _sfxSource;

    // ── Fade coroutine handle ──────────────────────────────────────────────
    private Coroutine _fadeCo;

    // ── Volume Properties ──────────────────────────────────────────────────
    public float MasterVolume
    {
        get => _master;
        set
        {
            _master = Mathf.Clamp01(value);
            ApplyMaster();
            PlayerPrefs.SetFloat(KEY_MASTER, _master);
            PlayerPrefs.Save();
        }
    }

    public float MusicVolume
    {
        get => _music;
        set
        {
            _music = Mathf.Clamp01(value);
            ApplyMusic();
            PlayerPrefs.SetFloat(KEY_MUSIC, _music);
            PlayerPrefs.Save();
        }
    }

    public float SFXVolume
    {
        get => _sfx;
        set
        {
            _sfx = Mathf.Clamp01(value);
            ApplySFX();
            PlayerPrefs.SetFloat(KEY_SFX, _sfx);
            PlayerPrefs.Save();
        }
    }

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create BGM AudioSource (loops continuously)
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop        = true;
        _bgmSource.playOnAwake = false;

        // Create SFX AudioSource (one-shot, no loop)
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop        = false;
        _sfxSource.playOnAwake = false;

        LoadSettings();
    }

    // ── BGM Control ────────────────────────────────────────────────────────

    /// <summary>Play a BGM clip, cross-fading out the current one first.</summary>
    public void PlayBGM(AudioClip clip, float fadeDuration = 0.5f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] PlayBGM called with a null clip.");
            return;
        }

        // Already playing the same clip — do nothing
        if (_bgmSource.isPlaying && _bgmSource.clip == clip) return;

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CrossFadeBGM(clip, fadeDuration));
    }

    /// <summary>Fade out and stop the current BGM.</summary>
    public void StopBGM(float fadeDuration = 1f)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeOutBGM(fadeDuration));
    }

    // Convenience wrappers
    public void PlayMainMenuBGM(float fade = 0.5f) => PlayBGM(bgmMainMenu,  fade);
    public void PlayIngameBGM  (float fade = 0.5f) => PlayBGM(bgmIngame,    fade);
    public void PlaySettingsBGM(float fade = 0.5f) => PlayBGM(bgmSettings,  fade);

    // ── SFX Control ────────────────────────────────────────────────────────

    /// <summary>Play a one-shot SFX clip, respecting SFX volume.</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        // Volume = SFX channel * master channel (without mixer)
        float vol = Mixer != null ? 1f : _sfx * _master;
        _sfxSource.PlayOneShot(clip, vol);
    }

    // ── Private Helpers ────────────────────────────────────────────────────
    private void LoadSettings()
    {
        _master = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        _music  = PlayerPrefs.GetFloat(KEY_MUSIC,  1f);
        _sfx    = PlayerPrefs.GetFloat(KEY_SFX,    1f);

        ApplyMaster();
        ApplyMusic();
        ApplySFX();
    }

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
        {
            Mixer.SetFloat(MusicParam, LinearToDecibel(_music));
        }
        else
        {
            // No mixer — drive BGM source volume directly
            if (_bgmSource != null)
                _bgmSource.volume = _music;
        }
    }

    private void ApplySFX()
    {
        if (Mixer != null)
            Mixer.SetFloat(SFXParam, LinearToDecibel(_sfx));
        // Without mixer, sfx vol is applied per-PlayOneShot call
    }

    private static float LinearToDecibel(float linear)
        => linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;

    // ── Coroutines ─────────────────────────────────────────────────────────
    private IEnumerator CrossFadeBGM(AudioClip newClip, float duration)
    {
        float targetVol = Mixer != null ? 1f : _music;
        float half      = duration * 0.5f;

        // Fade OUT current BGM
        if (_bgmSource.isPlaying)
        {
            float startVol = _bgmSource.volume;
            float elapsed  = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                _bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / half);
                yield return null;
            }
        }

        // Swap clip
        _bgmSource.Stop();
        _bgmSource.clip   = newClip;
        _bgmSource.volume = 0f;
        _bgmSource.Play();

        // Fade IN new BGM
        float elapsed2 = 0f;
        while (elapsed2 < half)
        {
            elapsed2 += Time.unscaledDeltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, targetVol, elapsed2 / half);
            yield return null;
        }

        _bgmSource.volume = targetVol;
        _fadeCo = null;
    }

    private IEnumerator FadeOutBGM(float duration)
    {
        float startVol = _bgmSource.volume;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        _bgmSource.Stop();
        _bgmSource.volume = Mixer != null ? 1f : _music;
        _fadeCo = null;
    }
}
