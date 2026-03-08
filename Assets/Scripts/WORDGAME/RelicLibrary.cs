using System.Collections.Generic;
using UnityEngine;

public enum Relic
{
    // Parts of Speech
    NounOverclock, VerbDrive, AdjectiveArray, AdverbAccelerator,
    // Letters & Composition
    VowelBattery, ConsonantCruncher, DoubleVision, QwertyVirus,
    // Word Length
    ShortCircuit, FourLetterWord, TheLongCon,
    // Affixes & Structure
    Pluralizer, GerundEngine, PrefixProtocol, PalindromeProtocol,
    // Database Meta
    TomeSkimmer, MainstreamInjector, HipsterCache,
    // Game State
    LastResort, FirstStrike
}

public class RelicData
{
    public Relic ID;
    public string Name;
    public string Description;
    public int Cost;
}

public class RelicLibrary : MonoBehaviour
{
    public static RelicLibrary Instance { get; private set; }

    public readonly Dictionary<Relic, RelicData> AllRelics = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeLibrary();
    }

    private void InitializeLibrary()
    {
        // PARTS OF SPEECH
        AllRelics.Add(Relic.NounOverclock, new RelicData { ID = Relic.NounOverclock, Name = "Noun Overclock", Cost = 300, Description = "Nouns multiply final damage by 1.5x." });
        AllRelics.Add(Relic.VerbDrive, new RelicData { ID = Relic.VerbDrive, Name = "Verb Drive", Cost = 350, Description = "Verbs gain a flat +5,000,000 Global Hits." });
        AllRelics.Add(Relic.AdjectiveArray, new RelicData { ID = Relic.AdjectiveArray, Name = "Adjective Array", Cost = 300, Description = "Adjectives grant +2 Base Score for every letter in the word." });
        AllRelics.Add(Relic.AdverbAccelerator, new RelicData { ID = Relic.AdverbAccelerator, Name = "Adverb Accelerator", Cost = 400, Description = "Adverbs multiply the Tome Size by 2." });

        // LETTERS & COMPOSITION
        AllRelics.Add(Relic.VowelBattery, new RelicData { ID = Relic.VowelBattery, Name = "Vowel Battery", Cost = 250, Description = "Every vowel (A,E,I,O,U) grants +2 Base Score." });
        AllRelics.Add(Relic.ConsonantCruncher, new RelicData { ID = Relic.ConsonantCruncher, Name = "Consonant Cruncher", Cost = 500, Description = "Words with 4 or more consonants in a row multiply damage by 2x." });
        AllRelics.Add(Relic.DoubleVision, new RelicData { ID = Relic.DoubleVision, Name = "Double Vision", Cost = 450, Description = "Words containing double letters (e.g. BOOK) multiply damage by 2x." });
        AllRelics.Add(Relic.QwertyVirus, new RelicData { ID = Relic.QwertyVirus, Name = "QWERTY Virus", Cost = 600, Description = "Words containing Q, Z, J, or X multiply damage by 3x." });

        // WORD LENGTH
        AllRelics.Add(Relic.ShortCircuit, new RelicData { ID = Relic.ShortCircuit, Name = "Short Circuit", Cost = 200, Description = "Exactly 3-letter words gain +5,000,000 Global Hits." });
        AllRelics.Add(Relic.FourLetterWord, new RelicData { ID = Relic.FourLetterWord, Name = "Four-Letter Word", Cost = 400, Description = "Exactly 4-letter words multiply Base Score by 3." });
        AllRelics.Add(Relic.TheLongCon, new RelicData { ID = Relic.TheLongCon, Name = "The Long Con", Cost = 500, Description = "Words 7+ letters long gain +10 Base Score and multiply damage by 1.5x." });

        // AFFIXES
        AllRelics.Add(Relic.Pluralizer, new RelicData { ID = Relic.Pluralizer, Name = "Pluralizer", Cost = 250, Description = "Words ending in 'S' gain +2,000,000 Global Hits." });
        AllRelics.Add(Relic.GerundEngine, new RelicData { ID = Relic.GerundEngine, Name = "Gerund Engine", Cost = 300, Description = "Words ending in 'ING' multiply Tome Size by 1.5." });
        AllRelics.Add(Relic.PrefixProtocol, new RelicData { ID = Relic.PrefixProtocol, Name = "Prefix Protocol", Cost = 350, Description = "Words starting with 'RE' or 'UN' multiply Base Score by 2." });
        AllRelics.Add(Relic.PalindromeProtocol, new RelicData { ID = Relic.PalindromeProtocol, Name = "Palindrome Protocol", Cost = 800, Description = "Palindromes multiply damage by 5x." });

        // DATABASE META
        AllRelics.Add(Relic.TomeSkimmer, new RelicData { ID = Relic.TomeSkimmer, Name = "Tome Skimmer", Cost = 400, Description = "If the top article has < 500 words, multiply damage by 3x." });
        AllRelics.Add(Relic.MainstreamInjector, new RelicData { ID = Relic.MainstreamInjector, Name = "Mainstream Injector", Cost = 300, Description = "If Global Hits > 50,000,000, add +15 Base Score." });
        AllRelics.Add(Relic.HipsterCache, new RelicData { ID = Relic.HipsterCache, Name = "Hipster Cache", Cost = 600, Description = "If Global Hits < 1,000,000, multiply damage by 3x." });

        // GAME STATE
        AllRelics.Add(Relic.LastResort, new RelicData { ID = Relic.LastResort, Name = "Last Resort", Cost = 500, Description = "If played on your final Query, multiply damage by 3x." });
        AllRelics.Add(Relic.FirstStrike, new RelicData { ID = Relic.FirstStrike, Name = "First Strike", Cost = 400, Description = "If played on your first Query of a level, multiply damage by 2x." });
    }
}