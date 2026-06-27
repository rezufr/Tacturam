using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

[ExecuteInEditMode]
public class HandLayoutManager : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] private float spacing = 100f;      // Jarak antar kartu
    [SerializeField] private float maxRotation = 15f;   // Maksimal rotasi di pinggir (derajat)
    [SerializeField] private float arcHeight = 30f;     // Tinggi lengkungan di tengah (busur)
    [SerializeField] private float verticalOffset = -50f; // Offset posisi Y dasar

    private int lastChildCount = -1;
    private int lastSiblingHash = -1;
    private float lastSpacing, lastMaxRot, lastArc, lastOffset;

    void Update()
    {
        int currentCount = 0;
        int currentSiblingHash = 0;
        int index = 0;
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                currentCount++;
                // Hash yang sensitif terhadap urutan (index)
                currentSiblingHash += (index + 1) * child.GetHashCode();
                index++;
            }
        }

        // Update layout jika jumlah anak BERUBAH atau URUTAN (Sibling Index) BERUBAH
        if (currentCount != lastChildCount || 
            currentSiblingHash != lastSiblingHash ||
            spacing != lastSpacing || 
            maxRotation != lastMaxRot || 
            arcHeight != lastArc || 
            verticalOffset != lastOffset)
        {
            UpdateLayout();
            lastChildCount = currentCount;
            lastSiblingHash = currentSiblingHash;
            lastSpacing = spacing; 
            lastMaxRot = maxRotation; 
            lastArc = arcHeight; 
            lastOffset = verticalOffset;
        }
    }

    private void UpdateLayout()
    {
        List<RectTransform> cards = new List<RectTransform>();
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf) cards.Add(child as RectTransform);
        }

        int count = cards.Count;
        if (count == 0) return;

        float currentSpacing = spacing;
        float startX = (count > 1) ? -(count - 1) * currentSpacing / 2f : 0f;

        for (int i = 0; i < count; i++)
        {
            RectTransform card = cards[i];
            if (card == null) continue;

            // CEK: Jika kartu sedang dimainkan (Dying) atau sedang di-drag, jangan dipindahkan posisinya oleh Layout
            CardController controller = card.GetComponent<CardController>();
            if (controller != null && (controller.IsDying || controller.IsDragging)) continue;

            card.pivot = new Vector2(0.5f, 0f);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);

            float ratio = (count > 1) ? (float)i / (count - 1) * 2f - 1f : 0f;
            float posX = startX + (i * currentSpacing);
            float posY = (1f - (ratio * ratio)) * arcHeight + verticalOffset;
            float rotZ = -ratio * maxRotation;

            if (Application.isPlaying)
            {
                DOTween.Kill(card); 
                card.DOAnchorPos(new Vector2(posX, posY), 0.4f).SetEase(Ease.OutBack).SetLink(card.gameObject);
                card.DORotate(new Vector3(0, 0, rotZ), 0.4f).SetEase(Ease.OutBack).SetLink(card.gameObject);
            }
            else
            {
                card.anchoredPosition = new Vector2(posX, posY);
                card.localRotation = Quaternion.Euler(0, 0, rotZ);
            }
        }
    }
}
