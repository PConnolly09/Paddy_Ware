using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class WordUIManager : MonoBehaviour
{
    public static WordUIManager Instance { get; private set; }

    [Header("Run HUD")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI firewallHpText;
    public TextMeshProUGUI queriesText;
    public TextMeshProUGUI dicePoolText;
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI activeRelicsText; // NEW: Live display for equipped relics!
    public TextMeshProUGUI damageLogText;

    [Header("Drafting Area - Physical Trays")]
    public Transform availableLettersContainer;
    public Transform selectedLettersContainer;
    public GameObject letterButtonPrefab;
    public TextMeshProUGUI liveScorePreviewText;
    public TextMeshProUGUI selectedDieInfoText;

    [Header("Menus & Overlays")]
    public GameObject burnedWordsPanel;
    public TextMeshProUGUI burnedWordsListText;
    public GameObject deckReviewPanel;
    public TextMeshProUGUI deckReviewText;

    [Header("Victory Summary")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryStatsText;
    public TextMeshProUGUI victoryButtonText;

    [Header("Defeat Summary")]
    public GameObject defeatPanel;
    public TextMeshProUGUI defeatStatsText;

    [Header("Shop UI")]
    public GameObject shopPanel;
    public TextMeshProUGUI shopTitleText;
    public TextMeshProUGUI shopBalanceText;
    public Transform shopItemContainer;
    public GameObject shopItemPrefab;
    private string _currentCurrencyLabel = "";

    private readonly List<GameObject> _allActiveDiceObjs = new();
    private readonly Dictionary<GameObject, DieData> _diceDataMap = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (burnedWordsPanel != null) burnedWordsPanel.SetActive(false);
        if (deckReviewPanel != null) deckReviewPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    public void ForceUpdateHUD()
    {
        if (RunManager.Instance != null && DiceDeck.Instance != null)
            UpdateRunStats(RunManager.Instance.currentLevel, RunManager.Instance.currentDamageDealt, RunManager.Instance.targetFirewallHP, RunManager.Instance.queriesRemaining, DiceDeck.Instance.currentDrawPile.Count, RunManager.Instance.currentCredits, RunManager.Instance.activeRelics);
    }

    public void UpdateRunStats(int level, double currentDamage, double targetHp, int queries, int diceInPool, int credits, List<Relic> relics)
    {
        if (levelText != null)
        {
            bool isBoss = RunManager.Instance != null && RunManager.Instance.isBossLevel;
            if (isBoss)
            {
                string bossDesc = RunManager.Instance.activeBossDescription;
                levelText.text = $"MAINFRAME LEVEL {level}\n<color=#FF0000>[BOSS: {bossDesc}]</color>";
            }
            else
            {
                levelText.text = $"MAINFRAME LEVEL {level}";
            }
        }

        if (queriesText != null) queriesText.text = $"QUERIES: {queries}";
        if (dicePoolText != null) dicePoolText.text = $"DICE POOL: {diceInPool}";
        if (creditsText != null) creditsText.text = $"<color=#FFD700>CREDITS: {credits:N0}</color>";

        if (firewallHpText != null)
        {
            double hpRemaining = System.Math.Max(0, targetHp - currentDamage);
            firewallHpText.text = $"FIREWALL HP: {hpRemaining:N0} / {targetHp:N0}";
        }

        // DYNAMIC RELIC TRACKER DISPLAY
        if (activeRelicsText != null)
        {
            bool isVirus = RunManager.Instance != null && RunManager.Instance.activeBossModifier == BossModifier.Virus;
            if (isVirus)
            {
                activeRelicsText.text = "RELICS: <color=#FF0000>DISABLED (VIRUS ENCOUNTER)</color>";
            }
            else if (relics.Count == 0)
            {
                activeRelicsText.text = "RELICS: None";
            }
            else
            {
                List<string> relicNames = new();
                foreach (Relic r in relics)
                {
                    if (RelicLibrary.Instance != null && RelicLibrary.Instance.AllRelics.ContainsKey(r))
                        relicNames.Add($"<color=#00FFFF>{RelicLibrary.Instance.AllRelics[r].Name}</color>");
                    else
                        relicNames.Add(r.ToString());
                }
                activeRelicsText.text = "RELICS: " + string.Join(", ", relicNames);
            }
        }
    }

    public void LogDamage(string word, string pos, string def, int baseScore, long hits, int tomeSize, double multiplier, double totalDamage, bool isNew, bool isFavorite, string triggeredRelics)
    {
        if (damageLogText != null)
        {
            string multiStr = multiplier > 1.0 ? $" <color=#00FF00>(x{multiplier:F1} Total Multiplier!)</color>" : "";
            string bannerStr = "";
            if (isNew) bannerStr = "<color=#FFFF00>*** NEW WORD DISCOVERED (+50% Dmg) ***</color>\n";
            else if (isFavorite) bannerStr = "<color=#FF8800>*** FAVORITE WORD (+20% Dmg, Extra Play) ***</color>\n";

            string relicStr = !string.IsNullOrEmpty(triggeredRelics) ? $"<color=#00FF00>Relics Triggered: [{triggeredRelics}]</color>\n" : "";

            damageLogText.text = $"{bannerStr}" +
                                 $"[<color=#FFD700>{word.ToUpper()}</color>]\n" +
                                 $"<color=#00FFFF>PART OF SPEECH: {pos}</color>\n" +
                                 $"<color=#AAAAAA><i>{def}</i></color>\n" +
                                 $"{relicStr}\n" +
                                 $"Base: {baseScore} | Global Hits: {hits:N0} | Tome: {tomeSize:N0}{multiStr}\n" +
                                 $"<color=#00FF00>DAMAGE DEALT: {totalDamage:N0}</color>";
        }
    }

    public void LogError(string message) { if (damageLogText != null) damageLogText.text = $"<color=#FF0000>[SYSTEM]</color> {message}"; }

    public void SpawnRolledLetters(List<DieData> hand)
    {
        ClearDraftingArea();
        foreach (DieData die in hand)
        {
            GameObject btnObj = Instantiate(letterButtonPrefab, availableLettersContainer);
            _allActiveDiceObjs.Add(btnObj);
            _diceDataMap[btnObj] = die;

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = $"{die.CurrentFace}\n<size=50%><color=#AAAAAA>({die.ScoreValue})</color></size>";

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                GameObject capturedBtn = btnObj;
                btn.onClick.AddListener(() => OnDiceClicked(capturedBtn));
            }
        }
    }

    private void OnDiceClicked(GameObject clickedDieObj)
    {
        if (clickedDieObj.transform.parent == availableLettersContainer) clickedDieObj.transform.SetParent(selectedLettersContainer);
        else clickedDieObj.transform.SetParent(availableLettersContainer);

        UpdateLivePreview();
        if (selectedDieInfoText != null && _diceDataMap.TryGetValue(clickedDieObj, out DieData dieInfo))
            selectedDieInfoText.text = $"Die Type: {dieInfo.Type}\nPossible Faces: {dieInfo.PossibleFaces}";
    }

    private void UpdateLivePreview()
    {
        string currentWord = "";
        bool isCipher = RunManager.Instance != null && RunManager.Instance.activeBossModifier == BossModifier.Cipher;

        foreach (Transform child in selectedLettersContainer) { if (_diceDataMap.TryGetValue(child.gameObject, out DieData die)) currentWord += die.CurrentFace; }

        if (liveScorePreviewText != null)
        {
            int currentBase = isCipher ? currentWord.Length : WordValidator.Instance.CalculateBaseScore(currentWord);
            string cipherWarning = isCipher ? " <color=#FF0000>[CIPHER]</color>" : "";
            liveScorePreviewText.text = string.IsNullOrEmpty(currentWord) ? "" : $"<color=#AAAAAA>(Base Score: {currentBase}){cipherWarning}</color>";
        }
    }

    public void OnClearButtonClicked()
    {
        // FIX: Iterating backwards ensures we don't skip elements while changing their hierarchy parent!
        for (int i = selectedLettersContainer.childCount - 1; i >= 0; i--)
        {
            selectedLettersContainer.GetChild(i).SetParent(availableLettersContainer);
        }

        UpdateLivePreview();
        if (selectedDieInfoText != null) selectedDieInfoText.text = "Select a die to inspect.";
    }

    public void OnSubmitButtonClicked()
    {
        string wordToSubmit = "";
        List<DieData> diceToSubmit = new();

        foreach (Transform child in selectedLettersContainer) { if (_diceDataMap.TryGetValue(child.gameObject, out DieData die)) { wordToSubmit += die.CurrentFace; diceToSubmit.Add(die); } }

        if (string.IsNullOrEmpty(wordToSubmit)) return;
        if (RunManager.Instance != null) RunManager.Instance.SubmitWord(wordToSubmit, diceToSubmit);
    }

    public void OnDiscardButtonClicked()
    {
        List<DieData> diceToDiscard = new();
        foreach (Transform child in selectedLettersContainer) { if (_diceDataMap.TryGetValue(child.gameObject, out DieData die)) diceToDiscard.Add(die); }

        if (diceToDiscard.Count == 0) return;
        if (RunManager.Instance != null) RunManager.Instance.DiscardSelectedLetters(diceToDiscard);
    }

    public void ClearDraftingArea()
    {
        foreach (GameObject btn in _allActiveDiceObjs) Destroy(btn);
        _allActiveDiceObjs.Clear();
        _diceDataMap.Clear();
        UpdateLivePreview();
        if (selectedDieInfoText != null) selectedDieInfoText.text = "";
    }

    public void OpenShopUI(string title, int balance, string currencyName, List<ShopItem> items)
    {
        if (shopPanel == null) return;
        shopPanel.SetActive(true);
        _currentCurrencyLabel = currencyName;
        if (shopTitleText != null) shopTitleText.text = title;
        UpdateShopBalance(balance);

        foreach (Transform child in shopItemContainer) Destroy(child.gameObject);

        foreach (ShopItem item in items)
        {
            GameObject itemObj = Instantiate(shopItemPrefab, shopItemContainer);
            TextMeshProUGUI[] texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 3)
            {
                texts[0].text = item.ItemName;
                texts[1].text = item.Description;
                texts[2].text = $"{item.Cost} {currencyName}";
            }

            Button btn = itemObj.GetComponentInChildren<Button>();
            if (btn != null)
            {
                ShopItem capturedItem = item;
                GameObject capturedBtn = btn.gameObject;
                btn.onClick.AddListener(() => ShopManager.Instance.TryBuyItem(capturedItem, capturedBtn));
            }
        }
    }

    public void UpdateShopBalance(int newBalance) { if (shopBalanceText != null) shopBalanceText.text = $"BALANCE: {newBalance:N0} {_currentCurrencyLabel}"; }

    public void MarkShopItemSold(GameObject buttonObj)
    {
        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null) btn.interactable = false;
        TextMeshProUGUI btnText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null) btnText.text = "SOLD OUT";
    }

    public void OnCloseShopButtonClicked()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (ShopManager.Instance != null && ShopManager.Instance.currentShopType == ShopType.PreRun) RunManager.Instance.StartNewRun();
        else RunManager.Instance.CloseShopAndAdvance();
    }

    public void ShowVictoryScreen(int words, int dice, int queriesLeft, double damageDealt, int credsEarned, bool isBoss)
    {
        if (victoryPanel == null || victoryStatsText == null) return;
        victoryPanel.SetActive(true);
        string bossTitle = isBoss ? "<color=#FF0000>BOSS NODE BREACHED</color>" : "<color=#00FF00>MAINFRAME BREACHED</color>";
        if (victoryButtonText != null) victoryButtonText.text = isBoss ? "ENTER TERMINAL SHOP" : "ADVANCE TO NEXT LEVEL";

        victoryStatsText.text = $"{bossTitle}\n\nTotal Damage: {damageDealt:N0} Bytes\nWords Compiled: {words}\nDice Expended: {dice}\nQueries Remaining: {queriesLeft}\n\n<color=#FFD700>REWARD: {credsEarned} CREDITS</color>";
    }

    public void ShowDefeatScreen(int level, int firewalls, int totalWords, string bestWord, double bestScore, string uniqueWord, long uniqueHits, string topLetters, string worstLetter, int dataCoresEarned)
    {
        if (defeatPanel == null || defeatStatsText == null) return;
        defeatPanel.SetActive(true);
        defeatStatsText.text = $"<color=#FF0000>SYSTEM COMPROMISED - RUN OVER</color>\n\n<color=#00FFFF>Run Summary</color>\nMainframe Reached: Level {level}\nFirewalls Breached: {firewalls}\nTotal Words Compiled: {totalWords}\n\n<color=#00FFFF>Vocabulary Diagnostics</color>\nHighest Scoring Word: <color=#FFD700>{bestWord}</color> ({bestScore:N0} Dmg)\nMost Unique Word: <color=#FFD700>{uniqueWord}</color> ({uniqueHits:N0} Global Hits)\n\n<color=#00FFFF>Dice Diagnostics</color>\nTop 5 Letters: {topLetters}\nLeast Common Letter: {worstLetter}\n\n<color=#FF00FF>META REWARD: {dataCoresEarned} DATA CORES EARNED</color>";
    }

    public void OnNextMainframeButtonClicked()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (RunManager.Instance != null) RunManager.Instance.AdvanceToNextLevel();
    }

    public void OnRestartRunButtonClicked()
    {
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (ShopManager.Instance != null) ShopManager.Instance.GeneratePreRunShop();
    }

    public void ToggleBurnedWordsPanel()
    {
        if (burnedWordsPanel == null) return;
        bool isActive = !burnedWordsPanel.activeSelf;
        burnedWordsPanel.SetActive(isActive);
        if (isActive && burnedWordsListText != null)
        {
            List<string> burned = WordValidator.Instance.GetBurnedWordsList();
            burnedWordsListText.text = burned.Count == 0 ? "No words burned yet." : string.Join("\n", burned);
        }
    }

    public void ToggleDeckReviewPanel()
    {
        if (deckReviewPanel == null) return;
        bool isActive = !deckReviewPanel.activeSelf;
        deckReviewPanel.SetActive(isActive);
        if (isActive && deckReviewText != null && DiceDeck.Instance != null) deckReviewText.text = DiceDeck.Instance.GetDeckSummary();
    }
}