using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Parent object tempat kartu-kartu berada")]
    public Transform handContainer;

    [Tooltip("Kumpulan prefab kartu yang tersedia dalam game")]
    public GameObject[] cardPrefabs;

    [Tooltip("UI Text sisa deck")]
    public TextMeshProUGUI deckCountText;

    [Tooltip("Transform objek visual Deck (untuk animasi spawn kartu)")]
    public RectTransform deckTransform;

    [Header("Game Settings")]
    public int startingDeckSize = 20;

    [Header("Player Reference")]
    public PlayerMovement player;
    public EnemyMovement[] enemy;

    [Header("Animation & Timings")]
    [SerializeField] private float playStaggerDelay = 0.25f; // Jeda antar kartu mulai menghilang
    [SerializeField] private float cardShowDuration = 0.5f;  // Berapa lama kartu "pamer" sebelum hilang
    [SerializeField] private float drawDelayAfterPlay = 0.8f; // Tunggu berapa lama setelah kartu terakhir 'pamer' baru draw
    [SerializeField] private float drawStaggerDelay = 0.15f;  // Jeda antar kartu yang di-draw

    private List<GameObject> deckList = new List<GameObject>();
    private bool isPlayingSequence = false;

    // Permanent deck registry persisting across scene reloads
    public static List<string> permanentDeckNames = new List<string>();

    // Untuk fitur Copy
    private CardAction lastActionType;
    private int lastActionValue;
    private bool lastActionIsFlipped;
    private bool hasLastAction = false;

    void Start()
    {
        InitializeDeck();
        UpdateDeckText();

        // Start in-game BGM
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayIngameBGM();
    }

    void Update()
    {
        // Testing trigger for UI/reward integration
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[GameManager] Cheat key 'R' pressed. Loading RewardUI scene...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("RewardUI");
        }
    }

    public static void AddCardToPermanentDeck(string cardName)
    {
        if (permanentDeckNames == null)
        {
            permanentDeckNames = new List<string>();
        }
        permanentDeckNames.Add(cardName);
        Debug.Log($"[GameManager] Added {cardName} permanently to deck registry. Total: {permanentDeckNames.Count} cards.");
    }

    private void InitializeDeck()
    {
        deckList.Clear();
        
        if (permanentDeckNames != null && permanentDeckNames.Count > 0)
        {
            Debug.Log($"[GameManager] Loading deck from permanent deck registry ({permanentDeckNames.Count} cards).");
            foreach (var cardName in permanentDeckNames)
            {
                GameObject prefab = FindPrefabInArray(cardName);
                if (prefab != null)
                {
                    deckList.Add(prefab);
                }
            }
        }
        else
        {
            Debug.Log("[GameManager] Permanent deck registry is empty. Initializing a new random deck.");
            permanentDeckNames = new List<string>();
            for (int i = 0; i < startingDeckSize; i++)
            {
                if (cardPrefabs.Length > 0)
                {
                    int randomIndex = Random.Range(0, cardPrefabs.Length);
                    GameObject selectedPrefab = cardPrefabs[randomIndex];
                    deckList.Add(selectedPrefab);
                    permanentDeckNames.Add(selectedPrefab.name);
                }
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
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonClick);
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

        // LOOP 2: Hilang SATU PER SATU + Eksekusi Aksi
        for (int i = 0; i < selectedCards.Count; i++)
        {
            CardController card = selectedCards[i];
            
            // Eksekusi Aksi Player
            ExecuteCardAction(card);

            // Tunggu player selesai bergerak/berputar
            yield return new WaitUntil(() => player.IsMoving == false);

            // Hilangkan kartu
            card.PlayVanishPhase();
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

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonClick);

        CardController[] cardsInHand = handContainer.GetComponentsInChildren<CardController>();
        int count = 0;
        foreach (var c in cardsInHand)
        {
            if (c.IsSelected) 
            { 
                deckList.Add(FindPrefabInArray(c.gameObject.name));
                
                // SAFETY: Matikan semua animasi sebelum Destroy
                c.transform.DOKill();
                DOTween.Kill(c.gameObject);
                
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

        // Spawn di dalam handContainer (untuk ikut layout), tapi posisi awalnya di atas deck
        GameObject newCard = Instantiate(cardToDraw, handContainer);
        UpdateDeckText();

        // Jalankan animasi dari posisi deck ke posisi di tangan
        CardController cardCtrl = newCard.GetComponent<CardController>();
        if (cardCtrl != null && deckTransform != null)
        {
            // Konversi posisi dunia deck ke posisi lokal di dalam handContainer
            Vector2 deckLocalPos = handContainer is RectTransform handRT
                ? (Vector2)handRT.InverseTransformPoint(deckTransform.position)
                : Vector2.zero;

            cardCtrl.PlayDrawAnimation(deckLocalPos);
        }
    }

    private void ExecuteCardAction(CardController card)
    {
        if (player == null) return;

        CardAction action = card.actionType;
        int value = card.actionValue;
        bool flipped = card.IsFlipped;

        // Jika ini kartu Copy
        if (action == CardAction.Copy)
        {
            if (hasLastAction)
            {
                Debug.Log("[GameManager] Copying last action: " + lastActionType + " with value: " + lastActionValue);
                ApplyAction(lastActionType, lastActionValue, lastActionIsFlipped);
            }
            else
            {
                Debug.LogWarning("[GameManager] No last action to copy!");
            }
            return;
        }

        // Simpan sebagai aksi terakhir
        lastActionType = action;
        lastActionValue = value;
        lastActionIsFlipped = flipped;
        hasLastAction = true;

        ApplyAction(action, value, flipped);
    }

    private void ApplyAction(CardAction action, int value, bool flipped)
    {
        switch (action)
        {
            case CardAction.Move:
                player.Move(player.facingDirection, value);
                break;
            case CardAction.Dash:
                player.Move(player.facingDirection, value);
                break;
            case CardAction.Back:
                player.Move(-player.facingDirection, value);
                break;
            case CardAction.Rotate:
                // Original: Putar Kanan (1), Flipped: Putar Kiri (-1)
                player.RotatePlayer(flipped ? -1 : 1);
                break;
            case CardAction.Side:
                // Original: Geser Kanan Player, Flipped: Geser Kiri Player
                Vector2Int sideDir = player.GetSideDirection(!flipped);
                player.Move(sideDir, value);
                break;
        }
    }

    private void UpdateDeckText()
    {
        if (deckCountText != null) deckCountText.text = deckList.Count.ToString();
    }
}