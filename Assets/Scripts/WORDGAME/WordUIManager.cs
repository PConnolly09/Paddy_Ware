using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class WordUIManager : MonoBehaviour
{
    public static WordUIManager Instance { get; private set; }

    [Header("Run HUD & Progress")]
    public TextMeshProUGUI levelText;
    public Image levelProgressBar;
    public TextMeshProUGUI firewallHpText;
    public TextMeshProUGUI queriesText;
    public TextMeshProUGUI discardsText;
    public TextMeshProUGUI rerollsText;
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI activeRelicsText;
    public TextMeshProUGUI damageLogText;
    public TextMeshProUGUI transientMessageText;

    [Header("Drafting Area - FIXED SLOTS")]
    public Transform[] handSlots = new Transform[7];
    public Transform[] boardSlots = new Transform[7];
    public GameObject letterButtonPrefab;
    public TextMeshProUGUI selectedDieInfoText;
    public TextMeshProUGUI liveScorePreviewText;

    [Header("Dice Sprites")]
    public Sprite d4Sprite;
    public Sprite d6Sprite;
    public Sprite d8Sprite;
    public Sprite d20Sprite;

    [Header("Menus & Overlays")]
    public GameObject burnedWordsPanel;
    public TextMeshProUGUI burnedWordsListText;
    public GameObject deckReviewPanel;
    public TextMeshProUGUI deckReviewText;
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryStatsText;
    public TextMeshProUGUI victoryButtonText;
    public GameObject defeatPanel;
    public TextMeshProUGUI defeatStatsText;
    public GameObject shopPanel;
    public TextMeshProUGUI shopTitleText;
    public TextMeshProUGUI shopBalanceText;
    public Transform shopItemContainer;
    public GameObject shopItemPrefab;

    private string _currentCurrencyLabel = "";
    private class DieUIWrapper { public DieData Data; public Transform OriginHandSlot; }
    private readonly Dictionary<GameObject, DieUIWrapper> _activeDice = new();

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
        ClearTransient();
    }

    public void ForceUpdateHUD()
    {
        if (RunManager.Instance != null && RunManager.Instance.settings != null)
            UpdateRunStats(RunManager.Instance.currentLevel, RunManager.Instance.settings.maxLevel, RunManager.Instance.currentDamageDealt, RunManager.Instance.targetFirewallHP, RunManager.Instance.queriesRemaining, RunManager.Instance.discardsRemaining, RunManager.Instance.rerollsRemaining, RunManager.Instance.currentCredits, RunManager.Instance.activeRelics);
    }

    public void UpdateRunStats(int level, int maxLevel, double currentDamage, double targetHp, int queries, int discards, int rerolls, int credits, List<Relic> relics)
    {
        if (levelText != null)
        {
            string bossWarning = RunManager.Instance.isBossLevel ? $"\n<color=#FF0000>[BOSS: {RunManager.Instance.activeBossDescription}]</color>" : "";
            levelText.text = $"MAINFRAME {level} / {maxLevel}{bossWarning}";
        }

        if (levelProgressBar != null) levelProgressBar.fillAmount = (float)level / maxLevel;

        if (queriesText != null) queriesText.text = $"QUERIES: {queries}";
        if (discardsText != null) discardsText.text = $"DISCARDS: {discards}";
        if (rerollsText != null) rerollsText.text = $"REROLLS: {rerolls}";
        if (creditsText != null) creditsText.text = $"<color=#FFD700>CREDITS: {credits:N0}</color>";

        if (firewallHpText != null)
            firewallHpText.text = $"FIREWALL HP: {System.Math.Max(0, targetHp - currentDamage):N0} / {targetHp:N0}";

        if (activeRelicsText != null)
        {
            if (RunManager.Instance.activeBossModifier == BossModifier.Virus) activeRelicsText.text = "RELICS: <color=#FF0000>DISABLED (VIRUS ENCOUNTER)</color>";
            else if (relics.Count == 0) activeRelicsText.text = "RELICS: None";
            else
            {
                var names = relics.Select(r => RelicLibrary.Instance != null && RelicLibrary.Instance.AllRelics.ContainsKey(r) ? $"<color=#00FFFF>{RelicLibrary.Instance.AllRelics[r].Name}</color>" : r.ToString());
                activeRelicsText.text = "RELICS: " + string.Join(", ", names);
            }
        }
    }

    public void ShowTransientMessage(string msg)
    {
        if (transientMessageText != null)
        {
            transientMessageText.text = msg;
            CancelInvoke(nameof(ClearTransient));
            Invoke(nameof(ClearTransient), 2f);
        }
    }
    private void ClearTransient() { if (transientMessageText != null) transientMessageText.text = ""; }
    public void LogError(string message) { if (damageLogText != null) damageLogText.text = $"<color=#FF0000>[SYSTEM]</color> {message}"; }

    public IEnumerator AnimateScoringSequence(RunManager.ScoreBreakdown data)
    {
        if (damageLogText == null) yield break;

        string log = $"[<color=#FFD700>{data.word.ToUpper()}</color>]\n";
        log += $"<color=#00FFFF>POS: {data.pos}</color>\n";
        log += $"<color=#AAAAAA><i>{data.def}</i></color>\n\n";

        damageLogText.text = log;
        yield return new WaitForSeconds(0.3f);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 2.5f;
            int curBase = (int)Mathf.Lerp(0, data.baseScore, t);
            long curHits = (long)Mathf.Lerp(0, data.hits, t);
            int curTome = (int)Mathf.Lerp(0, data.tomeSize, t);
            damageLogText.text = log + $"Base: {curBase} | Hits: {curHits:N0} | Tome: {curTome:N0}\n";
            yield return null;
        }

        log += $"Base: {data.baseScore} | Hits: {data.hits:N0} | Tome: {data.tomeSize:N0}\n";
        damageLogText.text = log;
        yield return new WaitForSeconds(0.15f);

        if (data.lengthMult != 1.0f) { log += $"<color=#FF8800>Length Mult: x{data.lengthMult:F1}</color>\n"; damageLogText.text = log; yield return new WaitForSeconds(0.15f); }
        if (data.rarityMult != 1.0f) { log += $"<color=#FF8800>Rarity Mult: x{data.rarityMult:F1}</color>\n"; damageLogText.text = log; yield return new WaitForSeconds(0.15f); }

        foreach (string relic in data.relicLogs)
        {
            log += $"<color=#00FF00>{relic}</color>\n";
            damageLogText.text = log;
            yield return new WaitForSeconds(0.15f);
        }

        if (data.isNewWord) { log += $"<color=#FFFF00>*** NEW WORD (x1.5) ***</color>\n"; damageLogText.text = log; yield return new WaitForSeconds(0.2f); }
        if (data.isFavorite) { log += $"<color=#FF8800>*** FAVORITE (x1.2) ***</color>\n"; damageLogText.text = log; yield return new WaitForSeconds(0.2f); }
        if (data.isNewHighScore) { log += $"<color=#FF00FF>*** NEW HIGH SCORE! ***</color>\n"; damageLogText.text = log; yield return new WaitForSeconds(0.2f); }

        log += $"\n<color=#00FF00>DAMAGE: ";
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            double curDmg = data.totalDamage * t;
            damageLogText.text = log + $"{curDmg:N0}</color>";
            yield return null;
        }

        damageLogText.text = log + $"{data.totalDamage:N0}</color>";
        yield return new WaitForSeconds(0.5f);

        RunManager.Instance.ApplyCalculatedDamage(data.totalDamage);
    }

    // --- FIXED BOARD DRAFTING ---

    public void RefreshFixedBoard(List<DieData> currentHand)
    {
        ClearBoard();

        for (int i = 0; i < currentHand.Count && i < handSlots.Length; i++)
        {
            DieData die = currentHand[i];
            Transform targetSlot = handSlots[i];
            GameObject btnObj = Instantiate(letterButtonPrefab, targetSlot);

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = $"{die.CurrentFace}\n<size=50%><color=#AAAAAA>({die.ScoreValue})</color></size>";

            // --- SWAP DICE SPRITE HERE ---
            Image btnImage = btnObj.GetComponent<Image>();
            if (btnImage != null)
            {
                Sprite targetSprite = null;
                switch (die.Type)
                {
                    case DiceType.D4_Vowel: targetSprite = d4Sprite; break;
                    case DiceType.D6_Standard: targetSprite = d6Sprite; break;
                    case DiceType.D8_Consonant: targetSprite = d8Sprite; break;
                    case DiceType.D20_Rare: targetSprite = d20Sprite; break;
                }

                // Only override if you've assigned a custom sprite in the inspector
                if (targetSprite != null) btnImage.sprite = targetSprite;
            }

            _activeDice[btnObj] = new DieUIWrapper { Data = die, OriginHandSlot = targetSlot };

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
        if (RunManager.Instance != null && RunManager.Instance.currentState != RunState.Drafting) return;

        DieUIWrapper wrapper = _activeDice[clickedDieObj];
        if (clickedDieObj.transform.parent == wrapper.OriginHandSlot)
        {
            foreach (Transform slot in boardSlots)
            {
                if (slot.childCount == 0)
                {
                    clickedDieObj.transform.SetParent(slot, false);
                    break;
                }
            }
        }
        else clickedDieObj.transform.SetParent(wrapper.OriginHandSlot, false);

        UpdateLivePreview();
        if (selectedDieInfoText != null) selectedDieInfoText.text = $"Die Type: {wrapper.Data.Type}\nPossible Faces: {wrapper.Data.PossibleFaces}";
    }

    private void UpdateLivePreview()
    {
        string currentWord = "";
        bool isCipher = RunManager.Instance != null && RunManager.Instance.activeBossModifier == BossModifier.Cipher;

        foreach (Transform slot in boardSlots)
        {
            if (slot.childCount > 0)
            {
                GameObject dieObj = slot.GetChild(0).gameObject;
                if (_activeDice.TryGetValue(dieObj, out DieUIWrapper wrapper)) currentWord += wrapper.Data.CurrentFace;
            }
        }

        if (string.IsNullOrEmpty(currentWord)) { if (liveScorePreviewText != null) liveScorePreviewText.text = ""; return; }

        int currentBase = isCipher ? currentWord.Length : WordValidator.Instance.CalculateBaseScore(currentWord);
        string cipherWarning = isCipher ? " <color=#FF0000>[CIPHER]</color>" : "";

        List<string> predicted = new();
        if (RunManager.Instance != null && RunManager.Instance.activeBossModifier != BossModifier.Virus)
        {
            var relics = RunManager.Instance.activeRelics;
            string up = currentWord.ToUpper();

            if (relics.Contains(Relic.VowelBattery)) { int v = up.Count(c => "AEIOU".Contains(c)); if (v > 0) predicted.Add($"Vowel Battery (+{v * 2} Base)"); }
            if (relics.Contains(Relic.ConsonantCruncher))
            {
                int max = 0, cur = 0;
                foreach (char c in up) { if (!"AEIOU".Contains(c)) cur++; else { if (cur > max) max = cur; cur = 0; } }
                if (cur > max) max = cur;
                if (max >= 4) predicted.Add("Consonant Cruncher (x2.0)");
            }
            if (relics.Contains(Relic.DoubleVision) && System.Text.RegularExpressions.Regex.IsMatch(up, @"(.)\1")) predicted.Add("Double Vision (x2.0)");
            if (relics.Contains(Relic.QwertyVirus) && up.IndexOfAny(new char[] { 'Q', 'Z', 'J', 'X' }) >= 0) predicted.Add("QWERTY Virus (x3.0)");
            if (relics.Contains(Relic.ShortCircuit) && up.Length == 3) predicted.Add("Short Circuit (+5M Hits)");
            if (relics.Contains(Relic.FourLetterWord) && up.Length == 4) predicted.Add("Four-Letter Word (x3 Base)");
            if (relics.Contains(Relic.TheLongCon) && up.Length >= 7) predicted.Add("The Long Con");
            if (relics.Contains(Relic.Pluralizer) && up.EndsWith("S")) predicted.Add("Pluralizer (+2M Hits)");
            if (relics.Contains(Relic.GerundEngine) && up.EndsWith("ING")) predicted.Add("Gerund Engine (x1.5 Tome)");
            if (relics.Contains(Relic.PrefixProtocol) && (up.StartsWith("RE") || up.StartsWith("UN"))) predicted.Add("Prefix Protocol (x2 Base)");
            char[] arr = up.ToCharArray(); System.Array.Reverse(arr); string rev = new string(arr);
            if (relics.Contains(Relic.PalindromeProtocol) && up == rev && up.Length > 1) predicted.Add("Palindrome Protocol (x5.0)");
        }

        string predStr = predicted.Count > 0 ? $"\n<color=#00FF00>Expected: {string.Join(", ", predicted)}</color>" : "";

        double pb = LexiconSaveManager.Instance.GetWordHighestScore(currentWord);
        string pbStr = pb > 0 ? $" | <color=#FF00FF>PB: {pb:N0}</color>" : "";

        if (liveScorePreviewText != null)
            liveScorePreviewText.text = $"<color=#AAAAAA>(Base Score: {currentBase}{cipherWarning}{pbStr})</color>{predStr}";
    }

    public void OnSubmitButtonClicked()
    {
        if (RunManager.Instance != null && RunManager.Instance.currentState != RunState.Drafting) return;

        string wordToSubmit = "";
        List<DieData> diceToSubmit = new();

        foreach (Transform slot in boardSlots)
        {
            if (slot.childCount > 0)
            {
                GameObject dieObj = slot.GetChild(0).gameObject;
                if (_activeDice.TryGetValue(dieObj, out DieUIWrapper wrapper))
                {
                    wordToSubmit += wrapper.Data.CurrentFace;
                    diceToSubmit.Add(wrapper.Data);
                }
            }
        }

        if (string.IsNullOrEmpty(wordToSubmit)) return;
        RunManager.Instance.SubmitWord(wordToSubmit, diceToSubmit);
    }

    public void ReturnLettersToHand() { foreach (var kvp in _activeDice) kvp.Key.transform.SetParent(kvp.Value.OriginHandSlot, false); UpdateLivePreview(); }

    public void OnClearButtonClicked() { ReturnLettersToHand(); if (selectedDieInfoText != null) selectedDieInfoText.text = "Select a die to inspect."; }

    public void OnDiscardButtonClicked()
    {
        if (RunManager.Instance != null && RunManager.Instance.currentState != RunState.Drafting) return;
        foreach (Transform slot in boardSlots) { if (slot.childCount > 0) { RunManager.Instance.ProcessDiscard(_activeDice[slot.GetChild(0).gameObject].Data); return; } }
    }

    public void OnRerollButtonClicked()
    {
        if (RunManager.Instance != null && RunManager.Instance.currentState != RunState.Drafting) return;
        foreach (Transform slot in boardSlots) { if (slot.childCount > 0) { RunManager.Instance.ProcessReroll(_activeDice[slot.GetChild(0).gameObject].Data); return; } }
    }

    public void ClearBoard() { foreach (var kvp in _activeDice) Destroy(kvp.Key); _activeDice.Clear(); UpdateLivePreview(); }

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
            if (texts.Length >= 3) { texts[0].text = item.ItemName; texts[1].text = item.Description; texts[2].text = $"{item.Cost} {currencyName}"; }

            Button btn = itemObj.GetComponentInChildren<Button>();
            if (btn != null)
            {
                ShopItem capturedItem = item; GameObject capturedBtn = btn.gameObject;
                btn.onClick.AddListener(() => ShopManager.Instance.TryBuyItem(capturedItem, capturedBtn));
            }
        }
    }

    public void UpdateShopBalance(int newBalance) { if (shopBalanceText != null) shopBalanceText.text = $"BALANCE: {newBalance:N0} {_currentCurrencyLabel}"; }
    public void MarkShopItemSold(GameObject buttonObj) { Button btn = buttonObj.GetComponent<Button>(); if (btn != null) btn.interactable = false; TextMeshProUGUI btnText = buttonObj.GetComponentInChildren<TextMeshProUGUI>(); if (btnText != null) btnText.text = "SOLD OUT"; }
    public void OnCloseShopButtonClicked() { if (shopPanel != null) shopPanel.SetActive(false); if (ShopManager.Instance != null && ShopManager.Instance.currentShopType == ShopType.PreRun) RunManager.Instance.StartNewRun(); else RunManager.Instance.CloseShopAndAdvance(); }

    public void ShowBossVictoryScreen(double damageDealt, int credsEarned)
    {
        if (victoryPanel == null || victoryStatsText == null) return;
        victoryPanel.SetActive(true);
        if (victoryButtonText != null) victoryButtonText.text = "ENTER TERMINAL SHOP";
        victoryStatsText.text = $"<color=#FF0000>BOSS NODE BREACHED</color>\n\nTotal Damage: {damageDealt:N0} Bytes\n\n<color=#FFD700>REWARD: {credsEarned} CREDITS</color>";
    }

    public void ShowDefeatScreen(int level, int firewalls, int totalWords, string bestWord, double bestScore, string uniqueWord, long uniqueHits, string topLetters, string worstLetter, int dataCoresEarned)
    {
        if (defeatPanel == null || defeatStatsText == null) return;
        defeatPanel.SetActive(true);
        defeatStatsText.text = $"<color=#FF0000>SYSTEM COMPROMISED - RUN OVER</color>\n\n<color=#00FFFF>Run Summary</color>\nMainframe Reached: Level {level}\nFirewalls Breached: {firewalls}\nTotal Words Compiled: {totalWords}\n\n<color=#00FFFF>Vocabulary Diagnostics</color>\nHighest Scoring Word: <color=#FFD700>{bestWord}</color> ({bestScore:N0} Dmg)\nMost Unique Word: <color=#FFD700>{uniqueWord}</color> ({uniqueHits:N0} Global Hits)\n\n<color=#00FFFF>Dice Diagnostics</color>\nTop 5 Letters: {topLetters}\nLeast Common Letter: {worstLetter}\n\n<color=#FF00FF>META REWARD: {dataCoresEarned} DATA CORES EARNED</color>";
    }

    public void OnNextMainframeButtonClicked() { if (victoryPanel != null) victoryPanel.SetActive(false); if (RunManager.Instance != null) RunManager.Instance.AdvanceFromBossVictory(); }
    public void OnRestartRunButtonClicked() { if (defeatPanel != null) defeatPanel.SetActive(false); if (ShopManager.Instance != null) ShopManager.Instance.GeneratePreRunShop(); }

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