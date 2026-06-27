using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("DOTween Animation Settings")]
    [SerializeField] private float hoverHeight = 40f;       // Tinggi kartu saat di-hover
    [SerializeField] private float selectHeight = 90f;      // Tinggi kartu saat diklik (Selected)
    [SerializeField] private float hoverScale = 1.05f;       // Skala pembesaran saat di-hover/klik
    [SerializeField] private float duration = 0.15f;        // Durasi animasi (detik)
    [SerializeField] private Ease easeType = Ease.OutQuad;  // Jenis transisi halus DOTween
    [SerializeField] private RectTransform visualTransform; // Object visual yang akan digerakkan (Child)

    [Header("Flip Settings (Card Sprite Swap)")]
    [SerializeField] private bool canFlip = false;          // Apakah kartu ini bisa di-flip
    [SerializeField] private UnityEngine.UI.Image cardImage;    // Komponen Image kartu
    [SerializeField] private Sprite cardOriginalSprite;    // Sprite awal (misal: Rotate Right)
    [SerializeField] private Sprite cardFlippedSprite;     // Sprite saat di-flip (misal: Rotate Left)

    [Header("Flip Settings (Button UI Swap)")]
    [SerializeField] private UnityEngine.UI.Image flipButtonImage; // Komponen Image pada tombol flip
    [SerializeField] private Sprite btnOriginalSprite;    // Sprite tombol awal (misal: Kuning)
    [SerializeField] private Sprite btnFlippedSprite;     // Sprite tombol saat di-flip (misal: Merah)
    [SerializeField] private RectTransform flipButtonTransform; // Transform tombol untuk pindah posisi
    [SerializeField] private Vector2 btnOriginalPos;      // Posisi tombol awal (misal: Kiri Atas)
    [SerializeField] private Vector2 btnFlippedPos;       // Posisi tombol saat di-flip (misal: Kanan Atas)

    private RectTransform rectTransform;

    // Karena kartu di dalam wrapper, posisi dasar Y selalu 0 relatif terhadap wrapper-nya
    private float startY = 0f;
    private bool isHovered = false;
    private bool isAnimatingFlip = false;

    public bool IsSelected { get; private set; } = false;
    public bool IsFlipped { get; private set; } = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // Jika visualTransform belum di-assign, pakai object ini sendiri sebagai peringatan/fallback
        if (visualTransform == null) visualTransform = rectTransform;

        // Memastikan Anchor berada di tengah bawah (Bottom Center) agar tarikan ke atas konsisten
        visualTransform.anchorMin = new Vector2(0.5f, 0f);
        visualTransform.anchorMax = new Vector2(0.5f, 0f);
        visualTransform.pivot = new Vector2(0.5f, 0f);

        // Reset posisi ke titik 0 wrapper saat mulai
        visualTransform.anchoredPosition = new Vector2(visualTransform.anchoredPosition.x, startY);

        // Inisialisasi tampilan awal
        UpdateVisualsInstant();
    }

    private void UpdateVisualsInstant()
    {
        if (cardImage != null && cardOriginalSprite != null && cardFlippedSprite != null)
            cardImage.sprite = IsFlipped ? cardFlippedSprite : cardOriginalSprite;

        if (flipButtonImage != null && btnOriginalSprite != null && btnFlippedSprite != null)
            flipButtonImage.sprite = IsFlipped ? btnFlippedSprite : btnOriginalSprite;

        if (flipButtonTransform != null)
            flipButtonTransform.anchoredPosition = IsFlipped ? btnFlippedPos : btnOriginalPos;
    }

    private void AnimateCard()
    {
        if (isAnimatingFlip) return;

        // 1. Hitung Target Posisi Y (Pasti akurat karena startY selalu 0)
        float targetY = startY;

        if (IsSelected)
        {
            targetY += selectHeight;
            if (isHovered) targetY += hoverHeight * 0.3f;
        }
        else if (isHovered)
        {
            targetY += hoverHeight;
        }

        // 2. Hitung Target Skala
        float targetScale = (isHovered || IsSelected) ? hoverScale : 1f;

        // 3. Eksekusi DOTween
        visualTransform.DOKill();

        visualTransform.DOAnchorPosY(targetY, duration).SetEase(easeType);
        visualTransform.DOScale(new Vector3(targetScale, targetScale, 1f), duration).SetEase(easeType);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        AnimateCard();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        AnimateCard();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            IsSelected = !IsSelected;
            AnimateCard();
        }
    }

    public void ResetCard()
    {
        IsSelected = false;
        isHovered = false;
        AnimateCard();
    }

    /// <summary>
    /// Fungsi untuk membalik (flip) kartu dan tombol secara dinamis
    /// </summary>
    public void ToggleFlip()
    {
        if (!canFlip || isAnimatingFlip) return;

        isAnimatingFlip = true;
        IsFlipped = !IsFlipped;
        
        visualTransform.DOKill();
        
        float currentTargetScale = (isHovered || IsSelected) ? hoverScale : 1f;

        // Animasi: Kecilkan lebar -> Ganti Sprite & Posisi Tombol -> Besarkan kembali
        Sequence flipSequence = DOTween.Sequence();
        
        flipSequence.Append(visualTransform.DOScaleX(0, duration).SetEase(Ease.InQuad));
        flipSequence.AppendCallback(() => {
            UpdateVisualsInstant(); // Tukar semua visual kartu + tombol di tengah animasi
        });
        flipSequence.Append(visualTransform.DOScaleX(currentTargetScale, duration).SetEase(Ease.OutQuad));
        flipSequence.OnComplete(() => {
            isAnimatingFlip = false;
        });
    }

    public bool IsDying { get; private set; } = false;

    /// <summary>
    /// Fase 1: Membuat kartu menonjol (Pop Up) bersama kartu lainnya yang dipilih
    /// </summary>
    public void PlayShowPhase()
    {
        if (IsDying) return;

        transform.DOKill();
        visualTransform.DOKill();
        
        Sequence seq = DOTween.Sequence();
        // Naik sedikit (80f) dan membesar (1.1x)
        seq.Append(visualTransform.DOAnchorPosY(80f, 0.25f).SetEase(Ease.OutBack));
        seq.Join(visualTransform.DOScale(1.1f, 0.25f).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// Fase 2: Menghilangkan kartu dari tangan satu per satu
    /// </summary>
    public void PlayVanishPhase()
    {
        if (IsDying) return;
        IsDying = true; // Langsung memicu pergeseran layout

        visualTransform.DOKill();
        Sequence seq = DOTween.Sequence();

        // Mengecil ke nol
        seq.Append(visualTransform.DOScale(0, 0.3f).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }

    /// <summary>
    /// Animasi saat kartu dimainkan: Terbang ke posisi target -> Tunggu -> Menghilang
    /// </summary>
    public void PlayPlayAnimation(Vector2 targetPos)
    {
        if (isAnimatingFlip) return;
        isAnimatingFlip = true; 

        // 1. Pindahkan ke Root Canvas agar bebas dari pengaruh layout dan terlihat paling depan
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
        }

        // 2. Kill semua animasi aktif (termasuk hover/hover height)
        visualTransform.DOKill();
        rectTransform.DOKill();

        Sequence playSeq = DOTween.Sequence();

        // 3. Animasi Card ROOT ke posisi target yang diberikan (misal: Center-Top tapi berjejer)
        playSeq.Append(rectTransform.DOAnchorPos(targetPos, 0.5f).SetEase(Ease.OutBack));
        playSeq.Join(transform.DOScale(1.2f, 0.5f).SetEase(Ease.OutBack));
        playSeq.Join(transform.DORotate(Vector3.zero, 0.5f)); 

        // 4. Tunggu 1 detik
        playSeq.AppendInterval(1f);

        // 5. Mengecil dan menghilang
        playSeq.Append(transform.DOScale(0, 0.3f).SetEase(Ease.InBack));
        
        // 6. Hancurkan objek setelah selesai
    }

    // Getter untuk kebutuhan Deck View
    public Sprite GetOriginalSprite() => cardOriginalSprite;
}
