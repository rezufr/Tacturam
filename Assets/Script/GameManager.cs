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
    public Canvas rewardCanvas;

    [Tooltip("Kumpulan prefab kartu yang tersedia dalam game")]
    public GameObject[] cardPrefabs;

    [Tooltip("UI Text sisa deck")]
    public TextMeshProUGUI deckCountText;

    [Tooltip("Transform objek visual Deck (untuk animasi spawn kartu)")]
    public RectTransform deckTransform;

    [Header("Game Settings")]
    public int startingDeckSize = 20;
    public bool isPlayerTurn = true;

    [Header("Player Reference")]
    public PlayerMovement player;
    public EnemyMovement[] enemy;
    public TilemapController tilemapController;

    [Header("Animation & Timings")]
    [SerializeField] private float playStaggerDelay = 0.25f; // Jeda antar kartu mulai menghilang
    [SerializeField] private float cardShowDuration = 0.5f;  // Berapa lama kartu "pamer" sebelum hilang
    [SerializeField] private float drawDelayAfterPlay = 0.8f; // Tunggu berapa lama setelah kartu terakhir 'pamer' baru draw
    [SerializeField] private float drawStaggerDelay = 0.15f;  // Jeda antar kartu yang di-draw
    [SerializeField] private float enemyTelegraphDelay = 0.4f;

    [SerializeField] private List<GameObject> deckList = new List<GameObject>();
    [SerializeField] private List<GameObject> takenCards = new List<GameObject>();
    private bool isPlayingSequence = false;
    private int lastTelegraphPreviewHash = int.MinValue;

    // Permanent deck registry persisting across scene reloads
    public static List<string> permanentDeckNames = new List<string>();
    public static List<string> takenCardNames = new List<string>();

    // Stage tracker, persist selama aplikasi jalan (dipakai CardRewardManager)
    public static int currentGameStage = 1;

    // Untuk fitur Copy
    private CardAction lastActionType;
    private int lastActionValue;
    private bool lastActionIsFlipped;
    private bool hasLastAction = false;

    void Start()
    {
        InitializeDeck();
        UpdateDeckText();
        ResolveTilemapController();
        RefreshTelegraphPreview(true);
        ShowEnemyTelegraphsForPlayerTurn();
    }

    void Update()
    {
        RefreshTelegraphPreview();

        // Testing trigger for UI/reward integration
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log($"[GameManager] Cheat key 'R' pressed. Loading RewardUI scene for stage {currentGameStage}...");
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
        // PENTING: kosongkan dulu deckList (yang mungkin berisi data default dari Inspector),
        // supaya tidak menumpuk/dobel setiap kali scene di-reload.
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
            Debug.Log("[GameManager] Permanent deck registry is empty. Initializing a default deck.");
            // Karena deckList sudah di-Clear() di atas, kita butuh sumber default lain.
            // Gunakan cardPrefabs sebagai starting deck jika permanentDeckNames masih kosong
            // (misal ini adalah run pertama kali game dijalankan).
            permanentDeckNames = new List<string>();

            if (cardPrefabs != null && cardPrefabs.Length > 0)
            {
                int countToAdd = Mathf.Min(startingDeckSize, cardPrefabs.Length);
                for (int i = 0; i < countToAdd; i++)
                {
                    GameObject prefab = cardPrefabs[i % cardPrefabs.Length];
                    deckList.Add(prefab);
                    permanentDeckNames.Add(prefab.name);
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

    public void SetEnemyMove(int index)
    {
        enemy[index].SetMove(EnemyState.Moving);
    }

    public void SetEnemyAttacking(int index)
    {
        enemy[index].SetMove(EnemyState.Attacking);
    }

    public void SetEnemyRotate(int index)
    {
        print("[GameManager] Enemy rotate action triggered.");

        enemy[index].SetMove(EnemyState.Rotating);
    }

    public void PlayEnemy()
    {
        isPlayerTurn = false; // Set turn ke musuh
        if (tilemapController != null)
        {
            tilemapController.ClearEnemyTelegraphPreview();
        }
        StartCoroutine(EnemySequence());
    }

    IEnumerator EnemySequence()
    {
        for (int i = 0; i < enemy.Length; i++)
        {
            if (enemy[i] == null)
                continue;

            if (enemy[i].enemyType == EnemyType.Groom)
            {
                SetEnemyAttacking(i);
            }
            else
            {
                SetEnemyMove(i);
            }

            yield return new WaitUntil(() => !enemy[i].IsMoving);
        }

        ShowEnemyTelegraphsForPlayerTurn();
        print("Semua enemy selesai.");
        isPlayerTurn = true; // Set turn ke player
    }

    public void DiscardCardInHand(int amount)
    {
        CardController[] allCards = handContainer.GetComponentsInChildren<CardController>();

        for (int i = 0; i < amount; i++) // Loop sebanyak jumlah kartu yang ingin dibuang
        {
            for (int j = 0; j < allCards.Length; j++) // gimana cara ngambil yang terakhir aja? biar ga semua diambil
            {
                if (j == 0)
                {
                    GameObject prefab = FindPrefabInArray(allCards[j].gameObject.name);
                    takenCards.Add(prefab); // Ambil kartu terakhir di tangan
                    takenCardNames.Add(allCards[j].gameObject.name); // Tambahkan nama kartu ke daftar kartu yang diambil
                    allCards[j].PlayVanishPhase(); // Mainkan animasi hilang
                }
            }
        }
    }

    public void DiscardCardPermanently(int amount)
    {
        CardController[] allCards = handContainer.GetComponentsInChildren<CardController>();

        for (int i = 0; i < amount; i++) // Loop sebanyak jumlah kartu yang ingin dibuang
        {
            if (deckList.Count > 0)
            {
                print($"Discarding card permanently. Deck size before: {deckList.Count}");
                deckList.RemoveAt(deckList.Count - 1); // Hapus kartu terakhir dari deck
                UpdateDeckText();
            }
            else
            {
                for (int j = 0; j < allCards.Length; j++) // gimana cara ngambil yang terakhir aja? biar ga semua diambil
                {
                    if (j == allCards.Length - 1) // Ambil kartu terakhir di tangan
                    {
                        allCards[j].PlayVanishPhase(); // Mainkan animasi hilang
                    }
                }
            }

        }
    }

    public List<GameObject> GetCurrentDeck() { return deckList; }

    public void OnPlayButtonClicked()
    {
        if (tilemapController != null)
        {
            tilemapController.ClearTelegraphPreview();
        }

        lastTelegraphPreviewHash = int.MinValue;
        StartCoroutine(PlayCardsSequence());
    }

    public void OnPlayerReachFinish()
    {
        Debug.Log("[GameManager] Player reached the finish tile! Loading RewardUI scene...");
        rewardCanvas.gameObject.SetActive(true);
    }

    private IEnumerator PlayCardsSequence()
    {
        if (isPlayingSequence || handContainer == null) yield break;

        if (tilemapController != null)
        {
            tilemapController.ClearTelegraphPreview();
        }

        lastTelegraphPreviewHash = int.MinValue;

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
        PlayEnemy(); // Jalankan aksi musuh setelah player selesai main kartu
        yield return new WaitUntil(() => isPlayerTurn); // Tunggu sebentar sebelum menggambar kartu baru
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

    public void DrawTakenCards()
    {
        if (takenCards.Count == 0) return;
        GameObject cardToDraw = takenCards[0].gameObject;
        takenCards.RemoveAt(0);
        deckList.Add(cardToDraw);

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

    private void ResolveTilemapController()
    {
        if (tilemapController != null)
            return;

        if (player != null && player.tilemapController != null)
        {
            tilemapController = player.tilemapController;
            return;
        }

        tilemapController = FindObjectOfType<TilemapController>();
    }

    private void RefreshTelegraphPreview(bool forceRefresh = false)
    {
        if (!isPlayerTurn)
            return;

        ResolveTilemapController();

        if (tilemapController == null || player == null || handContainer == null || tilemapController.gridTilemap == null)
        {
            if (tilemapController != null)
            {
                tilemapController.ClearTelegraphPreview();
            }

            lastTelegraphPreviewHash = int.MinValue;
            return;
        }

        if (isPlayingSequence)
        {
            tilemapController.ClearTelegraphPreview();
            lastTelegraphPreviewHash = int.MinValue;
            return;
        }

        CardController[] allCards = handContainer.GetComponentsInChildren<CardController>();
        List<CardController> selectedCards = new List<CardController>();

        int previewHash = 17;
        for (int i = 0; i < allCards.Length; i++)
        {
            CardController card = allCards[i];
            if (card == null || !card.IsSelected)
                continue;

            selectedCards.Add(card);

            unchecked
            {
                previewHash = previewHash * 31 + card.actionType.GetHashCode();
                previewHash = previewHash * 31 + card.actionValue;
                previewHash = previewHash * 31 + (card.IsFlipped ? 1 : 0);
                previewHash = previewHash * 31 + card.transform.GetSiblingIndex();
            }
        }

        if (!forceRefresh && previewHash == lastTelegraphPreviewHash)
            return;

        lastTelegraphPreviewHash = previewHash;

        if (selectedCards.Count == 0)
        {
            tilemapController.ClearTelegraphPreview();
            return;
        }

        Vector3Int startGridPos = tilemapController.gridTilemap.WorldToCell(player.transform.position);
        tilemapController.BuildTelegraphPreview(startGridPos, player.facingDirection, selectedCards);
    }

    private void ShowEnemyTelegraphsForPlayerTurn()
    {
        ResolveTilemapController();

        if (tilemapController == null || enemy == null)
            return;

        tilemapController.BuildAllEnemyTelegraphPreview(enemy);
    }

    private void PreviewAction(CardAction action, int value, bool flipped)
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