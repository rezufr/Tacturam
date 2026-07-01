using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

public class CardRewardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Transform[] rewardSlots;
    [SerializeField] private TextMeshProUGUI[] descriptionTexts;

    [Header("Buttons")]
    [SerializeField] private Button nextStageButton;

    [Header("UI Highlight Style")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float selectScale = 1.15f;
    [SerializeField] private float tweenDuration = 0.25f;

    // ---------------- Card Pools ----------------

    [Header("Card Pools")]
    [Tooltip("Kartu-kartu yang dianggap sebagai 'kartu baru'. Tinggal drag prefab kartu ke sini.")]
    [SerializeField] private GameObject[] newCardPool;

    [Tooltip("Kartu-kartu yang dianggap sebagai 'kartu random'. Tinggal drag prefab kartu ke sini.")]
    [SerializeField] private GameObject[] randomCardPool;

    // ---------------- Reward Configuration ----------------

    [System.Serializable]
    public class StageRewardConfig
    {
        public int stageNumber;
        [Tooltip("Jumlah slot diambil dari New Card Pool")]
        public int newCardSlots;
        [Tooltip("Jumlah slot diambil dari Random Card Pool")]
        public int randomCardSlots;
    }

    [Header("Reward Rules per Stage")]
    [Tooltip("Atur kombinasi reward tiap stage. Kalau stage gak ada di list, pake Default Config.")]
    [SerializeField] private StageRewardConfig[] stageConfigs;

    [Tooltip("Dipake kalau stage sekarang gak ketemu di Stage Configs")]
    [SerializeField]
    private StageRewardConfig defaultConfig = new StageRewardConfig
    {
        stageNumber = -1,
        newCardSlots = 0,
        randomCardSlots = 3
    };

    [Tooltip("Stage sekarang. Otomatis di-overwrite dari GameManager.currentGameStage saat Start(). Cuma dipakai sebagai fallback/testing manual.")]
    [SerializeField] private int currentStage = 1;

    private GameObject[] instantiatedCards = new GameObject[3];
    private int selectedIndex = -1;

    void Start()
    {
        if (nextStageButton != null)
        {
            nextStageButton.interactable = false;
            nextStageButton.onClick.AddListener(OnNextStageClicked);
        }

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        // Ambil stage sekarang dari GameManager (persist lintas scene), bukan dari field manual
        currentStage = GameManager.currentGameStage;

        GenerateRewardChoices();
    }

    public void GenerateRewardChoices(int stageOverride = -1)
    {
        int stageToUse = stageOverride != -1 ? stageOverride : currentStage;

        StageRewardConfig config = GetConfigForStage(stageToUse);
        GameObject[] chosenPrefabs = BuildRewardSet(config);

        for (int i = 0; i < 3; i++)
        {
            if (i >= chosenPrefabs.Length || chosenPrefabs[i] == null)
            {
                if (rewardSlots != null && i < rewardSlots.Length && rewardSlots[i] != null)
                    rewardSlots[i].gameObject.SetActive(false);
                if (descriptionTexts != null && i < descriptionTexts.Length && descriptionTexts[i] != null)
                    descriptionTexts[i].gameObject.SetActive(false);
                continue;
            }

            GameObject cardPrefab = chosenPrefabs[i];
            Transform slot = (rewardSlots != null && i < rewardSlots.Length) ? rewardSlots[i] : null;

            if (slot == null) continue;

            GameObject cardObj = Instantiate(cardPrefab, slot);
            instantiatedCards[i] = cardObj;

            RectTransform cardRT = cardObj.GetComponent<RectTransform>();
            if (cardRT != null)
            {
                cardRT.anchorMin = new Vector2(0.5f, 0.5f);
                cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                cardRT.pivot = new Vector2(0.5f, 0.5f);
                cardRT.anchoredPosition = Vector2.zero;
                cardRT.sizeDelta = new Vector2(200f, 300f);
                cardRT.localScale = Vector3.one;
            }

            CardController cardCtrl = cardObj.GetComponent<CardController>();
            if (cardCtrl != null)
            {
                cardCtrl.enabled = false;

                if (cardCtrl.VisualTransform != null)
                {
                    cardCtrl.VisualTransform.sizeDelta = new Vector2(200f, 300f);
                }

                if (cardCtrl.CardImageUI != null)
                {
                    cardCtrl.CardImageUI.rectTransform.sizeDelta = new Vector2(200f, 300f);
                }
            }

            // Matikan Button yang ada di dalam prefab kartu (gak dipakai di reward screen)
            Button innerButton = cardObj.GetComponentInChildren<Button>();
            if (innerButton != null)
            {
                innerButton.gameObject.SetActive(false);
            }

            SetupSlotInteractions(cardObj, i);

            if (descriptionTexts != null && i < descriptionTexts.Length && descriptionTexts[i] != null)
            {
                if (cardCtrl != null && !string.IsNullOrEmpty(cardCtrl.cardDescription))
                {
                    descriptionTexts[i].text = cardCtrl.cardDescription;
                }
                else if (cardCtrl != null)
                {
                    descriptionTexts[i].text = GetDynamicDescription(cardCtrl);
                }
                else
                {
                    descriptionTexts[i].text = "Move two steps and ignore the negative tile.";
                }
            }
        }
    }

    private StageRewardConfig GetConfigForStage(int stage)
    {
        if (stageConfigs != null)
        {
            foreach (var cfg in stageConfigs)
            {
                if (cfg.stageNumber == stage)
                    return cfg;
            }
        }
        return defaultConfig;
    }

    private GameObject[] BuildRewardSet(StageRewardConfig config)
    {
        GameObject[] result = new GameObject[3];
        int idx = 0;

        List<GameObject> newPoolCopy = new List<GameObject>(newCardPool ?? new GameObject[0]);
        List<GameObject> randomPoolCopy = new List<GameObject>(randomCardPool ?? new GameObject[0]);

        // Ambil slot dari New Card Pool
        for (int i = 0; i < config.newCardSlots && idx < 3; i++)
        {
            GameObject picked = PickRandomAndRemove(newPoolCopy);
            if (picked != null)
            {
                result[idx] = picked;
                idx++;
            }
        }

        // Ambil slot dari Random Card Pool
        for (int i = 0; i < config.randomCardSlots && idx < 3; i++)
        {
            GameObject picked = PickRandomAndRemove(randomPoolCopy);
            if (picked != null)
            {
                result[idx] = picked;
                idx++;
            }
        }

        // Fallback kalau masih ada slot kosong (pool kehabisan kartu unik):
        // isi dari gabungan kedua pool, boleh duplikat antar slot kalau terpaksa
        while (idx < 3)
        {
            List<GameObject> fallbackPool = new List<GameObject>();
            if (newCardPool != null) fallbackPool.AddRange(newCardPool);
            if (randomCardPool != null) fallbackPool.AddRange(randomCardPool);

            if (fallbackPool.Count == 0) break; // beneran gak ada kartu sama sekali

            result[idx] = fallbackPool[Random.Range(0, fallbackPool.Count)];
            idx++;
        }

        return result;
    }

    private GameObject PickRandomAndRemove(List<GameObject> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        int randomIndex = Random.Range(0, pool.Count);
        GameObject picked = pool[randomIndex];
        pool.RemoveAt(randomIndex); // biar gak ke-pick lagi dalam reward yang sama
        return picked;
    }

    private void SetupSlotInteractions(GameObject cardObj, int index)
    {
        Button btn = cardObj.GetComponent<Button>();
        if (btn == null) btn = cardObj.AddComponent<Button>();

        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnCardClicked(index));

        EventTrigger trigger = cardObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = cardObj.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((data) => OnCardHover(index, true));
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((data) => OnCardHover(index, false));
        trigger.triggers.Add(entryExit);
    }

    private void OnCardHover(int index, bool enter)
    {
        if (selectedIndex == index) return;

        GameObject card = instantiatedCards[index];
        if (card == null) return;

        float targetScale = enter ? hoverScale : 1.0f;
        card.transform.DOKill();
        card.transform.DOScale(targetScale, tweenDuration).SetEase(Ease.OutQuad);
    }

    private void OnCardClicked(int index)
    {
        if (selectedIndex == index) return;

        if (selectedIndex != -1)
        {
            GameObject prevCard = instantiatedCards[selectedIndex];
            if (prevCard != null)
            {
                prevCard.transform.DOKill();
                prevCard.transform.DOScale(1.0f, tweenDuration).SetEase(Ease.OutQuad);
                SetCardAlpha(prevCard, 1.0f);
            }
        }

        selectedIndex = index;

        GameObject selected = instantiatedCards[selectedIndex];
        if (selected != null)
        {
            selected.transform.DOKill();
            selected.transform.DOScale(selectScale, tweenDuration).SetEase(Ease.OutBack);
            SetCardAlpha(selected, 1.0f);
        }

        for (int i = 0; i < instantiatedCards.Length; i++)
        {
            if (i != selectedIndex && instantiatedCards[i] != null)
            {
                instantiatedCards[i].transform.DOKill();
                instantiatedCards[i].transform.DOScale(0.9f, tweenDuration).SetEase(Ease.OutQuad);
                SetCardAlpha(instantiatedCards[i], 0.5f);
            }
        }

        if (nextStageButton != null)
        {
            nextStageButton.interactable = true;
        }
    }

    private void SetCardAlpha(GameObject cardObj, float alpha)
    {
        CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
        cg.DOKill();
        cg.DOFade(alpha, tweenDuration);
    }

    private void OnNextStageClicked()
    {
        if (selectedIndex == -1) return;

        // Simpen dulu kartu yang dipilih, TAPI belum pindah scene
        GameObject chosenCard = instantiatedCards[selectedIndex];
        if (chosenCard != null)
        {
            string cleanName = chosenCard.name.Replace("(Clone)", "").Trim();
            GameManager.AddCardToPermanentDeck(cleanName);
        }

        // Scene TIDAK langsung berpindah di sini.
        // Tombol arrow (ShipPathController) yang akan trigger perpindahan scene
        // setelah animasi kapal selesai jalan, lewat ConfirmCardSelectionAndProceed()
    }

    // Dipanggil dari ShipPathController setelah animasi kapal selesai
    public void ConfirmCardSelectionAndProceed()
    {
        GameManager.currentGameStage++;
        Debug.Log($"[CardRewardManager] Stage naik jadi {GameManager.currentGameStage}. Loading gameplay scene...");

        SceneManager.LoadScene("SampleScene");
    }

    private string GetDynamicDescription(CardController card)
    {
        switch (card.actionType)
        {
            case CardAction.Move:
                return "Move forward 1 tile.";
            case CardAction.Dash:
                return "Dash forward 2 tiles.";
            case CardAction.Back:
                return "Move backward 1 tile.";
            case CardAction.Rotate:
                return "Rotate your character's facing direction.";
            case CardAction.Side:
                return "Step sideways to the tile beside your character.";
            case CardAction.Copy:
                return "Copy the effect of the previously played card.";
            default:
                return "Move forward 1 tile.";
        }
    }
}