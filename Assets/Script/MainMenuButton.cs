using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class MainMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Image Reference")]
    [SerializeField] private Image buttonImage;

    [Header("Shader Reference")]
    [SerializeField] private Shader colorOverrideShader;

    [Header("Color Settings")]
    [SerializeField] private Color hoverColor = new Color(0.7f, 0.15f, 0.1f);

    [Header("Animation Settings")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private Ease easeType = Ease.OutQuad;

    [Header("Audio")]
    [Tooltip("Override hover SFX (empty = use AudioManager.sfxButtonHover)")]
    [SerializeField] private AudioClip sfxHover;
    [Tooltip("Override click SFX (empty = use AudioManager.sfxButtonClick)")]
    [SerializeField] private AudioClip sfxClick;
    [Tooltip("If true, pressing this button will fade out the BGM before switching scene.")]
    [SerializeField] private bool isStartButton = false;
    [Tooltip("BGM fade duration in seconds (only used when isStartButton = true).")]
    [SerializeField] private float bgmFadeDuration = 1f;

    private Vector3 originalScale;
    private Material instancedMaterial;
    private bool isHovered = false;

    void Start()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        originalScale = transform.localScale;

        if (colorOverrideShader == null)
            colorOverrideShader = Shader.Find("UI/ColorOverride");

        if (buttonImage != null && colorOverrideShader != null)
        {
            instancedMaterial = new Material(colorOverrideShader);
            instancedMaterial.SetColor("_HoverColor", hoverColor);
            instancedMaterial.SetFloat("_HoverProgress", 0f);
            buttonImage.material = instancedMaterial;
        }
        else if (colorOverrideShader == null)
        {
            Debug.LogWarning("UIColorOverride shader 'UI/ColorOverride' not found!");
        }

        ResetVisualsInstant();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        AnimateHover(true);

        // Play hover SFX
        if (AudioManager.Instance != null)
        {
            AudioClip clip = sfxHover != null ? sfxHover : AudioManager.Instance.sfxButtonHover;
            AudioManager.Instance.PlaySFX(clip);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        AnimateHover(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Play click SFX
        if (AudioManager.Instance != null)
        {
            AudioClip clip = sfxClick != null ? sfxClick : AudioManager.Instance.sfxButtonClick;
            AudioManager.Instance.PlaySFX(clip);

            // If this is the Start/Play button, fade out BGM
            if (isStartButton)
                AudioManager.Instance.StopBGM(bgmFadeDuration);
        }
    }

    private void AnimateHover(bool showHover)
    {
        transform.DOKill();
        if (instancedMaterial != null) instancedMaterial.DOKill();

        float targetScaleMultiplier = showHover ? hoverScale : 1.0f;
        Vector3 targetScale = originalScale * targetScaleMultiplier;
        transform.DOScale(targetScale, duration).SetEase(easeType).SetLink(gameObject);

        if (instancedMaterial != null)
        {
            float targetProgress = showHover ? 1f : 0f;
            instancedMaterial.DOFloat(targetProgress, "_HoverProgress", duration)
                .SetEase(easeType).SetLink(gameObject);
        }
    }

    private void ResetVisualsInstant()
    {
        transform.DOKill();
        transform.localScale = originalScale;

        if (instancedMaterial != null)
        {
            instancedMaterial.DOKill();
            instancedMaterial.SetFloat("_HoverProgress", 0f);
        }
    }

    void OnDisable()
    {
        ResetVisualsInstant();
        isHovered = false;
    }

    void OnDestroy()
    {
        transform.DOKill();
        if (instancedMaterial != null)
        {
            instancedMaterial.DOKill();
            Destroy(instancedMaterial);
        }
    }
}
