using UnityEngine;
using UnityEngine.Serialization;
using DG.Tweening;

public class SwingingMenuPanel : MonoBehaviour
{
    [Header("UI Canvas Hierarchy")]
    [FormerlySerializedAs("settingsCanvasObject")]
    [SerializeField] protected GameObject panelCanvasObject;    // Parent GameObject of this menu panel

    [Header("Dark Background Overlay")]
    [SerializeField] protected CanvasGroup overlayCanvasGroup;   // The dark overlay that fades in
    [SerializeField] protected float overlayMaxAlpha = 0.6f;     // Maximum opacity of dark background
    [SerializeField] protected float fadeDuration = 0.3f;        // Fade transition duration

    [Header("Sticky Note Panel")]
    [SerializeField] protected RectTransform stickyNotePanel;    // The sticky note UI panel
    [SerializeField] protected Vector2 pinPivot = new Vector2(0.15f, 0.85f); // Position of the pin (top-left)
    [SerializeField] protected bool autoAdjustPivot = true;       // Set true to adjust pivot automatically at Start

    [Header("Swing Animation (Axe / Pendulum)")]
    [SerializeField] protected float startRotationAngle = 75f;    // Initial rotation angle before swinging in
    [SerializeField] protected float swingDuration = 0.8f;        // Total duration of the entrance swing
    [SerializeField] protected Vector3 normalScale = Vector3.one;

    protected Sequence swingSequence;
    protected Tween fadeTween;
    protected bool isOpening = false;
    protected bool isClosing = false;

    protected virtual void Start()
    {
        // Adjust the pivot dynamically to ensure the rotation origin is exactly at the pin
        if (autoAdjustPivot && stickyNotePanel != null)
        {
            SetPivotWithoutMoving(stickyNotePanel, pinPivot);
        }

        // Initially hide the panel canvas
        if (panelCanvasObject != null)
        {
            panelCanvasObject.SetActive(false);
        }

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Opens the panel, fading in the overlay and swinging the sticky note down like an axe/pendulum
    /// </summary>
    public virtual void OpenPanel()
    {
        if (isOpening || isClosing) return;
        isOpening = true;

        // Clean up any active tweens/sequences
        KillActiveTweens();

        // Enable canvas objects
        if (panelCanvasObject != null)
        {
            panelCanvasObject.SetActive(true);
        }

        // 1. Fade In Dark Background Overlay
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.blocksRaycasts = true;
            fadeTween = overlayCanvasGroup.DOFade(overlayMaxAlpha, fadeDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(overlayCanvasGroup.gameObject);
        }

        // 2. Perform axe/pendulum swing animation on sticky note
        if (stickyNotePanel != null)
        {
            // Initial positions: rotated far up and scaled down
            stickyNotePanel.localRotation = Quaternion.Euler(0, 0, startRotationAngle);
            stickyNotePanel.localScale = Vector3.zero;

            swingSequence = DOTween.Sequence();

            // Swing 1: Initial drop (fast swing downwards, overshooting past center)
            float t1 = swingDuration * 0.40f;
            swingSequence.Append(stickyNotePanel.DOLocalRotate(new Vector3(0, 0, -16f), t1, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
            // Scale up quickly during the first phase of the swing
            swingSequence.Join(stickyNotePanel.DOScale(normalScale, t1).SetEase(Ease.OutQuad));

            // Swing 2: Swing backwards (rising back up to the other side)
            float t2 = swingDuration * 0.25f;
            swingSequence.Append(stickyNotePanel.DOLocalRotate(new Vector3(0, 0, 8f), t2).SetEase(Ease.OutQuad));

            // Swing 3: Swing forward again (smaller overshoot)
            float t3 = swingDuration * 0.20f;
            swingSequence.Append(stickyNotePanel.DOLocalRotate(new Vector3(0, 0, -4f), t3).SetEase(Ease.InOutQuad));

            // Swing 4: Settle to rest (final drift to equilibrium, 0 degrees)
            float t4 = swingDuration * 0.15f;
            swingSequence.Append(stickyNotePanel.DOLocalRotate(Vector3.zero, t4).SetEase(Ease.OutQuad));

            swingSequence.SetLink(stickyNotePanel.gameObject);
            swingSequence.OnComplete(() =>
            {
                isOpening = false;
            });
        }
        else
        {
            isOpening = false;
        }
    }

    /// <summary>
    /// Closes the panel, fading out the overlay and swinging the sticky note away
    /// </summary>
    public virtual void ClosePanel()
    {
        if (isOpening || isClosing) return;
        isClosing = true;

        KillActiveTweens();

        // 1. Fade Out Background Overlay
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.blocksRaycasts = false;
            fadeTween = overlayCanvasGroup.DOFade(0f, fadeDuration)
                .SetEase(Ease.InQuad)
                .SetLink(overlayCanvasGroup.gameObject);
        }

        // 2. Swing Away Sticky Note
        if (stickyNotePanel != null)
        {
            swingSequence = DOTween.Sequence();
            
            // Swing upwards matching start angle direction and scale down
            swingSequence.Append(stickyNotePanel.DOLocalRotate(new Vector3(0, 0, startRotationAngle), fadeDuration).SetEase(Ease.InQuad));
            swingSequence.Join(stickyNotePanel.DOScale(Vector3.zero, fadeDuration).SetEase(Ease.InQuad));
            
            swingSequence.SetLink(stickyNotePanel.gameObject);
            swingSequence.OnComplete(() =>
            {
                if (panelCanvasObject != null)
                {
                    panelCanvasObject.SetActive(false);
                }
                isClosing = false;
            });
        }
        else
        {
            if (panelCanvasObject != null)
            {
                panelCanvasObject.SetActive(false);
            }
            isClosing = false;
        }
    }

    protected void KillActiveTweens()
    {
        if (swingSequence != null && swingSequence.IsActive())
        {
            swingSequence.Kill();
        }

        if (fadeTween != null && fadeTween.IsActive())
        {
            fadeTween.Kill();
        }
    }

    /// <summary>
    /// Changes the pivot of a RectTransform without causing it to jump in position visually.
    /// This makes runtime pivot adjustments completely seamless.
    /// </summary>
    protected void SetPivotWithoutMoving(RectTransform rt, Vector2 newPivot)
    {
        if (rt == null) return;

        Vector2 size = rt.rect.size;
        Vector2 deltaPivot = rt.pivot - newPivot;
        Vector3 deltaPosition = new Vector3(
            deltaPivot.x * size.x * rt.localScale.x,
            deltaPivot.y * size.y * rt.localScale.y,
            0f
        );

        // Apply pivot and correct position offset
        rt.pivot = newPivot;
        rt.localPosition -= deltaPosition;
    }

    protected virtual void OnDestroy()
    {
        KillActiveTweens();
    }
}
