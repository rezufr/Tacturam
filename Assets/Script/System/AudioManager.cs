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
        public AudioClip[] musicClips;
        public bool shufflePlaylist = false;
        public bool loopPlaylist = true;
        public bool loopSingleClip = true;
    }

    [Header("Music Playback")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private SceneMusicEntry[] sceneMusicMap;

    private const string PrefSFX = "SavedSFXVolume";
    private const string PrefMusic = "SavedMusicVolume";

    private Coroutine fadeCoroutine;
    private AudioClip[] activeMusicClips;
    private int activeMusicIndex = -1;
    private bool activeMusicLoopPlaylist = true;
    private bool activeMusicLoopSingle = true;
    private bool isMusicTransitioning = false;

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

    private void Update()
    {
        UpdatePlaylistPlayback();
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

        PlaySceneMusic(GetEntryForScene(scene.name));
    }

    private SceneMusicEntry GetEntryForScene(string sceneName)
    {
        foreach (var entry in sceneMusicMap)
        {
            if (entry.sceneName == sceneName)
                return entry;
        }
        return null;
    }

    private AudioClip[] GetClipsForEntry(SceneMusicEntry entry)
    {
        if (entry == null)
            return null;

        if (entry.musicClips != null && entry.musicClips.Length > 0)
        {
            int validCount = 0;
            for (int i = 0; i < entry.musicClips.Length; i++)
            {
                if (entry.musicClips[i] != null)
                    validCount++;
            }

            if (validCount == 0)
                return null;

            AudioClip[] clips = new AudioClip[validCount];
            int index = 0;
            for (int i = 0; i < entry.musicClips.Length; i++)
            {
                if (entry.musicClips[i] != null)
                    clips[index++] = entry.musicClips[i];
            }

            return clips;
        }

        if (entry.musicClip != null)
            return new[] { entry.musicClip };

        return null;
    }

    public void PlayMusic(AudioClip newClip)
    {
        PlayMusic(newClip, false);
    }

    public void PlayMusic(AudioClip newClip, bool loopCurrentClip)
    {
        if (musicSource == null || newClip == null) return;
        if (musicSource.clip == newClip && musicSource.isPlaying && musicSource.loop == loopCurrentClip) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToNewTrack(newClip, loopCurrentClip));
    }

    private System.Collections.IEnumerator FadeToNewTrack(AudioClip newClip, bool loopCurrentClip)
    {
        isMusicTransitioning = true;
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
        musicSource.loop = loopCurrentClip;
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
        isMusicTransitioning = false;
    }

    private void PlaySceneMusic(SceneMusicEntry entry)
    {
        activeMusicClips = GetClipsForEntry(entry);
        activeMusicIndex = -1;
        activeMusicLoopPlaylist = entry != null && entry.loopPlaylist;
        activeMusicLoopSingle = entry != null && entry.loopSingleClip;

        if (activeMusicClips == null || activeMusicClips.Length == 0)
            return;

        if (entry != null && entry.shufflePlaylist && activeMusicClips.Length > 1)
            ShuffleAudioClips(activeMusicClips);

        activeMusicIndex = 0;
        PlayMusic(activeMusicClips[activeMusicIndex], activeMusicClips.Length == 1 && activeMusicLoopSingle);
    }

    private void UpdatePlaylistPlayback()
    {
        if (isMusicTransitioning || musicSource == null || activeMusicClips == null || activeMusicClips.Length <= 1)
            return;

        if (musicSource.isPlaying)
            return;

        PlayNextPlaylistTrack();
    }

    private void PlayNextPlaylistTrack()
    {
        if (activeMusicClips == null || activeMusicClips.Length == 0)
            return;

        activeMusicIndex++;

        if (activeMusicIndex >= activeMusicClips.Length)
        {
            if (!activeMusicLoopPlaylist)
                return;

            activeMusicIndex = 0;

            if (activeMusicClips.Length > 1)
                ShuffleAudioClips(activeMusicClips);
        }

        AudioClip nextClip = activeMusicClips[activeMusicIndex];
        if (nextClip != null)
            PlayMusic(nextClip, false);
    }

    private void ShuffleAudioClips(AudioClip[] clips)
    {
        for (int i = 0; i < clips.Length; i++)
        {
            int randomIndex = Random.Range(i, clips.Length);
            AudioClip temp = clips[i];
            clips[i] = clips[randomIndex];
            clips[randomIndex] = temp;
        }
    }
}