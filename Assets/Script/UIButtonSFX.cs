using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drop this on any UI element to give it hover + click SFX.
/// If overrideHover / overrideClick are left empty, falls back
/// to the global clips on AudioManager (sfxButtonHover / sfxButtonClick).
/// </summary>
public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Override Clips (leave empty to use AudioManager defaults)")]
    [SerializeField] private AudioClip overrideHover;
    [SerializeField] private AudioClip overrideClick;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.Instance == null) return;
        AudioClip clip = overrideHover != null ? overrideHover : AudioManager.Instance.sfxButtonHover;
        AudioManager.Instance.PlaySFX(clip);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (AudioManager.Instance == null) return;
        AudioClip clip = overrideClick != null ? overrideClick : AudioManager.Instance.sfxButtonClick;
        AudioManager.Instance.PlaySFX(clip);
    }
}
