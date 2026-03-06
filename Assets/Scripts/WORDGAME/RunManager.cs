using UnityEngine;
using System.Collections.Generic;

public enum RunState { Setup, Drafting, Resolving, Victory, Defeat }

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

        // Fills hand up to 7, keeps unspent dice!
        DiceDeck.Instance.FillHand(maxHandSize);
        WordUIManager.Instance.SpawnRolledLetters(DiceDeck.Instance.currentHand);
        UpdateHUD();
    }

    public void DiscardSelectedLetters(List<DieData> selectedDice)
    {
        if (currentState != RunState.Drafting || selectedDice.Count == 0) return;

        // Discarding costs a query, but removes bad dice from your hand to draw new ones
        queriesRemaining--;
        foreach (DieData die in selectedDice)
        {
            DiceDeck.Instance.currentHand.Remove(die);
        }

        WordUIManager.Instance.ClearDraftingArea();

        if (queriesRemaining <= 0 && currentDamageDealt < targetFirewallHP)
        {
            currentState = RunState.Defeat;
            WordUIManager.Instance.LogError("CONNECTION TERMINATED. Run Failed.");
        }
        else
        {
            StartTurn();
        }
    }

    public void SubmitWord(string spelledWord, List<DieData> usedDice)
    {
        if (currentState != RunState.Drafting) return;

        if (!WordValidator.Instance.IsValidWord(spelledWord))
        {
            WordUIManager.Instance.LogError($"'{spelledWord}' is not recognized in the archive.");
            WordUIManager.Instance.OnClearButtonClicked(); // Free retry
            return;
        }

        if (WordValidator.Instance.IsWordBurned(spelledWord))
        {
            WordUIManager.Instance.LogError($"'{spelledWord}' has already been burned this run.");
            WordUIManager.Instance.OnClearButtonClicked(); // Free retry
            return;
        }

        currentState = RunState.Resolving;
        WordValidator.Instance.BurnWord(spelledWord);
        queriesRemaining--;

        // Remove the physically used dice from the persistent hand
        foreach (DieData die in usedDice) DiceDeck.Instance.currentHand.Remove(die);

        UpdateHUD();

        int baseScore = WordValidator.Instance.CalculateBaseScore(spelledWord);

        // Chain the APIs: Dictionary First, then Wikipedia
        StartCoroutine(DictionaryAPIConnector.Instance.FetchDefinition(spelledWord, (pos, def) =>
        {
            StartCoroutine(WikiAPIConnector.Instance.PingWikipediaForDamage(spelledWord, baseScore, (damage, word, bScore, hits, size) =>
            {
                OnDamageCalculated(damage, word, pos, def, bScore, hits, size);
            }));
        }));
    }

    private void OnDamageCalculated(double damageDealt, string word, string pos, string def, int baseScore, int hits, int tomeSize)
    {
        currentDamageDealt += damageDealt;
        WordUIManager.Instance.LogDamage(word, pos, def, baseScore, hits, tomeSize, damageDealt);
        UpdateHUD();

        if (currentDamageDealt >= targetFirewallHP)
        {
            currentState = RunState.Victory;
            WordUIManager.Instance.LogError("FIREWALL BREACHED! Advancing to next Mainframe...");

            currentLevel++;
            GenerateEncounter();
        }
        else if (queriesRemaining <= 0)
        {
            currentState = RunState.Defeat;
            WordUIManager.Instance.LogError("CONNECTION TERMINATED. Run Failed.");
        }
        else
        {
            StartTurn();
        }
    }

    private void UpdateHUD()
    {
        if (WordUIManager.Instance != null)
        {
            WordUIManager.Instance.UpdateRunStats(currentLevel, currentDamageDealt, targetFirewallHP, queriesRemaining, DiceDeck.Instance.currentDrawPile.Count);
        }
    }
}