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
    [SerializeField] private GameManager gameManager;            // Fallback helper to find prefabs
    [SerializeField] private GameObject[] rewardPrefabs;          // Available card rewards in case gameManager is null
    [SerializeField] private Transform[] rewardSlots;            // Exactly 3 slots (Left page)
    [SerializeField] private TextMeshProUGUI[] descriptionTexts; // Exactly 3 descriptions (sticky notes)
    
    [Header("Buttons")]
    [SerializeField] private Button nextStageButton;

    [Header("UI Highlight Style")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float selectScale = 1.15f;
    [SerializeField] private float tweenDuration = 0.25f;

    private GameObject[] instantiatedCards = new GameObject[3];
    private int selectedIndex = -1;

    void Start()
    {
        // Interaksi awal
        if (nextStageButton != null)
        {
            nextStageButton.interactable = false;
            nextStageButton.onClick.AddListener(OnNextStageClicked);
        }

        // Cari GameManager jika belum di-assign
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        GenerateRewardChoices();
    }

    private void GenerateRewardChoices()
    {
        // Tarik daftar prefab kartu dari GameManager jika tersedia
        GameObject[] availableCards = rewardPrefabs;
        if (gameManager != null && gameManager.cardPrefabs != null && gameManager.cardPrefabs.Length > 0)
        {
            availableCards = gameManager.cardPrefabs;
        }

        if (availableCards == null || availableCards.Length == 0)
        {
            Debug.LogError("CardRewardManager: Tidak ada prefab kartu yang tersedia!");
            return;
        }

        // Memilih 3 index prefab acak yang unik
        List<int> chosenIndices = new List<int>();
        int maxRewards = Mathf.Min(3, availableCards.Length);
        
        while (chosenIndices.Count < maxRewards)
        {
            int rIdx = Random.Range(0, availableCards.Length);
            if (!chosenIndices.Contains(rIdx))
            {
                chosenIndices.Add(rIdx);
            }
        }

        // Spawn kartu di 3 slot UI
        for (int i = 0; i < 3; i++)
        {
            if (i >= chosenIndices.Count)
            {
                if (rewardSlots != null && i < rewardSlots.Length && rewardSlots[i] != null) 
                    rewardSlots[i].gameObject.SetActive(false);
                if (descriptionTexts != null && i < descriptionTexts.Length && descriptionTexts[i] != null) 
                    descriptionTexts[i].gameObject.SetActive(false);
                continue;
            }

            GameObject cardPrefab = availableCards[chosenIndices[i]];
            Transform slot = (rewardSlots != null && i < rewardSlots.Length) ? rewardSlots[i] : null;
            
            if (slot == null) continue;

            // Spawn Card Object inside the Slot
            GameObject cardObj = Instantiate(cardPrefab, slot);
            instantiatedCards[i] = cardObj;

            // Sesuaikan Anchoring, Posisi, dan Ukuran ke 250x400
            RectTransform cardRT = cardObj.GetComponent<RectTransform>();
            if (cardRT != null)
            {
                cardRT.anchorMin = new Vector2(0.5f, 0.5f);
                cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                cardRT.pivot = new Vector2(0.5f, 0.5f);
                cardRT.anchoredPosition = Vector2.zero;
                cardRT.sizeDelta = new Vector2(250f, 400f); // Ukuran container kartu
                cardRT.localScale = Vector3.one;
            }

            // Matikan script controller asli agar tidak bentrok dengan logika drag/flip biasa
            CardController cardCtrl = cardObj.GetComponent<CardController>();
            if (cardCtrl != null)
            {
                cardCtrl.enabled = false;

                // Samakan ukuran visual container utama objek kartu
                if (cardCtrl.VisualTransform != null)
                {
                    cardCtrl.VisualTransform.sizeDelta = new Vector2(250f, 400f);
                }

                // Samakan ukuran image utama kartu
                if (cardCtrl.CardImageUI != null)
                {
                    cardCtrl.CardImageUI.rectTransform.sizeDelta = new Vector2(250f, 400f);
                }
            }

            // Bind logik klik dan event hover
            SetupSlotInteractions(cardObj, i);

            // Set Deskripsi sticky note
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

    private void SetupSlotInteractions(GameObject cardObj, int index)
    {
        // Tambahkan Button untuk intercept klik pada kartu
        Button btn = cardObj.GetComponent<Button>();
        if (btn == null) btn = cardObj.AddComponent<Button>();

        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnCardClicked(index));

        // Tambahkan EventTrigger untuk Hover feedback
        EventTrigger trigger = cardObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = cardObj.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        // Pointer Enter
        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((data) => OnCardHover(index, true));
        trigger.triggers.Add(entryEnter);

        // Pointer Exit
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

        // Reset visual kartu yang sebelumnya dipilih
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

        // Visual feedback untuk kartu yang diklik
        GameObject selected = instantiatedCards[selectedIndex];
        if (selected != null)
        {
            selected.transform.DOKill();
            selected.transform.DOScale(selectScale, tweenDuration).SetEase(Ease.OutBack);
            SetCardAlpha(selected, 1.0f);
        }

        // Dim (redupkan) kartu-kartu lainnya
        for (int i = 0; i < instantiatedCards.Length; i++)
        {
            if (i != selectedIndex && instantiatedCards[i] != null)
            {
                instantiatedCards[i].transform.DOKill();
                instantiatedCards[i].transform.DOScale(0.9f, tweenDuration).SetEase(Ease.OutQuad);
                SetCardAlpha(instantiatedCards[i], 0.5f);
            }
        }

        // Aktifkan tombol Next Stage
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

        GameObject chosenCard = instantiatedCards[selectedIndex];
        if (chosenCard != null)
        {
            // Ambil nama prefab asli
            string cleanName = chosenCard.name.Replace("(Clone)", "").Trim();
            GameManager.AddCardToPermanentDeck(cleanName);
        }

        // Load kembali scene permainan
        SceneManager.LoadScene("SampleScene");
    }

    private string GetDynamicDescription(CardController card)
    {
        switch (card.actionType)
        {
            case CardAction.Move:
                return $"Move {card.actionValue} step{(card.actionValue > 1 ? "s" : "")} forward.";
            case CardAction.Dash:
                return $"Dash forward by {card.actionValue} tiles.";
            case CardAction.Back:
                return $"Move backward by {card.actionValue} tiles.";
            case CardAction.Rotate:
                return card.IsFlipped ? "Rotate 90 degrees counter-clockwise." : "Rotate 90 degrees clockwise.";
            case CardAction.Side:
                return card.IsFlipped ? "Slide 1 tile to the left." : "Slide 1 tile to the right.";
            case CardAction.Copy:
                return "Duplicate the effect of your previous action.";
            default:
                return "Move two steps and ignore the negative tile.";
        }
    }
}
