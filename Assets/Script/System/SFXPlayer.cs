using UnityEngine;
using UnityEngine.Audio;

public class SFXPlayer : MonoBehaviour
{
    public static SFXPlayer Instance { get; private set; }

    [SerializeField] private AudioSource sfxSourcePrefab;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private int poolSize = 10;

    private AudioSource[] pool;
    private int currentIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = new GameObject($"SFX_Source_{i}");
            obj.transform.SetParent(transform);
            AudioSource src = obj.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = sfxMixerGroup;
            src.playOnAwake = false;
            pool[i] = src;
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        pool[currentIndex].PlayOneShot(clip, volume);
        currentIndex = (currentIndex + 1) % poolSize;
    }
}