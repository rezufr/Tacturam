using UnityEngine;

/// <summary>
/// Place this on any GameObject in the MainMenu scene.
/// It will automatically start the Main Menu BGM when the scene loads.
/// </summary>
public class MainMenuBootstrap : MonoBehaviour
{
    [Tooltip("Fade duration when starting the main menu BGM.")]
    [SerializeField] private float bgmFadeIn = 0.5f;

    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMainMenuBGM(bgmFadeIn);
    }
}
