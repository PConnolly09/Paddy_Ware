using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum RunState { Setup, Drafting, Resolving, Victory, Defeat, Shopping }
public enum BossModifier { None, Titan, Cipher, Virus, Drain }

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Run State")]
    public RunState currentState;
    public int currentLevel = 1;

    [Header("The Economy")]
    public int currentCredits = 0;
    public int totalCreditsSpent = 0;

    [Header("The Firewall")]
    public double targetFirewallHP;
    public double currentDamageDealt;
    public int maxQueries = 5;
    public int queriesRemaining;
    public int maxHandSize = 7;

    [Header("Boss Encounter")]
    public bool isBossLevel = false;
    public BossModifier activeBossModifier = BossModifier.None;
    public string activeBossDescription = "";

    [Header("Round Statistics")]
    public int wordsUsedThisLevel;
    public int diceSpentThisLevel;

    [Header("Lifetime Run Statistics")]
    public int totalWordsEntered;
    public int firewallsBreached;
    public string highestScoringWord;
    public double highestScore;
    public string mostUniqueWord;
    public long lowestWikiHits;
    public readonly Dictionary<char, int> letterUsageCount = new();

    public readonly List<Relic> activeRelics = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ShopManager.Instance.GeneratePreRunShop();
    }

    public void StartNewRun()
    {
        currentLevel = 1;
        currentCredits = 300; // FIX: Start runs with 300 credits!
        totalCreditsSpent = 0;

        totalWordsEntered = 0;
        firewallsBreached = 0;
        highestScoringWord = "NONE";
        highestScore = 0;
        mostUniqueWord = "NONE";
        lowestWikiHits = long.MaxValue;
        letterUsageCount.Clear();
        activeRelics.Clear();

        WordValidator.Instance.ResetBurnedWordsForNewRun();

        maxQueries = 5 + LexiconSaveManager.Instance.currentData.bonusStartingQueries;
        DiceDeck.Instance.InitializeStartingDeck();
        for (int i = 0; i < LexiconSaveManager.Instance.currentData.bonusStartingD20s; i++)
        {
            DiceDeck.Instance.startingDeckBlueprint.Add(DiceType.D20_Rare);
        }

        GenerateEncounter();
    }

    private void GenerateEncounter()
    {
        isBossLevel = currentLevel % 5 == 0;
        activeBossModifier = BossModifier.None;
        activeBossDescription = "";

        double hpCalc = 500000000 * Mathf.Pow(1.5f, currentLevel - 1);

        if (isBossLevel)
        {
            activeBossModifier = (BossModifier)UnityEngine.Random.Range(1, 5);

            switch (activeBossModifier)
            {
                case BossModifier.Titan: hpCalc *= 4.0f; activeBossDescription = "THE TITAN: Massive HP pool."; break;
                case BossModifier.Cipher: hpCalc *= 2.5f; activeBossDescription = "THE CIPHER: All letters have a base score of 1."; break;
                case BossModifier.Virus: hpCalc *= 2.5f; activeBossDescription = "THE VIRUS: All Relics are disabled."; break;
                case BossModifier.Drain: hpCalc *= 2.5f; activeBossDescription = "THE DRAIN: Start with 2 fewer Queries."; break;
            }
        }

        targetFirewallHP = hpCalc;
        currentDamageDealt = 0;
        queriesRemaining = (activeBossModifier == BossModifier.Drain) ? Mathf.Max(1, maxQueries - 2) : maxQueries;
        wordsUsedThisLevel = 0;
        diceSpentThisLevel = 0;

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

        if (!WordValidator.Instance.IsValidWord(spelledWord)) { WordUIManager.Instance.LogError($"'{spelledWord}' is not recognized."); WordUIManager.Instance.OnClearButtonClicked(); return; }
        if (WordValidator.Instance.IsWordBurned(spelledWord)) { WordUIManager.Instance.LogError($"'{spelledWord}' has reached its burn limit."); WordUIManager.Instance.OnClearButtonClicked(); return; }

        currentState = RunState.Resolving;
        WordValidator.Instance.BurnWord(spelledWord);
        queriesRemaining--;
        wordsUsedThisLevel++;
        totalWordsEntered++;
        diceSpentThisLevel += usedDice.Count;

        foreach (char c in spelledWord.ToUpper())
        {
            if (letterUsageCount.ContainsKey(c)) letterUsageCount[c]++;
            else letterUsageCount[c] = 1;
        }

        foreach (DieData die in usedDice) DiceDeck.Instance.currentHand.Remove(die);
        UpdateHUD();

        int baseScore = WordValidator.Instance.CalculateBaseScore(spelledWord);

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
        double finalTomeSize = tomeSize;
        double dmgMultiplier = 1.0;
        string upperWord = word.ToUpper();
        string upperPos = pos.ToUpper();

        List<string> triggeredRelics = new();

        if (activeBossModifier == BossModifier.Cipher) finalBaseScore = word.Length;

        bool relicsEnabled = activeBossModifier != BossModifier.Virus;

        if (relicsEnabled)
        {
            if (activeRelics.Contains(Relic.NounOverclock) && upperPos.Contains("NOUN")) { dmgMultiplier *= 1.5; triggeredRelics.Add("Noun Overclock"); }
            if (activeRelics.Contains(Relic.VerbDrive) && upperPos.Contains("VERB")) { finalHits += 5000000; triggeredRelics.Add("Verb Drive"); }
            if (activeRelics.Contains(Relic.AdjectiveArray) && upperPos.Contains("ADJECTIVE")) { finalBaseScore += (word.Length * 2); triggeredRelics.Add("Adjective Array"); }
            if (activeRelics.Contains(Relic.AdverbAccelerator) && upperPos.Contains("ADVERB")) { finalTomeSize *= 2.0; triggeredRelics.Add("Adverb Accelerator"); }

            if (activeRelics.Contains(Relic.VowelBattery))
            {
                int vowelCount = upperWord.Count(c => "AEIOU".Contains(c));
                if (vowelCount > 0) { finalBaseScore += (vowelCount * 2); triggeredRelics.Add("Vowel Battery"); }
            }
            if (activeRelics.Contains(Relic.ConsonantCruncher))
            {
                int maxCons = 0, currentCons = 0;
                foreach (char c in upperWord) { if (!"AEIOU".Contains(c)) currentCons++; else { if (currentCons > maxCons) maxCons = currentCons; currentCons = 0; } }
                if (currentCons > maxCons) maxCons = currentCons;
                if (maxCons >= 4) { dmgMultiplier *= 2.0; triggeredRelics.Add("Consonant Cruncher"); }
            }
            if (activeRelics.Contains(Relic.DoubleVision))
            {
                bool hasDouble = false;
                for (int i = 0; i < upperWord.Length - 1; i++) if (upperWord[i] == upperWord[i + 1]) hasDouble = true;
                if (hasDouble) { dmgMultiplier *= 2.0; triggeredRelics.Add("Double Vision"); }
            }
            if (activeRelics.Contains(Relic.QwertyVirus) && upperWord.IndexOfAny(new char[] { 'Q', 'Z', 'J', 'X' }) >= 0) { dmgMultiplier *= 3.0; triggeredRelics.Add("QWERTY Virus"); }

            if (activeRelics.Contains(Relic.ShortCircuit) && word.Length == 3) { finalHits += 5000000; triggeredRelics.Add("Short Circuit"); }
            if (activeRelics.Contains(Relic.FourLetterWord) && word.Length == 4) { finalBaseScore *= 3; triggeredRelics.Add("Four-Letter Word"); }
            if (activeRelics.Contains(Relic.TheLongCon) && word.Length >= 7) { finalBaseScore += 10; dmgMultiplier *= 1.5; triggeredRelics.Add("The Long Con"); }

            if (activeRelics.Contains(Relic.Pluralizer) && upperWord.EndsWith("S")) { finalHits += 2000000; triggeredRelics.Add("Pluralizer"); }
            if (activeRelics.Contains(Relic.GerundEngine) && upperWord.EndsWith("ING")) { finalTomeSize *= 1.5; triggeredRelics.Add("Gerund Engine"); }
            if (activeRelics.Contains(Relic.PrefixProtocol) && (upperWord.StartsWith("RE") || upperWord.StartsWith("UN"))) { finalBaseScore *= 2; triggeredRelics.Add("Prefix Protocol"); }
            if (activeRelics.Contains(Relic.PalindromeProtocol))
            {
                char[] arr = upperWord.ToCharArray(); System.Array.Reverse(arr); string rev = new string(arr);
                if (upperWord == rev && upperWord.Length > 1) { dmgMultiplier *= 5.0; triggeredRelics.Add("Palindrome Protocol"); }
            }

            if (activeRelics.Contains(Relic.TomeSkimmer) && tomeSize < 500) { dmgMultiplier *= 3.0; triggeredRelics.Add("Tome Skimmer"); }
            if (activeRelics.Contains(Relic.MainstreamInjector) && rawHits > 50000000) { finalBaseScore += 15; triggeredRelics.Add("Mainstream Injector"); }
            if (activeRelics.Contains(Relic.HipsterCache) && rawHits < 1000000) { dmgMultiplier *= 3.0; triggeredRelics.Add("Hipster Cache"); }

            if (activeRelics.Contains(Relic.LastResort) && queriesRemaining == 0) { dmgMultiplier *= 3.0; triggeredRelics.Add("Last Resort"); }
            if (activeRelics.Contains(Relic.FirstStrike) && wordsUsedThisLevel == 1) { dmgMultiplier *= 2.0; triggeredRelics.Add("First Strike"); }
        }
        else { triggeredRelics.Add("<color=#FF0000>DISABLED BY VIRUS</color>"); }

        int lifetimePlays = LexiconSaveManager.Instance.GetWordPlayCount(word);
        bool isNewWord = lifetimePlays == 0;
        bool isFavorite = lifetimePlays >= LexiconSaveManager.Instance.favoriteThreshold;

        if (isNewWord) dmgMultiplier *= 1.5;
        if (isFavorite) dmgMultiplier *= 1.2;

        double totalDamage = (double)finalBaseScore * finalHits * finalTomeSize * dmgMultiplier;
        currentDamageDealt += totalDamage;

        if (totalDamage > highestScore) { highestScore = totalDamage; highestScoringWord = word; }
        if (rawHits < lowestWikiHits && rawHits > 0) { lowestWikiHits = rawHits; mostUniqueWord = word; }

        LexiconSaveManager.Instance.RecordWordPlay(word);

        string relicLog = triggeredRelics.Count > 0 ? string.Join(", ", triggeredRelics) : "";
        WordUIManager.Instance.LogDamage(word, pos, def, finalBaseScore, finalHits, (int)finalTomeSize, dmgMultiplier, totalDamage, isNewWord, isFavorite, relicLog);

        UpdateHUD();

        if (currentDamageDealt >= targetFirewallHP) TriggerVictory();
        else if (queriesRemaining <= 0) TriggerDefeat();
        else StartTurn();
    }

    private void TriggerVictory()
    {
        currentState = RunState.Victory;
        firewallsBreached++;
        int creditReward = (100 + (queriesRemaining * 50)) * (isBossLevel ? 3 : 1);
        currentCredits += creditReward;
        WordUIManager.Instance.ShowVictoryScreen(wordsUsedThisLevel, diceSpentThisLevel, queriesRemaining, currentDamageDealt, creditReward, isBossLevel);
    }

    public void AdvanceToNextLevel()
    {
        if (currentState != RunState.Victory) return;

        if (isBossLevel) { currentState = RunState.Shopping; ShopManager.Instance.GenerateMidRunShop(); }
        else { currentLevel++; GenerateEncounter(); }
    }

    public void CloseShopAndAdvance()
    {
        if (currentState == RunState.Shopping) { currentLevel++; GenerateEncounter(); }
    }

    private void TriggerDefeat()
    {
        currentState = RunState.Defeat;
        WordUIManager.Instance.LogError("CONNECTION TERMINATED. Run Failed.");

        int dataCoresEarned = (firewallsBreached * 100) + (currentCredits / 10);
        LexiconSaveManager.Instance.currentData.dataCores += dataCoresEarned;
        LexiconSaveManager.Instance.SaveGame();

        List<KeyValuePair<char, int>> sortedLetters = letterUsageCount.ToList();
        sortedLetters.Sort((x, y) => y.Value.CompareTo(x.Value));
        string top5Letters = "None", leastCommon = "None";

        if (sortedLetters.Count > 0)
        {
            var top5 = sortedLetters.Take(5).Select(kvp => $"{kvp.Key} ({kvp.Value})");
            top5Letters = string.Join(", ", top5);
            leastCommon = $"{sortedLetters.Last().Key} ({sortedLetters.Last().Value})";
        }

        WordUIManager.Instance.ShowDefeatScreen(currentLevel, firewallsBreached, totalWordsEntered, highestScoringWord, highestScore, mostUniqueWord, lowestWikiHits, top5Letters, leastCommon, dataCoresEarned);
    }

    private void UpdateHUD()
    {
        if (WordUIManager.Instance != null)
            WordUIManager.Instance.UpdateRunStats(currentLevel, currentDamageDealt, targetFirewallHP, queriesRemaining, DiceDeck.Instance.currentDrawPile.Count, currentCredits, activeRelics);
    }
}