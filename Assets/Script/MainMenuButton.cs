using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Image Reference")]
    [SerializeField] private Image buttonImage;

    [Header("Shader Reference")]
    [SerializeField] private Shader colorOverrideShader;

    [Header("Color Settings")]
    [SerializeField] private Color hoverColor = new Color(0.7f, 0.15f, 0.1f);     // Red hover color

    [Header("Animation Settings")]
    [SerializeField] private float hoverScale = 1.15f;      // Scale multiplier when hovered
    [SerializeField] private float duration = 0.15f;        // Transition duration in seconds
    [SerializeField] private Ease easeType = Ease.OutQuad;  // DOTween easing function

    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioClip clickSFX;

    private Vector3 originalScale;
    private Material instancedMaterial;
    private bool isHovered = false;

    void Start()
    {
        // Try getting Image component if not assigned
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        // Store the default local scale of this button
        originalScale = transform.localScale;

        // Find the custom shader if not explicitly assigned in Inspector
        if (colorOverrideShader == null)
        {
            colorOverrideShader = Shader.Find("UI/ColorOverride");
        }

        // Create an instanced material so it doesn't affect other UI elements sharing the same material
        if (buttonImage != null && colorOverrideShader != null)
        {
            instancedMaterial = new Material(colorOverrideShader);
            instancedMaterial.SetColor("_HoverColor", hoverColor);
            instancedMaterial.SetFloat("_HoverProgress", 0f);
            
            buttonImage.material = instancedMaterial;
        }
        else
        {
            if (colorOverrideShader == null)
            {
                Debug.LogWarning("UIColorOverride shader 'UI/ColorOverride' not found! Make sure UIColorOverride.shader is compiled.");
            }
        }

        // Apply normal visual state immediately at start
        ResetVisualsInstant();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        AnimateHover(true);
        SFXPlayer.Instance.PlaySFX(hoverSFX);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        AnimateHover(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SFXPlayer.Instance.PlaySFX(clickSFX);
        // Example action: Load the "SampleScene" scene when clicked
    }

    private void AnimateHover(bool showHover)
    {
        // Kill active tweens
        transform.DOKill();
        if (instancedMaterial != null)
        {
            instancedMaterial.DOKill();
        }

        // 1. Animate Scale
        float targetScaleMultiplier = showHover ? hoverScale : 1.0f;
        Vector3 targetScale = originalScale * targetScaleMultiplier;
        transform.DOScale(targetScale, duration)
            .SetEase(easeType)
            .SetLink(gameObject);

        // 2. Animate Shader Progress
        if (instancedMaterial != null)
        {
            float targetProgress = showHover ? 1f : 0f;
            instancedMaterial.DOFloat(targetProgress, "_HoverProgress", duration)
                .SetEase(easeType)
                .SetLink(gameObject);
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
        // Instantly reset visual state to avoid frozen hovered states when buttons are disabled/hidden
        ResetVisualsInstant();
        isHovered = false;
    }

    void OnDestroy()
    {
        transform.DOKill();
        if (instancedMaterial != null)
        {
            instancedMaterial.DOKill();
            // Clean up the instantiated material asset to prevent memory leaks in the editor
            Destroy(instancedMaterial);
        }
    }
}
