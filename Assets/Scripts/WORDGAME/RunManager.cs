using UnityEngine;
using System.Collections.Generic;

public enum RunState { Setup, Drafting, Resolving, Victory, Defeat }
// NEW: Modifiers!
public enum Relic { NounOverclock, VowelBattery, ShortCircuit }

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Run State")]
    public RunState currentState;
    public int currentLevel = 1;

    [Header("The Firewall")]
    public double targetFirewallHP;
    public double currentDamageDealt;
    public int maxQueries = 5;
    public int queriesRemaining;
    public int maxHandSize = 7;

    [Header("Modifiers")]
    public readonly List<Relic> activeRelics = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartNewRun();
    }

    public void StartNewRun()
    {
        currentLevel = 1;
        activeRelics.Clear();
        // Give the player some starting Relics for testing!
        activeRelics.Add(Relic.NounOverclock);
        activeRelics.Add(Relic.VowelBattery);

        WordValidator.Instance.ResetBurnedWordsForNewRun();
        DiceDeck.Instance.InitializeStartingDeck();

        GenerateEncounter();
    }

    private void GenerateEncounter()
    {
        targetFirewallHP = 500000000 * Mathf.Pow(1.5f, currentLevel - 1);
        currentDamageDealt = 0;
        queriesRemaining = maxQueries;

        DiceDeck.Instance.SetupEncounterPool();

        UpdateHUD();
        StartTurn();
    }

    public void StartTurn()
    {
        currentState = RunState.Drafting;
        DiceDeck.Instance.FillHand(maxHandSize);
        WordUIManager.Instance.SpawnRolledLetters(DiceDeck.Instance.currentHand);
        UpdateHUD();
    }

    public void DiscardSelectedLetters(List<DieData> selectedDice)
    {
        if (currentState != RunState.Drafting || selectedDice.Count == 0) return;

        queriesRemaining--;
        foreach (DieData die in selectedDice) DiceDeck.Instance.currentHand.Remove(die);

        WordUIManager.Instance.ClearDraftingArea();

        if (queriesRemaining <= 0 && currentDamageDealt < targetFirewallHP) TriggerDefeat();
        else StartTurn();
    }

    public void SubmitWord(string spelledWord, List<DieData> usedDice)
    {
        if (currentState != RunState.Drafting) return;

        if (!WordValidator.Instance.IsValidWord(spelledWord))
        {
            WordUIManager.Instance.LogError($"'{spelledWord}' is not recognized.");
            WordUIManager.Instance.OnClearButtonClicked();
            return;
        }
        if (WordValidator.Instance.IsWordBurned(spelledWord))
        {
            WordUIManager.Instance.LogError($"'{spelledWord}' is already burned.");
            WordUIManager.Instance.OnClearButtonClicked();
            return;
        }

        currentState = RunState.Resolving;
        WordValidator.Instance.BurnWord(spelledWord);
        queriesRemaining--;
        foreach (DieData die in usedDice) DiceDeck.Instance.currentHand.Remove(die);
        UpdateHUD();

        int baseScore = WordValidator.Instance.CalculateBaseScore(spelledWord);

        // Chain API calls: Dict -> Wiki -> Math Engine
        StartCoroutine(DictionaryAPIConnector.Instance.FetchDefinition(spelledWord, (pos, def) =>
        {
            StartCoroutine(WikiAPIConnector.Instance.PingWikipedia(spelledWord, (hits, size) =>
            {
                ExecuteDamageMath(spelledWord, pos, def, baseScore, hits, size);
            }));
        }));
    }

    private void ExecuteDamageMath(string word, string pos, string def, int baseScore, long rawHits, int tomeSize)
    {
        int finalBaseScore = baseScore;
        long finalHits = rawHits;
        double dmgMultiplier = 1.0;

        // --- RELIC ENGINE: Apply Modifiers! ---
        if (activeRelics.Contains(Relic.VowelBattery))
        {
            foreach (char c in word.ToUpper()) if ("AEIOU".Contains(c)) finalBaseScore += 2;
        }
        if (activeRelics.Contains(Relic.ShortCircuit) && word.Length == 3)
        {
            finalHits += 5000000; // Flat 5M hit bonus!
        }
        if (activeRelics.Contains(Relic.NounOverclock) && pos.ToUpper().Contains("NOUN"))
        {
            dmgMultiplier = 1.5; // 50% extra damage
        }

        // Final Calculation
        double totalDamage = (double)finalBaseScore * finalHits * tomeSize * dmgMultiplier;

        currentDamageDealt += totalDamage;
        WordUIManager.Instance.LogDamage(word, pos, def, finalBaseScore, finalHits, tomeSize, dmgMultiplier, totalDamage);
        UpdateHUD();

        if (currentDamageDealt >= targetFirewallHP) TriggerVictory();
        else if (queriesRemaining <= 0) TriggerDefeat();
        else StartTurn();
    }

    private void TriggerVictory()
    {
        currentState = RunState.Victory;
        WordUIManager.Instance.LogError("FIREWALL BREACHED! Upgrading Node...");
        currentLevel++;
        GenerateEncounter(); // Temporary auto-advance
    }

    private void TriggerDefeat()
    {
        currentState = RunState.Defeat;
        WordUIManager.Instance.LogError("CONNECTION TERMINATED. Run Failed.");
    }

    private void UpdateHUD()
    {
        if (WordUIManager.Instance != null)
        {
            WordUIManager.Instance.UpdateRunStats(currentLevel, currentDamageDealt, targetFirewallHP, queriesRemaining, DiceDeck.Instance.currentDrawPile.Count);
        }
    }
}