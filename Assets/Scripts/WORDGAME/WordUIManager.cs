using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class WordUIManager : MonoBehaviour
{
    public static WordUIManager Instance { get; private set; }

    [Header("Combat HUD (Left Panel)")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI bossModifierText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI queriesText;
    public TextMeshProUGUI discardsText;
    public TextMeshProUGUI rerollsText;
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI activeRelicsText;
    public TextMeshProUGUI tomeTrackerText;
    public Image hpFillBar;

    [Header("Tactile Dice Board")]
    public Transform handContainer;
    public Transform wordContainer;
    private List<DieUI> _activeDiceOnBoard = new List<DieUI>();

    [Header("Transient Messages")]
    public TextMeshProUGUI transientMessageText;

    [Header("Scoring Dashboard (Right Panel)")]
    public GameObject scoringDashboardPanel;
    public TextMeshProUGUI dashWordText;
    public TextMeshProUGUI dashBaseScoreText;
    public TextMeshProUGUI dashHitsMultText;
    public TextMeshProUGUI dashRarityMultText;
    public TextMeshProUGUI dashRelicMultText;
    public TextMeshProUGUI dashTotalDamageText;

    [Header("Post-Level Market UI")]
    public GameObject draftCardPrefab;
    public GameObject shopCardPrefab;
    public Transform draftRewardsContainer;
    public Transform shopItemsContainer;
    public TextMeshProUGUI marketTitleText;
    public TextMeshProUGUI marketCreditsText;
    public GameObject marketContinueButton;

    [Header("Deck Review UI")]
    public GameObject deckReviewPanel;
    public Transform deckReviewGrid;
    public TextMeshProUGUI deckReviewTitleText;
    public TextMeshProUGUI deckReviewAlphabetText;

    [Header("Defeat / Victory Screens")]
    public TextMeshProUGUI defeatStatsText;
    public TextMeshProUGUI victoryStatsText;

    private Dictionary<GameObject, Queue<DieUI>> _diePool = new Dictionary<GameObject, Queue<DieUI>>();

    private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    // ==========================================
    // COMBAT VISUALS & HUD
    // ==========================================
    public void UpdateRunStats(int level, int maxLevel, double damageDealt, double maxHp, int queries, int discards, int rerolls, int credits, List<RelicSO> activeRelics)
    {
        if (levelText != null) levelText.text = $"CHAPTER {level} / {maxLevel}";
        if (hpText != null) hpText.text = $"SEAL INTEGRITY: {(maxHp - damageDealt):N0} / {maxHp:N0}";
        if (queriesText != null) queriesText.text = $"FOCUS: {queries}";
        if (discardsText != null) discardsText.text = $"DISCARDS: {discards}";
        if (rerollsText != null) rerollsText.text = $"RECASTS: {rerolls}";
        if (creditsText != null) creditsText.text = $"DUST: {credits}";
        if (hpFillBar != null) hpFillBar.fillAmount = Mathf.Clamp01((float)((maxHp - damageDealt) / maxHp));

        if (marketCreditsText != null) marketCreditsText.text = $"POUCH: {credits} DUST";

        if (bossModifierText != null)
        {
            if (RunManager.Instance.isBossLevel)
            {
                bossModifierText.gameObject.SetActive(true);
                bossModifierText.text = $"<color=#FF0000>CURSE ACTIVE: {RunManager.Instance.activeBossDescription}</color>";
            }
            else bossModifierText.gameObject.SetActive(false);
        }

        if (activeRelicsText != null)
        {
            if (activeRelics == null || activeRelics.Count == 0) activeRelicsText.text = "ACTIVE RELICS:\nNone";
            else
            {
                List<string> relicNames = new List<string>();
                foreach (RelicSO relic in activeRelics) relicNames.Add(relic.relicName);
                activeRelicsText.text = "ACTIVE RELICS:\n" + string.Join("\n", relicNames);
            }
        }
    }

    public void ForceUpdateHUD() { if (RunManager.Instance != null && RunManager.Instance.settings != null) UpdateRunStats(RunManager.Instance.currentLevel, RunManager.Instance.settings.maxLevel, RunManager.Instance.currentDamageDealt, RunManager.Instance.targetFirewallHP, RunManager.Instance.queriesRemaining, RunManager.Instance.discardsRemaining, RunManager.Instance.rerollsRemaining, RunManager.Instance.currentCredits, RunManager.Instance.activeRelics); }

    // UPDATED: Now uses the high-performance pool instead of Instantiate/Destroy
    public void RefreshFixedBoard(List<DieData> currentHand)
    {
        ReturnAllDiceToPool();

        foreach (DieData dieData in currentHand)
        {
            if (dieData.diePrefab != null)
            {
                DieUI dieUI = GetDieFromPool(dieData.diePrefab, handContainer);
                if (dieUI != null)
                {
                    dieUI.SetupVisuals(dieData);
                    dieUI.isInHand = true;
                    _activeDiceOnBoard.Add(dieUI);
                }
            }
        }
    }

    public void MoveDieToWord(DieUI dieUI) { dieUI.transform.SetParent(wordContainer, false); dieUI.isInHand = false; }
    public void MoveDieToHand(DieUI dieUI) { dieUI.transform.SetParent(handContainer, false); dieUI.isInHand = true; }
    public void ReturnLettersToHand() { foreach (DieUI die in _activeDiceOnBoard) if (die != null && !die.isInHand) MoveDieToHand(die); }

    public void OnSubmitButtonClicked()
    {
        string finalWord = "";
        List<DieData> usedDice = new List<DieData>();

        foreach (Transform child in wordContainer)
        {
            DieUI activeDie = child.GetComponent<DieUI>();
            if (activeDie != null && activeDie.myData != null)
            {
                finalWord += activeDie.myData.currentFace.faceText;
                usedDice.Add(activeDie.myData);
            }
        }

        if (usedDice.Count > 0) RunManager.Instance.SubmitWord(finalWord, usedDice);
    }

    public void OnClearButtonClicked() { ReturnLettersToHand(); }

    public void OnDiscardButtonClicked()
    {
        List<DieData> selectedDice = new List<DieData>();
        foreach (Transform child in wordContainer)
        {
            DieUI activeDie = child.GetComponent<DieUI>();
            if (activeDie != null && activeDie.myData != null) selectedDice.Add(activeDie.myData);
        }

        if (selectedDice.Count > 0) RunManager.Instance.ProcessDiscard(selectedDice);
        else ShowTransientMessage("<color=#FF0000>Select at least one die to discard.</color>");
    }

    public void OnRerollButtonClicked()
    {
        List<DieData> selectedDice = new List<DieData>();
        foreach (Transform child in wordContainer)
        {
            DieUI activeDie = child.GetComponent<DieUI>();
            if (activeDie != null && activeDie.myData != null) selectedDice.Add(activeDie.myData);
        }

        if (selectedDice.Count > 0) RunManager.Instance.ProcessReroll(selectedDice);
        else ShowTransientMessage("<color=#FF0000>Select at least one die to recast.</color>");
    }

    public void ShowTransientMessage(string msg) { if (transientMessageText != null) transientMessageText.text = msg; }
    public void LogError(string errorMsg) { ShowTransientMessage($"<color=#FF0000>{errorMsg}</color>"); }

    // ==========================================
    // NEW: FIXED SCORING DASHBOARD ANIMATION
    // ==========================================
    public IEnumerator AnimateScoringSequence(RunManager.ScoreBreakdown bd)
    {
        if (scoringDashboardPanel != null) scoringDashboardPanel.SetActive(true);

        // Reset all fields to blank or "calculating" state
        if (dashWordText != null) dashWordText.text = $"INCANTING: <color=#00FFFF>{bd.word.ToUpper()}</color>";
        if (dashBaseScoreText != null) dashBaseScoreText.text = "Base Score: ...";
        if (dashHitsMultText != null) dashHitsMultText.text = "Hits Mult: ...";
        if (dashRarityMultText != null) dashRarityMultText.text = "Rarity Mult: ...";
        if (dashRelicMultText != null) dashRelicMultText.text = "Relics & Length: ...";
        if (dashTotalDamageText != null) dashTotalDamageText.text = "TOTAL INSIGHT: ...";

        yield return new WaitForSeconds(0.2f);

        // 1. Base Score
        if (dashBaseScoreText != null)
        {
            string logConcat = string.Join(" ", bd.baseLogs).Replace("\n", "");
            dashBaseScoreText.text = $"Base Score: <b>{bd.finalBaseScore}</b> <size=70%>{logConcat}</size>";
        }
        yield return new WaitForSeconds(0.3f);

        // 2. Hits Multiplier
        if (dashHitsMultText != null)
        {
            dashHitsMultText.text = $"Hits Mult: <b>x{bd.hitMultiplier:F2}</b> <size=70%><color=#AAAAAA>({bd.finalHits:N0} hits)</color></size>";
        }
        yield return new WaitForSeconds(0.3f);

        // 3. Rarity Multiplier
        if (dashRarityMultText != null)
        {
            string rarityConcat = string.Join(" ", bd.rarityLogs).Replace("\n", "");
            dashRarityMultText.text = $"Rarity Mult: <b>x{bd.rarityMult:F2}</b> <size=70%>{rarityConcat}</size>";
        }
        yield return new WaitForSeconds(0.3f);

        // 4. Global/Relic Multipliers
        if (dashRelicMultText != null)
        {
            string globalConcat = string.Join(" ", bd.globalLogs).Replace("\n", "");
            if (string.IsNullOrEmpty(globalConcat)) globalConcat = "<color=#555555>None</color>";
            dashRelicMultText.text = $"Relics/Length: <b>x{bd.globalMult:F2}</b> <size=70%>{globalConcat}</size>";
        }
        yield return new WaitForSeconds(0.4f);

        // 5. BOOM! Total Damage
        if (dashTotalDamageText != null)
        {
            dashTotalDamageText.text = $"<size=120%>TOTAL INSIGHT:</size>\n<size=150%><b><color=#00FF00>{bd.totalDamage:N0}</color></b></size>";
        }

        yield return new WaitForSeconds(1.5f);

        // We leave the panel visible until they take their next action!
        RunManager.Instance.ResolveSubmission(bd);
    }

    // ==========================================
    // SHOP / MARKET UI
    // ==========================================
    public void OpenShopUI(string shopTitle, int playerCurrency, string currencyName, List<ShopItem> items)
    {
        if (marketTitleText != null) marketTitleText.text = shopTitle;
        if (marketCreditsText != null) marketCreditsText.text = $"POUCH: {playerCurrency} {currencyName}";
        if (marketContinueButton != null) marketContinueButton.SetActive(true);

        if (draftRewardsContainer != null) foreach (Transform child in draftRewardsContainer) Destroy(child.gameObject);

        if (shopItemsContainer != null)
        {
            foreach (Transform child in shopItemsContainer) Destroy(child.gameObject);
            foreach (var item in items) Instantiate(shopCardPrefab, shopItemsContainer).GetComponent<ShopCardUI>().SetupShopItem(item);
        }
    }

    public void ShowCombinedMarket(List<RunManager.DraftUpgradeOption> upgrades, List<RunManager.DraftMutateOption> mutates, List<ShopItem> premiumItems, bool isBossDefeated)
    {
        if (marketTitleText != null) marketTitleText.text = isBossDefeated ? "<color=#FF00FF>GRIMOIRE UNBOUND - ALL KNOWLEDGE FREE</color>" : "THE SCRIPTORIUM";
        if (marketContinueButton != null) marketContinueButton.SetActive(false);

        if (draftRewardsContainer != null)
        {
            foreach (Transform child in draftRewardsContainer) Destroy(child.gameObject);
            foreach (var up in upgrades) Instantiate(draftCardPrefab, draftRewardsContainer).GetComponent<DraftCardUI>().SetupUpgrade(up);
            foreach (var mut in mutates) Instantiate(draftCardPrefab, draftRewardsContainer).GetComponent<DraftCardUI>().SetupMutation(mut);
        }

        if (shopItemsContainer != null)
        {
            foreach (Transform child in shopItemsContainer) Destroy(child.gameObject);
            foreach (var item in premiumItems) Instantiate(shopCardPrefab, shopItemsContainer).GetComponent<ShopCardUI>().SetupShopItem(item);
        }
    }

    // ==========================================
    // NEW: OBJECT POOLING FOR DICE UI
    // ==========================================
    private DieUI GetDieFromPool(GameObject prefab, Transform parent)
    {
        if (prefab == null) return null;

        if (!_diePool.ContainsKey(prefab)) _diePool[prefab] = new Queue<DieUI>();

        if (_diePool[prefab].Count > 0)
        {
            DieUI pooledDie = _diePool[prefab].Dequeue();
            pooledDie.transform.SetParent(parent, false);
            pooledDie.gameObject.SetActive(true);
            return pooledDie;
        }

        // Only instantiate if the pool is empty!
        GameObject newDieObj = Instantiate(prefab, parent);
        return newDieObj.GetComponent<DieUI>();
    }

    private void ReturnAllDiceToPool()
    {
        foreach (DieUI die in _activeDiceOnBoard)
        {
            if (die != null && die.myData != null && die.myData.diePrefab != null)
            {
                die.gameObject.SetActive(false);
                // Move it out of the active containers so it doesn't get picked up by Submit logic
                die.transform.SetParent(this.transform, false);

                if (!_diePool.ContainsKey(die.myData.diePrefab)) _diePool[die.myData.diePrefab] = new Queue<DieUI>();
                _diePool[die.myData.diePrefab].Enqueue(die);
            }
            else if (die != null) Destroy(die.gameObject); // Failsafe for orphaned UI
        }
        _activeDiceOnBoard.Clear();
    }

    // ==========================================
    // DECK REVIEW PANEL
    // ==========================================
    public void OpenDeckReview()
    {
        // [Existing Deck Review Code...]
    }

    public void OnClick_ViewAllBagStats()
    {
        // [Existing Alphabet Math Code...]
    }

    public void CloseDeckReview() { if (deckReviewPanel != null) deckReviewPanel.SetActive(false); }

    // ==========================================
    // POST-GAME SCREENS
    // ==========================================
    public void ShowDefeatScreen(int level, int breaches, int words, string bestWord, double bestScore, string rareWord, long rareHits, string topLetters, string worstLetter, string mostMutated, int dataCores)
    {
        if (defeatStatsText != null)
        {
            defeatStatsText.text =
                $"MIND SHATTERED\n\nTomes Unbound: {breaches}\nMax Chapter Reached: {level}\nIncantations Cast: {words}\n\n" +
                $"Highest Insight: {bestWord} ({bestScore:N0})\nForbidden Knowledge: {rareWord} ({rareHits:N0} global hits)\n\n" +
                $"Pillar Letters: {topLetters}\nDead Weight: {worstLetter}\nFavorite Mutation: {mostMutated}\n\n" +
                $"<color=#00FFFF>ASTRAL SEALS GATHERED: +{dataCores}</color>";
        }
    }

    public void ShowBossVictoryScreen(double finalDamage, int currentCredits)
    {
        if (victoryStatsText != null)
            victoryStatsText.text = $"GRIMOIRE DEFEATED!\nMassive Overkill: {finalDamage:N0}\nDust Banked: {currentCredits}";
    }
}