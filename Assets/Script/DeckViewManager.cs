using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DeckViewManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject deckPanel;       // Panel Utama
    [SerializeField] private Transform contentParent;    // Object dengan Grid Layout Group
    [SerializeField] private GameObject cardItemPrefab;  // Prefab Thumbnail (Hanya Image)

    public void ToggleDeckView()
    {
        if (deckPanel == null) return;

        bool isOpen = !deckPanel.activeSelf;
        deckPanel.SetActive(isOpen);

        if (isOpen)
        {
            RefreshRealtimeView();
        }
    }

    /// <summary>
    /// Menampilkan SETIAP kartu yang ada di deck secara individual (Gaya Full Deck Balatro)
    /// </summary>
    public void RefreshRealtimeView()
    {
        if (gameManager == null || contentParent == null || cardItemPrefab == null) return;

        // 1. Bersihkan list lama
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. Ambil data deck asli
        List<GameObject> currentDeck = gameManager.GetCurrentDeck();

        // 3. Tampilkan SETIAP kartu (tanpa grouping)
        foreach (GameObject cardPrefab in currentDeck)
        {
            GameObject item = Instantiate(cardItemPrefab, contentParent);
            
            // Ambil script CardController dari prefab untuk dapet Sprite-nya
            CardController controller = cardPrefab.GetComponent<CardController>();
            Image itemImage = item.GetComponent<Image>();
            if (itemImage == null) itemImage = item.GetComponentInChildren<Image>();

            if (controller != null && itemImage != null)
            {
                // Ambil "Real Image" dari konfigurasi CardController
                itemImage.sprite = controller.GetOriginalSprite();
            }

            // Hilangkan teks jumlah jika ada (karena sekarang sistemnya 1 kartu = 1 thumbnail)
            TextMeshProUGUI countText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (countText != null) countText.text = ""; 
        }
    }
}
