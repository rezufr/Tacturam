using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class ShipPathController : MonoBehaviour
{
    [Header("Ship")]
    [SerializeField] private RectTransform shipTransform;

    [Header("Scrolling")]
    [SerializeField] private RectTransform viewport;       // frame tetap
    [SerializeField] private RectTransform scrollContent;  // konten lebar yang digeser

    [Header("Path Waypoints (posisi LOCAL di dalam ScrollContent)")]
    [SerializeField] private RectTransform[] waypoints;

    [Header("Arrow Button")]
    [SerializeField] private Button arrowButton;

    [Header("Animation Settings")]
    [SerializeField] private float speedPerSegment = 0.6f;
    [SerializeField] private Ease easeType = Ease.InOutSine;
    [SerializeField] private bool rotateShipToDirection = true;

    [Header("After Arrival")]
    [SerializeField] private float delayBeforeSceneChange = 0.5f;
    [SerializeField] private string nextSceneName = "";  // Scene yang akan di-load ketika kapal tiba

    private bool isMoving = false;
    private float minScrollX;
    private float maxScrollX;

    private void Start()
    {
        if (arrowButton != null)
        {
            arrowButton.onClick.AddListener(OnArrowClicked);
        }

        if (shipTransform != null && waypoints.Length > 0)
        {
            shipTransform.anchoredPosition = waypoints[0].anchoredPosition;
        }

        CalculateScrollBounds();
    }

    private void CalculateScrollBounds()
    {
        // Batas scroll: 0 (paling kiri/awal) sampai selisih lebar content - viewport (paling kanan/akhir)
        float contentWidth = scrollContent.rect.width;
        float viewportWidth = viewport.rect.width;

        minScrollX = -(contentWidth - viewportWidth);
        maxScrollX = 0f;
    }

    private void OnArrowClicked()
    {
        if (isMoving) return;

        if (arrowButton != null) arrowButton.interactable = false;
        StartCoroutine(MoveShipAlongPath());
    }

    private IEnumerator MoveShipAlongPath()
    {
        isMoving = true;

        for (int i = 1; i < waypoints.Length; i++)
        {
            Vector2 targetShipPos = waypoints[i].anchoredPosition;

            if (rotateShipToDirection)
            {
                Vector2 direction = targetShipPos - shipTransform.anchoredPosition;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                shipTransform.DORotate(new Vector3(0, 0, angle), speedPerSegment * 0.3f);
            }

            // Gerakin kapal
            Tween shipTween = shipTransform.DOAnchorPos(targetShipPos, speedPerSegment).SetEase(easeType);

            // Bareng-bareng, gerakin ScrollContent supaya kapal tetep keliatan di viewport
            float targetScrollX = CalculateScrollXForShipPosition(targetShipPos.x);
            Tween scrollTween = scrollContent.DOAnchorPosX(targetScrollX, speedPerSegment).SetEase(easeType);

            yield return shipTween.WaitForCompletion();
        }

        isMoving = false;

        yield return new WaitForSeconds(delayBeforeSceneChange);

        OnShipArrived();
    }

    private float CalculateScrollXForShipPosition(float shipLocalX)
    {
        // Supaya kapal ada di TENGAH viewport, scroll content sejauh -(shipX - setengah lebar viewport)
        float viewportHalfWidth = viewport.rect.width / 2f;
        float targetScroll = -(shipLocalX - viewportHalfWidth);

        // Clamp biar gak scroll lewat batas kiri/kanan content
        targetScroll = Mathf.Clamp(targetScroll, minScrollX, maxScrollX);

        return targetScroll;
    }

    private void OnShipArrived()
    {
        // Load scene sesuai yang di-set di inspector
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // Fallback: jika tidak ada scene name, lanjut ke CardRewardManager
            CardRewardManager rewardManager = FindObjectOfType<CardRewardManager>();
            if (rewardManager != null)
            {
                rewardManager.ConfirmCardSelectionAndProceed();
            }
        }
    }
}