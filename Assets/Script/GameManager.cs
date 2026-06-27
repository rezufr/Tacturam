using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Parent object tempat kartu-kartu berada")]
    public Transform handContainer;

    [Tooltip("Kumpulan prefab kartu yang tersedia dalam game")]
    public GameObject[] cardPrefabs;

    [Tooltip("UI Text sisa deck")]
    public TextMeshProUGUI deckCountText;

    [Header("Game Settings")]
    public int startingDeckSize = 20;

    [Header("Animation & Timings")]
    [SerializeField] private float playStaggerDelay = 0.25f; // Jeda antar kartu mulai menghilang
    [SerializeField] private float cardShowDuration = 0.5f;  // Berapa lama kartu "pamer" sebelum hilang
    [SerializeField] private float drawDelayAfterPlay = 0.8f; // Tunggu berapa lama setelah kartu terakhir 'pamer' baru draw
    [SerializeField] private float drawStaggerDelay = 0.15f;  // Jeda antar kartu yang di-draw

    private List<GameObject> deckList = new List<GameObject>();
    private bool isPlayingSequence = false;

    void Start()
    {
        InitializeDeck();
        UpdateDeckText();
    }

    private void InitializeDeck()
    {
        deckList.Clear();
        for (int i = 0; i < startingDeckSize; i++)
        {
            if (cardPrefabs.Length > 0)
            {
                int randomIndex = Random.Range(0, cardPrefabs.Length);
                deckList.Add(cardPrefabs[randomIndex]);
            }
        }
        ShuffleDeck();
    }

    public void ShuffleDeck()
    {
        for (int i = 0; i < deckList.Count; i++)
        {
            GameObject temp = deckList[i];
            int randomIndex = Random.Range(i, deckList.Count);
            deckList[i] = deckList[randomIndex];
            deckList[randomIndex] = temp;
        }
    }

    public List<GameObject> GetCurrentDeck() { return deckList; }

    public void OnPlayButtonClicked()
    {
        StartCoroutine(PlayCardsSequence());
    }

    private IEnumerator PlayCardsSequence()
    {
        if (isPlayingSequence || handContainer == null) yield break;

        CardController[] allCards = handContainer.GetComponentsInChildren<CardController>();
        List<CardController> selectedCards = new List<CardController>();

        foreach (var card in allCards)
        {
            if (card.IsSelected) selectedCards.Add(card);
        }

        isPlayingSequence = true;
        Debug.Log($"[Play] Combo Show: {selectedCards.Count} kartu.");

        // LOOP 1: Pop Up SEMUA kartu pilihan (hampir bersamaan)
        for (int i = 0; i < selectedCards.Count; i++)
        {
            selectedCards[i].PlayShowPhase();
        }

        // TUNGGU: Agar player bisa melihat kartu apa saja yang dia mainkan
        yield return new WaitForSeconds(cardShowDuration);

        // LOOP 2: Hilang SATU PER SATU
        for (int i = 0; i < selectedCards.Count; i++)
        {
            selectedCards[i].PlayVanishPhase();
            yield return new WaitForSeconds(playStaggerDelay);
        }

        // Tunggu sebentar saja baru mulai draw kartu baru
        yield return new WaitForSeconds(drawDelayAfterPlay);
        StartCoroutine(DrawCardsSequence(selectedCards.Count, 0f));
        
        isPlayingSequence = false;
    }

    public void OnRecycleButtonClicked()
    {
        if (isPlayingSequence) return;
        
        CardController[] cardsInHand = handContainer.GetComponentsInChildren<CardController>();
        int count = 0;
        foreach (var c in cardsInHand)
        {
            if (c.IsSelected) 
            { 
                deckList.Add(FindPrefabInArray(c.gameObject.name));
                Destroy(c.gameObject); 
                count++; 
            }
        }

        if (count > 0)
        {
            UpdateDeckText();
            StartCoroutine(DrawCardsSequence(count, 0.5f));
        }
    }

    private GameObject FindPrefabInArray(string name)
    {
        string cleanName = name.Replace("(Clone)", "").Trim();
        foreach (var prefab in cardPrefabs)
        {
            if (prefab.name == cleanName) return prefab;
        }
        return cardPrefabs[0];
    }

    private IEnumerator DrawCardsSequence(int amountToDraw, float startDelay)
    {
        yield return new WaitForSeconds(startDelay);

        for (int i = 0; i < amountToDraw; i++)
        {
            if (deckList.Count > 0)
            {
                DrawOneCard();
                yield return new WaitForSeconds(drawStaggerDelay);
            }
            else break;
        }
    }

    private void DrawOneCard()
    {
        if (deckList.Count == 0) return;
        GameObject cardToDraw = deckList[0];
        deckList.RemoveAt(0);
        Instantiate(cardToDraw, handContainer);
        UpdateDeckText();
    }

    private void UpdateDeckText()
    {
        if (deckCountText != null) deckCountText.text = deckList.Count.ToString();
    }
}