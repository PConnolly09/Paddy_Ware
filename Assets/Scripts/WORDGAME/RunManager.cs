using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public enum RunState { Setup, Drafting, Resolving, Victory, Defeat, Shopping }
public enum BossModifier { None, Titan, Cipher, Virus, Drain }

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Game Settings")]
    public LexiconSettingsSO settings;
    public LexiconCharacterSO activeCharacter;

    [Header("Run State")]
    public RunState currentState;
    public int currentLevel = 1;
    public int currentCredits = 0;
    public float[] runLengthMultipliers;
    public int rareWordStreak = 0;
    public float currentObscurityMultiplier = 0f;

    // ==========================================
    // STREAK SYSTEM VARIABLES
    // ==========================================
    private int _lastWordLength = 0;
    private string _lastWordPOS = "";

    public int echoStreak = 0;       // Same Length
    public int chaosStreak = 0;      // Different Length
    public int syntaxStreak = 0;     // Same POS
    public int diversityStreak = 0;  // Different POS

    // ==========================================
    // LEVEL TRACKING
    // ==========================================
    private List<DieData> _dicePlayedThisLevel = new List<DieData>();

    [Header("The Firewall")]
    public double targetFirewallHP;
    public double currentDamageDealt;
    public int queriesRemaining;
    public int discardsRemaining;
    public int rerollsRemaining;
    public bool isBossLevel = false;
    public BossModifier activeBossModifier = BossModifier.None;
    public string activeBossDescription = "";

    [Header("Lifetime Run Statistics")]
    public int totalWordsEntered;
    public int firewallsBreached;
    public double highestScore;
    public double totalRunScore;
    public string highestScoringWord = "NONE";
    public string mostUniqueWord = "NONE";
    public long lowestWikiHits = long.MaxValue;

    public readonly Dictionary<char, int> letterUsageCount = new();
    public readonly List<RelicSO> activeRelics = new();

    public class ScoreBreakdown
    {
        public string word, pos, tags;
        public int baseScore, finalBaseScore;
        public long hits, finalHits;
        public float hitMultiplier;
        public float datamuseFrequency;
        public float rarityMult;
        public float lengthMult;
        public double globalMult, totalDamage;
        public bool isAnomaly;
        public List<string> baseLogs = new();
        public List<string> hitLogs = new();
        public List<string> rarityLogs = new();
        public List<string> globalLogs = new();
        public List<DieData> usedDice;
        public bool isNewWord, isFavorite, isNewHighScore;
    }

    public class DraftUpgradeOption { public DieData die; public DieFace face; public int bonusAmount; }
    public class DraftMutateOption { public DieData die; public DieFace face; public string newFaceText; public bool isSplitFace; }

    public class RunLinguisticHistory
    {
        // Tracks every word length you've successfully played this run
        public HashSet<int> wordLengthsPlayed = new HashSet<int>();

        // Tracks Parts of Speech (NOUN, VERB, etc.)
        public HashSet<string> partsOfSpeechPlayed = new HashSet<string>();

        // Special linguistic quirks
        public bool hasPlayedPalindrome = false;
        public bool hasPlayedAnagram = false;
        public int highestBaseScore = 0;
    }

    public RunLinguisticHistory currentRunHistory = new RunLinguisticHistory();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==========================================
    // INITIALIZATION & GUARANTEED RESTART
    // ==========================================
    public void InitializeNewRun(LexiconCharacterSO chosenClass)
    {
        activeCharacter = chosenClass;
        currentLevel = 1;
        currentCredits = chosenClass.startingCredits;
        totalWordsEntered = 0;
        firewallsBreached = 0;
        highestScore = 0;
        totalRunScore = 0;
        rareWordStreak = 0;
        currentObscurityMultiplier = 0f;
        highestScoringWord = "NONE";
        mostUniqueWord = "NONE";
        lowestWikiHits = long.MaxValue;

        // NEW: Reset all streaks for the new run!
        _lastWordLength = 0;
        _lastWordPOS = "";
        echoStreak = 0;
        chaosStreak = 0;
        syntaxStreak = 0;
        diversityStreak = 0;

        letterUsageCount.Clear();
        activeRelics.Clear();
        _dicePlayedThisLevel.Clear();

        if (settings != null && settings.lengthMultipliers != null)
        {
            runLengthMultipliers = new float[settings.lengthMultipliers.Length];
            System.Array.Copy(settings.lengthMultipliers, runLengthMultipliers, settings.lengthMultipliers.Length);
        }

        WordValidator.Instance.ResetBurnedWordsForNewRun();
        DiceDeck.Instance.InitializeStartingDeck(chosenClass);
    }

    public void RestartCurrentRun()
    {
        if (activeCharacter != null)
        {
            // Hard reset to guarantee it wipes the old data before returning to the shop!
            currentDamageDealt = 0;
            targetFirewallHP = 0;
            InitializeNewRun(activeCharacter);

            if (GameDirector.Instance != null)
                GameDirector.Instance.TransitionToState(GameState.PreRunShop);
        }
    }

    public void GenerateEncounter()
    {
        // 1. Give the player their starting resources for the entire run
        int bQueries = settings.startingQueries + LexiconSaveManager.Instance.currentData.bonusStartingQueries;
        if (activeCharacter != null) bQueries += activeCharacter.bonusQueries;

        queriesRemaining = bQueries;
        discardsRemaining = settings.startingDiscards;
        rerollsRemaining = settings.startingRerolls;

        // 2. It is Level 1, so we safely start at 0 damage
        currentDamageDealt = 0;

        // 3. Setup the physical dice bag for the run
        DiceDeck.Instance.SetupEncounterPool();

        // 4. Generate the Level 1 Boss HP
        GenerateNextFirewall();

        StartTurn();
    }


    public void StartTurn()
    {
        currentState = RunState.Drafting;
        DiceDeck.Instance.FillHand(7);
        WordUIManager.Instance.RefreshFixedBoard(DiceDeck.Instance.currentHand);
        UpdateHUD();
    }

    public void ProcessDiscard(List<DieData> diceToDiscard)
    {
        if (discardsRemaining <= 0 || currentState != RunState.Drafting || diceToDiscard.Count == 0) return;

        discardsRemaining--;
        foreach (DieData die in diceToDiscard)
        {
            foreach (RelicSO relic in activeRelics) relic.OnDiscard(die);
            DiceDeck.Instance.ReturnToBag(die);
        }
        StartTurn();
    }

    public void ProcessReroll(List<DieData> diceToReroll)
    {
        if (rerollsRemaining <= 0 || currentState != RunState.Drafting || diceToReroll.Count == 0) return;

        rerollsRemaining--;
        foreach (DieData die in diceToReroll)
        {
            die.Roll();
        }

        WordUIManager.Instance.RefreshFixedBoard(DiceDeck.Instance.currentHand);
        UpdateHUD();
    }

    // 1. THE SUBMISSION ENTRY POINT
    public void SubmitWord(string spelledWord, List<DieData> usedDice)
    {
        // 1. Sanitize the input
        spelledWord = System.Text.RegularExpressions.Regex.Replace(spelledWord.ToUpper(), @"[^A-Z\'\-]", "");

        if (currentState != RunState.Drafting) return;

        // 2. Reject gibberish without spending a turn
        if (!WordValidator.Instance.IsValidWord(spelledWord))
        {
            WordUIManager.Instance.ShowTransientMessage($"<color=#FF0000>INVALID STRING: {spelledWord}</color>");
            WordUIManager.Instance.ReturnLettersToHand();
            return;
        }

        currentState = RunState.Resolving;
        WordValidator.Instance.BurnWord(spelledWord);
        totalWordsEntered++;

        foreach (char c in spelledWord.ToUpper())
        {
            if (letterUsageCount.ContainsKey(c)) letterUsageCount[c]++;
            else letterUsageCount[c] = 1;
        }

        // Inside SubmitWord...
        float wordMultiplierFromDice = 1.0f;
        int totalBaseScore = 0;

        foreach (DieData die in usedDice)
        {
            die.currentFace.playedThisLevel = true;
            die.currentFace.timesPlayedThisRun++;

            float letterScore = die.currentFace.GetBaseLetterScore(die.currentFace.faceText);

            switch (die.currentFace.specialEffect)
            {
                case DieEffectType.LetterMultiplier: letterScore *= die.currentFace.effectValue; break;
                case DieEffectType.WordMultiplier: wordMultiplierFromDice *= die.currentFace.effectValue; break;
                case DieEffectType.HealPlayer:
                    currentDamageDealt = System.Math.Max(0, currentDamageDealt - die.currentFace.effectValue);
                    WordUIManager.Instance.ShowTransientMessage($"<color=#00FF00>SEAL MENDED: {die.currentFace.effectValue} HP</color>");
                    break;
                case DieEffectType.RefundQuery:
                    queriesRemaining += (int)die.currentFace.effectValue;
                    WordUIManager.Instance.ShowTransientMessage($"<color=#00FFFF>FOCUS REFUNDED!</color>");
                    break;
            }
            totalBaseScore += Mathf.RoundToInt(letterScore);
        }

        // 6. Push to Math
        if (LexiconDatabase.Instance != null && LexiconDatabase.Instance.TryGetWordData(spelledWord, out LexiconDatabase.CachedWordData cachedData))
        {
            ExecuteDamageMath(spelledWord, new List<string> { cachedData.pos }, cachedData.tags, totalBaseScore, cachedData.hits, cachedData.frequency, usedDice, wordMultiplierFromDice);
        }
        else
        {
            WordUIManager.Instance.ShowTransientMessage($"<color=#FF8800>UNSEALED KNOWLEDGE.\nCONSULTING THE ARCHIVES...</color>");
            StartCoroutine(FetchExternalDataAndExecute(spelledWord, totalBaseScore, usedDice, wordMultiplierFromDice));
        }
    }

    // 2. THE API FETCH COROUTINE (FIXED SIGNATURE)
    private IEnumerator FetchExternalDataAndExecute(string word, int baseScore, List<DieData> usedDice, float dieWordMultiplier)
    {
        yield return StartCoroutine(DatamuseAPI.Instance.GetWordData(word, (isSuccess, hits, freq, posList, tags) =>
        {
            if (isSuccess)
            {
                if (LexiconDatabase.Instance != null) LexiconDatabase.Instance.CacheWordData(word, hits, freq, string.Join(",", posList), tags);

                // Passes the multiplier to the final math step
                ExecuteDamageMath(word, posList, tags, baseScore, hits, freq, usedDice, dieWordMultiplier);
            }
            else
            {
                WordUIManager.Instance.LogError("ARCHIVE LOOKUP FAILED. USING BASE VALUES.");
                ExecuteDamageMath(word, new List<string> { "noun" }, "", baseScore, 1000, 1.0f, usedDice, dieWordMultiplier);
            }
        }));
    }

    // 3. THE FINAL MATH CALCULATION (FIXED SIGNATURE)
    private void ExecuteDamageMath(string word, List<string> posList, string tags, int baseScore, long rawHits, float frequency, List<DieData> usedDice, float dieWordMultiplier)
    {
        ScoreBreakdown bd = new ScoreBreakdown
        {
            word = word,
            pos = string.Join(", ", posList).ToUpper(),
            tags = tags,
            baseScore = baseScore,
            finalBaseScore = baseScore,
            hits = rawHits,
            finalHits = rawHits,
            datamuseFrequency = frequency,
            usedDice = usedDice,
            globalMult = 1.0
        };

        // Log the dice faces to the dashboard without the Overload colors
        foreach (DieData die in usedDice)
        {
            bd.baseLogs.Add($"[{die.currentFace.faceText}]");
        }

        if (activeBossModifier == BossModifier.Cipher)
        {
            bd.finalBaseScore = word.Length;
            bd.baseLogs.Add("<color=#FF0000>CURSE: Base = Length</color>");
        }

        if (activeBossModifier != BossModifier.Virus)
        {
            foreach (RelicSO relic in activeRelics) relic.OnPreMath(bd);
        }

        bd.hitMultiplier = Mathf.Max(1f, Mathf.Pow(bd.finalHits, settings.hitPowerScaling));

        if (bd.datamuseFrequency == 0) { bd.isAnomaly = true; bd.rarityMult = settings.anomalyMultiplier; bd.rarityLogs.Add($"<color=#FF00FF>FORBIDDEN TEXT: x{bd.rarityMult:F2}</color>"); }
        else if (bd.datamuseFrequency <= 1.0f) { bd.rarityMult = settings.ultraRareMultiplier; bd.rarityLogs.Add($"<color=#FF8800>Mythic Data: x{bd.rarityMult:F2}</color>"); }
        else if (bd.datamuseFrequency <= 10.0f) { bd.rarityMult = settings.rareMultiplier; bd.rarityLogs.Add($"<color=#FFFF00>Rare Text: x{bd.rarityMult:F2}</color>"); }
        else { bd.rarityMult = settings.commonMultiplier; bd.rarityLogs.Add($"<color=#AAAAAA>Common Prose: x{bd.rarityMult:F2}</color>"); }

        bd.lengthMult = Mathf.Max(1.0f, runLengthMultipliers[Mathf.Clamp(word.Length, 0, runLengthMultipliers.Length - 1)]);
        bd.globalMult *= bd.lengthMult;
        if (bd.lengthMult > 1.0f) bd.globalLogs.Add($"<color=#FF8800>x{bd.lengthMult:F2} Length Bonus</color>");

        if (dieWordMultiplier > 1.0f)
        {
            bd.globalMult *= dieWordMultiplier;
            bd.globalLogs.Add($"<color=#FFD700>x{dieWordMultiplier:F2} Enchanted Letters!</color>");
        }

        if (activeBossModifier != BossModifier.Virus)
        {
            foreach (RelicSO relic in activeRelics) relic.OnPostMath(bd);
        }

        // ==========================================
        // STREAK CALCULATIONS
        // ==========================================
        if (_lastWordLength > 0)
        {
            if (word.Length == _lastWordLength) { echoStreak++; chaosStreak = 0; }
            else { chaosStreak++; echoStreak = 0; }

            if (bd.pos == _lastWordPOS && !string.IsNullOrEmpty(bd.pos)) { syntaxStreak++; diversityStreak = 0; }
            else { diversityStreak++; syntaxStreak = 0; }
        }
        else
        {
            echoStreak = 1; chaosStreak = 1; syntaxStreak = 1; diversityStreak = 1;
        }

        // ==========================================
        // APPLY STREAK MULTIPLIERS
        // ==========================================
        float streakBonus = 0f;

        if (echoStreak > 1) { streakBonus += (echoStreak - 1) * 0.2f; bd.globalLogs.Add($"<color=#00FFFF>ECHO STREAK {echoStreak} (+{streakBonus}x)</color>"); }
        else if (chaosStreak > 1) { streakBonus += (chaosStreak - 1) * 0.2f; bd.globalLogs.Add($"<color=#FF5500>CHAOS STREAK {chaosStreak} (+{streakBonus}x)</color>"); }

        if (syntaxStreak > 1) { float posBonus = (syntaxStreak - 1) * 0.2f; streakBonus += posBonus; bd.globalLogs.Add($"<color=#00FF00>SYNTAX STREAK {syntaxStreak} (+{posBonus}x)</color>"); }
        else if (diversityStreak > 1) { float posBonus = (diversityStreak - 1) * 0.2f; streakBonus += posBonus; bd.globalLogs.Add($"<color=#FF00FF>DIVERSITY STREAK {diversityStreak} (+{posBonus}x)</color>"); }

        bd.globalMult += streakBonus;

        _lastWordLength = word.Length;
        _lastWordPOS = bd.pos;

        bd.totalDamage = (double)bd.finalBaseScore * bd.hitMultiplier * bd.rarityMult * bd.globalMult;

        LexiconSaveManager.Instance.RecordWordPlay(word, bd.totalDamage, out bd.isNewHighScore);
        if (bd.isNewHighScore) bd.globalLogs.Add("<color=#FF00FF>*** NEW RECORD! ***</color>");

        if (ArchiveManager.Instance != null && bd.isNewWord) ArchiveManager.Instance.OnNewWordDiscovered(word);

        if (bd.totalDamage > highestScore) { highestScore = bd.totalDamage; highestScoringWord = word; }

        EvaluateUnlocks(bd);
        UpdateHUD();
        StartCoroutine(WordUIManager.Instance.AnimateScoringSequence(bd));
    }


    private void EvaluateUnlocks(ScoreBreakdown lastWord)
    {
        if (lastWord.word.Length >= 7) LexiconSaveManager.Instance.UnlockRelic("UNLOCK_NOVELIST", "The Novelist");
    }

    public void ResolveSubmission(ScoreBreakdown bd)
    {
        queriesRemaining--;
        totalRunScore += bd.totalDamage;
        currentDamageDealt += bd.totalDamage;

        foreach (DieData die in bd.usedDice)
        {
            if (!_dicePlayedThisLevel.Contains(die)) _dicePlayedThisLevel.Add(die);
            DiceDeck.Instance.ReturnToBag(die); // Just throw it in the bag!
        }

        UpdateHUD();

        if (currentDamageDealt >= targetFirewallHP) TriggerLevelUp(); // Or TriggerVictory, based on your loop
        else if (queriesRemaining <= 0) TriggerDefeat();
        else StartTurn();
    }

    private void TriggerLevelUp()
    {
        firewallsBreached++;
        currentLevel++;
        double overkill = currentDamageDealt - targetFirewallHP;
        
        int dustEarned = 100 + (queriesRemaining * 10); // Base gold reward
        currentCredits += dustEarned;

        // ==========================================
        // NEW: QUERY OVERCLOCK MECHANIC
        // ==========================================
        int upgradesGranted = 0;
        string upgradeLogs = "";

        for (int i = 0; i < queriesRemaining; i++)
        {
            if (_dicePlayedThisLevel.Count > 0)
            {
                // Pick a random die played this level
                DieData randomDie = _dicePlayedThisLevel[UnityEngine.Random.Range(0, _dicePlayedThisLevel.Count)];
                
                // Pick a random face on that die to permanently upgrade!
                DieFace randomFace = randomDie.faces[UnityEngine.Random.Range(0, randomDie.faces.Count)];
                randomFace.bonusScore += 1;
                
                upgradesGranted++;
                upgradeLogs += $"[{randomFace.faceText}] ";
            }
        }

        // Grant the base survival Focus back
        queriesRemaining = 2; 

        GenerateNextFirewall();
        currentDamageDealt = overkill;

        // Display the rewards!
        string message = $"<size=120%><color=#FFD700>SEAL BREACHED! CHAPTER {currentLevel}</color></size>\n";
        message += $"<color=#00FF00>+{dustEarned} Dust</color>\n";
        
        if (upgradesGranted > 0)
        {
            message += $"<color=#00FFFF>OVERCLOCKED {upgradesGranted}x: {upgradeLogs}</color>";
        }

        WordUIManager.Instance.ShowTransientMessage(message);

        // Reset the tracking list for the next level
        _dicePlayedThisLevel.Clear();

        UpdateHUD();
        StartTurn(); 
    }

    public void GenerateNextFirewall()
    {
        // 1. Infinite Scaling Check
        if (currentLevel > settings.maxLevel)
        {
            settings.maxLevel += 50;
        }

        // 2. Boss Modifier Logic
        isBossLevel = currentLevel % 5 == 0;
        activeBossModifier = BossModifier.None;
        activeBossDescription = "";

        // 3. HP Math
        double hpCalc = settings.baseFirewallHP * Mathf.Pow(settings.hpScaleMultiplier, currentLevel - 1);
        if (activeCharacter != null) hpCalc -= activeCharacter.bonusStartingHP;

        if (isBossLevel)
        {
            activeBossModifier = (BossModifier)UnityEngine.Random.Range(1, 5);
            switch (activeBossModifier)
            {
                case BossModifier.Titan: hpCalc *= settings.titanBossHpMultiplier; activeBossDescription = "TITAN: Massive HP pool."; break;
                case BossModifier.Cipher: hpCalc *= settings.standardBossHpMultiplier; activeBossDescription = "CIPHER: All letters score 1."; break;
                case BossModifier.Virus: hpCalc *= settings.standardBossHpMultiplier; activeBossDescription = "VIRUS: Relics disabled."; break;

                case BossModifier.Drain:
                    hpCalc *= settings.standardBossHpMultiplier;
                    activeBossDescription = "DRAIN: Focus sapped!";
                    // Dynamic mid-run penalty!
                    queriesRemaining = Mathf.Max(1, queriesRemaining - 2);
                    break;
            }
        }

        targetFirewallHP = System.Math.Max(10, hpCalc);
    }


    private void TriggerDefeat()
    {
        currentState = RunState.Defeat;
        if (GameDirector.Instance != null) GameDirector.Instance.OnCombatDefeat();
    }

    // RESTORED DEFEAT LOGIC!
    public void TriggerDefeatSummary()
    {
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

        string mostMutated = "None";
        var mutList = LexiconSaveManager.Instance.currentData.letterStats.Where(l => l.timesMutated > 0).OrderByDescending(l => l.timesMutated).ToList();
        if (mutList.Count > 0)
            mostMutated = $"{mutList[0].faceText} ({mutList[0].timesMutated}x Lifetime)";

        WordUIManager.Instance.ShowDefeatScreen(currentLevel, firewallsBreached, totalWordsEntered, highestScoringWord, highestScore, mostUniqueWord, lowestWikiHits, top5Letters, leastCommon, mostMutated, dataCoresEarned);
    }

    // ==========================================
    // THE COMBINED MARKET ENGINE
    // ==========================================
    public void GenerateCombinedMarket()
    {
        if (GameDirector.Instance != null) GameDirector.Instance.TransitionToState(GameState.PostCombatShop);

        int bossMultiplier = isBossLevel ? Mathf.Max(1, currentLevel / 5) : 1;
        bool everythingIsFree = isBossLevel;

        List<DraftUpgradeOption> playedOptions = new();
        List<DraftMutateOption> allMutateOptions = new();

        foreach (DieData die in DiceDeck.Instance.allOwnedDice)
        {
            foreach (DieFace face in die.faces)
            {
                if (face.playedThisLevel)
                    playedOptions.Add(new DraftUpgradeOption { die = die, face = face, bonusAmount = bossMultiplier });

                allMutateOptions.Add(new DraftMutateOption { die = die, face = face, newFaceText = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[UnityEngine.Random.Range(0, 26)].ToString(), isSplitFace = isBossLevel });
            }
        }

        var upgrades = playedOptions.OrderBy(x => UnityEngine.Random.value).Take(2).ToList();
        var mutates = allMutateOptions.OrderBy(x => x.face.timesPlayedThisRun).ThenBy(x => UnityEngine.Random.value).Take(2).ToList();

        // THIS HOOKS TO THE SHOP MANAGER
        List<ShopItem> premiumItems = ShopManager.Instance.GetMarketItems(everythingIsFree);

        WordUIManager.Instance.ShowCombinedMarket(upgrades, mutates, premiumItems, everythingIsFree);
    }

    public void ApplyDraftUpgrade(DraftUpgradeOption opt)
    {
        opt.face.bonusScore += opt.bonusAmount;
        AdvanceFromMarket();
    }

    public void ApplyDraftMutation(DraftMutateOption opt)
    {
        LexiconSaveManager.Instance.RecordMutation(opt.face.faceText);

        if (opt.isSplitFace) opt.face.altFaceText = opt.newFaceText;
        else opt.face.faceText = opt.newFaceText;

        AdvanceFromMarket();
    }

    public void AdvanceFromMarket()
    {
        currentLevel++;
        if (GameDirector.Instance != null) GameDirector.Instance.TransitionToState(GameState.Combat);
    }

    private void UpdateHUD()
    {
        if (WordUIManager.Instance != null && settings != null)
            WordUIManager.Instance.UpdateRunStats(currentLevel, settings.maxLevel, currentDamageDealt, targetFirewallHP, queriesRemaining, discardsRemaining, rerollsRemaining, currentCredits, activeRelics);
    }
}