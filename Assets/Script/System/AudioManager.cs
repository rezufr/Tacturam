using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sliders")]
    [SerializeField] private Slider soundSliderSFX;
    [SerializeField] private Slider soundSliderMusic;

    [Header("Slider Tags (harus sama persis kayak di Tag Manager)")]
    [SerializeField] private string sfxSliderTag = "SFXSlider";
    [SerializeField] private string musicSliderTag = "MusicSlider";

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Exposed Parameter Names (harus sama persis kayak di Mixer)")]
    [SerializeField] private string sfxParam = "SFXVolume";
    [SerializeField] private string musicParam = "MusicVolume";

    [Header("Snapshots (Ducking)")]
    [SerializeField] private AudioMixerSnapshot normalSnapshot;
    [SerializeField] private AudioMixerSnapshot duckedSnapshot;
    [SerializeField] private float duckTransitionTime = 0.3f;

    [System.Serializable]
    public class SceneMusicEntry
    {
        public string sceneName;
        public AudioClip musicClip;
    }

    [Header("Music Playback")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private SceneMusicEntry[] sceneMusicMap;

    private const string PrefSFX = "SavedSFXVolume";
    private const string PrefMusic = "SavedMusicVolume";

    private Coroutine fadeCoroutine;

    // ---------------- Singleton & Lifecycle ----------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RelinkSliders();
        LoadSavedVolumes();
    }

    // ---------------- Slider Re-linking per Scene ----------------

    private void RelinkSliders()
    {
        // soundSliderMaster = FindSliderByTag(masterSliderTag);
        soundSliderSFX = FindSliderByTag(sfxSliderTag);
        soundSliderMusic = FindSliderByTag(musicSliderTag);
    }

    private Slider FindSliderByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return null;

        GameObject found = GameObject.FindWithTag(tag);
        if (found == null) return null;

        return found.GetComponent<Slider>();
    }

    // ---------------- Volume Sliders ----------------

    private void LoadSavedVolumes()
    {
        float savedSFX = PlayerPrefs.GetFloat(PrefSFX, 100f);
        float savedMusic = PlayerPrefs.GetFloat(PrefMusic, 100f);

        if (soundSliderSFX != null) soundSliderSFX.SetValueWithoutNotify(savedSFX);
        if (soundSliderMusic != null) soundSliderMusic.SetValueWithoutNotify(savedMusic);

        ApplyVolume(sfxParam, savedSFX);
        ApplyVolume(musicParam, savedMusic);
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat(PrefSFX, value);
        ApplyVolume(sfxParam, value);
    }

    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(PrefMusic, value);
        ApplyVolume(musicParam, value);
    }

    private void ApplyVolume(string parameterName, float sliderValue)
    {
        if (sliderValue < 1f) sliderValue = 0.001f;

        float dB = Mathf.Log10(sliderValue / 100f) * 20f;
        audioMixer.SetFloat(parameterName, dB);
    }

    // ---------------- Ducking ----------------

    public void DuckVolume()
    {
        duckedSnapshot.TransitionTo(duckTransitionTime);
    }

    public void RestoreVolume()
    {
        normalSnapshot.TransitionTo(duckTransitionTime);
    }

    // ---------------- Music Auto-Detect per Scene ----------------

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RelinkSliders();
        LoadSavedVolumes();

        AudioClip clipForScene = GetClipForScene(scene.name);

        if (clipForScene != null)
        {
            PlayMusic(clipForScene);
        }
    }

    private AudioClip GetClipForScene(string sceneName)
    {
        foreach (var entry in sceneMusicMap)
        {
            if (entry.sceneName == sceneName)
                return entry.musicClip;
        }
        return null;
    }

    public void PlayMusic(AudioClip newClip)
    {
        if (musicSource.clip == newClip && musicSource.isPlaying) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToNewTrack(newClip));
    }

    private System.Collections.IEnumerator FadeToNewTrack(AudioClip newClip)
    {
        float startVolume = musicSource.volume;

        if (musicSource.isPlaying)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
                yield return null;
            }
        }

        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        float t2 = 0f;
        while (t2 < fadeDuration)
        {
            t2 += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, startVolume, t2 / fadeDuration);
            yield return null;
        }

        musicSource.volume = startVolume;
    }
}