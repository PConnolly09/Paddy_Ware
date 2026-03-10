using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum RunState { Setup, Drafting, Resolving, Victory, Defeat, Shopping }
public enum BossModifier { None, Titan, Cipher, Virus, Drain }

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Game Settings")]
    public LexiconSettingsSO settings;

    [Header("Run State")]
    public RunState currentState;
    public int currentLevel = 1;
    public int currentCredits = 0;

    [Header("The Firewall")]
    public double targetFirewallHP;
    public double currentDamageDealt;
    public int queriesRemaining;
    public int discardsRemaining;
    public int rerollsRemaining;
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

    public class ScoreBreakdown
    {
        public string word, pos, def;
        public int baseScore, finalBaseScore;
        public long hits, finalHits;
        public int tomeSize, finalTomeSize;
        public float lengthMult, rarityMult;
        public List<string> relicLogs = new();
        public bool isNewWord, isFavorite, isNewHighScore;
        public double totalDamage;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start() { ShopManager.Instance.GeneratePreRunShop(); }

    public void StartNewRun()
    {
        currentLevel = 1;
        currentCredits = 300;

        totalWordsEntered = 0;
        firewallsBreached = 0;
        highestScoringWord = "NONE";
        highestScore = 0;
        mostUniqueWord = "NONE";
        lowestWikiHits = long.MaxValue;
        letterUsageCount.Clear();
        activeRelics.Clear();

        WordValidator.Instance.ResetBurnedWordsForNewRun();
        DiceDeck.Instance.InitializeStartingDeck();
        for (int i = 0; i < LexiconSaveManager.Instance.currentData.bonusStartingD20s; i++) DiceDeck.Instance.startingDeckBlueprint.Add(DiceType.D20_Rare);

        GenerateEncounter();
    }

    private void GenerateEncounter()
    {
        if (currentLevel > settings.maxLevel) { return; }

        isBossLevel = currentLevel % 5 == 0;
        activeBossModifier = BossModifier.None;
        activeBossDescription = "";

        double hpCalc = settings.baseFirewallHP * Mathf.Pow(settings.hpScaleMultiplier, currentLevel - 1);

        if (isBossLevel)
        {
            activeBossModifier = (BossModifier)UnityEngine.Random.Range(1, 5);
            switch (activeBossModifier)
            {
                case BossModifier.Titan: hpCalc *= 4.0f; activeBossDescription = "THE TITAN: Massive HP pool."; break;
                case BossModifier.Cipher: hpCalc *= 2.5f; activeBossDescription = "THE CIPHER: All letters have a base score of 1."; break;
                case BossModifier.Virus: hpCalc *= 2.5f; activeBossDescription = "THE VIRUS: All Relics are disabled."; break;
                case BossModifier.Drain: hpCalc *= 2.5f; activeBossDescription = "THE DRAIN: Resources halved."; break;
            }
        }

        targetFirewallHP = hpCalc;
        currentDamageDealt = 0;

        int bQueries = settings.startingQueries + LexiconSaveManager.Instance.currentData.bonusStartingQueries;
        queriesRemaining = (activeBossModifier == BossModifier.Drain) ? Mathf.Max(1, bQueries - 2) : bQueries;
        discardsRemaining = settings.startingDiscards;
        rerollsRemaining = settings.startingRerolls;

        wordsUsedThisLevel = 0;
        diceSpentThisLevel = 0;

        DiceDeck.Instance.SetupEncounterPool();
        UpdateHUD();
        StartTurn();
    }

    public void StartTurn()
    {
        currentState = RunState.Drafting;
        DiceDeck.Instance.FillHand(7);
        WordUIManager.Instance.RefreshFixedBoard(DiceDeck.Instance.currentHand);
        UpdateHUD();
    }

    public void ProcessDiscard(DieData dieToDiscard)
    {
        if (discardsRemaining <= 0 || currentState != RunState.Drafting) return;
        discardsRemaining--;
        DiceDeck.Instance.currentHand.Remove(dieToDiscard);
        StartTurn();
    }

    public void ProcessReroll(DieData dieToReroll)
    {
        if (rerollsRemaining <= 0 || currentState != RunState.Drafting) return;
        rerollsRemaining--;
        DiceDeck.Instance.currentHand.Remove(dieToReroll);
        DiceDeck.Instance.currentDrawPile.Insert(0, dieToReroll.Type);
        StartTurn();
    }

    public void SubmitWord(string spelledWord, List<DieData> usedDice)
    {
        if (currentState != RunState.Drafting) return;

        if (WordValidator.Instance.IsWordBurned(spelledWord))
        {
            WordUIManager.Instance.LogError($"'{spelledWord}' has reached its burn limit.");
            WordUIManager.Instance.ReturnLettersToHand();
            return;
        }

        // Lock state so they can't click Submit 10 times during the API call
        currentState = RunState.Resolving;

        // 1. Wikipedia is the ultimate truth. We ping it FIRST.
        StartCoroutine(WikiAPIConnector.Instance.PingWikipedia(spelledWord, (hits, size) =>
        {
            if (hits <= 0)
            {
                WordUIManager.Instance.LogError($"ACCESS DENIED: '{spelledWord}' returned 0 Wikipedia hits.");
                WordUIManager.Instance.ReturnLettersToHand();
                currentState = RunState.Drafting; // Unlock board
                return;
            }

            // 2. If it exists on Wiki, ping Datamuse for the definition/grammar
            StartCoroutine(DictionaryAPIConnector.Instance.FetchDefinition(spelledWord, (posList, def, isValidAPI) =>
            {
                // If it isn't in Datamuse (e.g. "MICROSOFT" or "GANDALF"), we declare it a PROPER NOUN!
                if (!isValidAPI)
                {
                    posList = new List<string> { "NOUN", "PROPER NOUN" };
                    def = "Data Entity / Uncatalogued Brand / Proper Noun.";
                }

                // Complete the Submission Logic
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

                int baseScore = WordValidator.Instance.CalculateBaseScore(spelledWord);

                ExecuteDamageMath(spelledWord, posList, def, baseScore, hits, size);
            }));
        }));
    }

    private void ExecuteDamageMath(string word, List<string> posList, string def, int baseScore, long rawHits, int tomeSize)
    {
        ScoreBreakdown bd = new ScoreBreakdown
        {
            word = word,
            pos = string.Join(", ", posList),
            def = def,
            baseScore = baseScore,
            finalBaseScore = baseScore,
            hits = rawHits,
            finalHits = rawHits,
            tomeSize = tomeSize,
            finalTomeSize = tomeSize
        };

        if (activeBossModifier == BossModifier.Cipher) bd.finalBaseScore = word.Length;

        double dmgMultiplier = 1.0;
        string upperWord = word.ToUpper();

        bd.lengthMult = settings.lengthMultipliers[Mathf.Clamp(word.Length, 0, settings.lengthMultipliers.Length - 1)];
        bd.rarityMult = settings.commonWordBonus;

        if (rawHits < 1000000) bd.rarityMult = settings.rareWordBonus;
        else if (rawHits < 10000000) bd.rarityMult = settings.uncommonWordBonus;
        else if (rawHits > 100000000) bd.rarityMult = settings.mainstreamPenalty;

        dmgMultiplier *= (bd.lengthMult * bd.rarityMult);

        if (activeBossModifier != BossModifier.Virus)
        {
            if (activeRelics.Contains(Relic.NounOverclock) && posList.Contains("NOUN")) { dmgMultiplier *= 1.5; bd.relicLogs.Add("Noun Overclock (x1.5 Mult)"); }
            if (activeRelics.Contains(Relic.VerbDrive) && posList.Contains("VERB")) { bd.finalHits += 5000000; bd.relicLogs.Add("Verb Drive (+5M Hits)"); }

            if (activeRelics.Contains(Relic.AdjectiveArray) && posList.Contains("ADJECTIVE")) { bd.finalBaseScore += (word.Length * 2); bd.relicLogs.Add($"Adjective Array (+{word.Length * 2} Base)"); }
            if (activeRelics.Contains(Relic.AdverbAccelerator) && posList.Contains("ADVERB")) { bd.finalTomeSize *= 2; bd.relicLogs.Add("Adverb Accelerator (x2 Tome)"); }

            if (activeRelics.Contains(Relic.VowelBattery))
            {
                int vowelCount = upperWord.Count(c => "AEIOU".Contains(c));
                if (vowelCount > 0) { bd.finalBaseScore += (vowelCount * 2); bd.relicLogs.Add($"Vowel Battery (+{vowelCount * 2} Base)"); }
            }
            if (activeRelics.Contains(Relic.ConsonantCruncher))
            {
                int maxCons = 0, currentCons = 0;
                foreach (char c in upperWord) { if (!"AEIOU".Contains(c)) currentCons++; else { if (currentCons > maxCons) maxCons = currentCons; currentCons = 0; } }
                if (currentCons > maxCons) maxCons = currentCons;
                if (maxCons >= 4) { dmgMultiplier *= 2.0; bd.relicLogs.Add("Consonant Cruncher (x2.0 Mult)"); }
            }
            if (activeRelics.Contains(Relic.DoubleVision))
            {
                bool hasDouble = false;
                for (int i = 0; i < upperWord.Length - 1; i++) if (upperWord[i] == upperWord[i + 1]) hasDouble = true;
                if (hasDouble) { dmgMultiplier *= 2.0; bd.relicLogs.Add("Double Vision (x2.0 Mult)"); }
            }
            if (activeRelics.Contains(Relic.QwertyVirus) && upperWord.IndexOfAny(new char[] { 'Q', 'Z', 'J', 'X' }) >= 0) { dmgMultiplier *= 3.0; bd.relicLogs.Add("QWERTY Virus (x3.0 Mult)"); }

            if (activeRelics.Contains(Relic.ShortCircuit) && word.Length == 3) { bd.finalHits += 5000000; bd.relicLogs.Add("Short Circuit (+5M Hits)"); }
            if (activeRelics.Contains(Relic.FourLetterWord) && word.Length == 4) { bd.finalBaseScore *= 3; bd.relicLogs.Add("Four-Letter Word (x3 Base)"); }
            if (activeRelics.Contains(Relic.TheLongCon) && word.Length >= 7) { bd.finalBaseScore += 10; dmgMultiplier *= 1.5; bd.relicLogs.Add("The Long Con (+10 Base, x1.5 Mult)"); }

            if (activeRelics.Contains(Relic.Pluralizer) && upperWord.EndsWith("S")) { bd.finalHits += 2000000; bd.relicLogs.Add("Pluralizer (+2M Hits)"); }
            if (activeRelics.Contains(Relic.GerundEngine) && upperWord.EndsWith("ING")) { bd.finalTomeSize = (int)(bd.finalTomeSize * 1.5); bd.relicLogs.Add("Gerund Engine (x1.5 Tome)"); }
            if (activeRelics.Contains(Relic.PrefixProtocol) && (upperWord.StartsWith("RE") || upperWord.StartsWith("UN"))) { bd.finalBaseScore *= 2; bd.relicLogs.Add("Prefix Protocol (x2 Base)"); }

            char[] arr = upperWord.ToCharArray(); System.Array.Reverse(arr); string rev = new string(arr);
            if (activeRelics.Contains(Relic.PalindromeProtocol) && upperWord == rev && upperWord.Length > 1) { dmgMultiplier *= 5.0; bd.relicLogs.Add("Palindrome Protocol (x5.0 Mult)"); }

            if (activeRelics.Contains(Relic.TomeSkimmer) && tomeSize < 500) { dmgMultiplier *= 3.0; bd.relicLogs.Add("Tome Skimmer (x3.0 Mult)"); }
            if (activeRelics.Contains(Relic.MainstreamInjector) && rawHits > 50000000) { bd.finalBaseScore += 15; bd.relicLogs.Add("Mainstream Injector (+15 Base)"); }
            if (activeRelics.Contains(Relic.HipsterCache) && rawHits < 1000000) { dmgMultiplier *= 3.0; bd.relicLogs.Add("Hipster Cache (x3.0 Mult)"); }

            if (activeRelics.Contains(Relic.LastResort) && queriesRemaining == 0) { dmgMultiplier *= 3.0; bd.relicLogs.Add("Last Resort (x3.0 Mult)"); }
            if (activeRelics.Contains(Relic.FirstStrike) && wordsUsedThisLevel == 1) { dmgMultiplier *= 2.0; bd.relicLogs.Add("First Strike (x2.0 Mult)"); }
        }
        else { bd.relicLogs.Add("<color=#FF0000>DISABLED BY VIRUS</color>"); }

        int lifetimePlays = LexiconSaveManager.Instance.GetWordPlayCount(word);
        bd.isNewWord = lifetimePlays == 0;
        bd.isFavorite = lifetimePlays >= LexiconSaveManager.Instance.favoriteThreshold;

        if (bd.isNewWord) dmgMultiplier *= 1.5;
        if (bd.isFavorite) dmgMultiplier *= 1.2;

        bd.totalDamage = (double)bd.finalBaseScore * bd.finalHits * bd.finalTomeSize * dmgMultiplier;

        if (bd.totalDamage > highestScore) { highestScore = bd.totalDamage; highestScoringWord = word; }
        if (rawHits < lowestWikiHits && rawHits > 0) { lowestWikiHits = rawHits; mostUniqueWord = word; }

        LexiconSaveManager.Instance.RecordWordPlay(word, bd.totalDamage, out bd.isNewHighScore);

        UpdateHUD();

        StartCoroutine(WordUIManager.Instance.AnimateScoringSequence(bd));
    }

    public void ApplyCalculatedDamage(double totalDamage)
    {
        currentDamageDealt += totalDamage;
        UpdateHUD();

        if (currentDamageDealt >= targetFirewallHP) TriggerVictory();
        else if (queriesRemaining <= 0) TriggerDefeat();
        else StartTurn();
    }

    private void TriggerVictory()
    {
        currentState = RunState.Victory;
        currentCredits += (100 + (queriesRemaining * 50));

        if (isBossLevel) WordUIManager.Instance.ShowBossVictoryScreen(currentDamageDealt, currentCredits);
        else
        {
            WordUIManager.Instance.ShowTransientMessage("<color=#00FF00>FIREWALL BREACHED. ADVANCING...</color>");
            currentLevel++;
            GenerateEncounter();
        }
    }

    public void AdvanceFromBossVictory()
    {
        currentState = RunState.Shopping;
        ShopManager.Instance.GenerateMidRunShop();
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
        if (WordUIManager.Instance != null && settings != null)
            WordUIManager.Instance.UpdateRunStats(currentLevel, settings.maxLevel, currentDamageDealt, targetFirewallHP, queriesRemaining, discardsRemaining, rerollsRemaining, currentCredits, activeRelics);
    }
}