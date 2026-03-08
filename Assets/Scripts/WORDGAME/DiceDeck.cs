using System.Collections.Generic;
using UnityEngine;

public enum DiceType { D4_Vowel, D6_Standard, D8_Consonant, D20_Rare }

public class DieData
{
    public DiceType Type;
    public char CurrentFace;
    public string PossibleFaces;
    public int ScoreValue;
}

public class DiceDeck : MonoBehaviour
{
    public static DiceDeck Instance { get; private set; }

    private readonly char[] _d4Vowels = { 'A', 'E', 'I', 'O' };
    private readonly char[] _d6Standard = { 'A', 'E', 'S', 'T', 'R', 'N' };
    private readonly char[] _d8Consonant = { 'C', 'D', 'H', 'L', 'M', 'P', 'R', 'T' };
    private readonly char[] _d20Rare = { 'B', 'F', 'G', 'J', 'K', 'Q', 'V', 'W', 'X', 'Y', 'Z', 'B', 'F', 'H', 'M', 'P', 'V', 'W', 'Y', 'Z' };

    public readonly List<DiceType> startingDeckBlueprint = new();

    public readonly List<DiceType> currentDrawPile = new();
    public readonly List<DieData> currentHand = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void InitializeStartingDeck()
    {
        startingDeckBlueprint.Clear();
        for (int i = 0; i < 8; i++) startingDeckBlueprint.Add(DiceType.D4_Vowel);
        for (int i = 0; i < 12; i++) startingDeckBlueprint.Add(DiceType.D6_Standard);
        for (int i = 0; i < 8; i++) startingDeckBlueprint.Add(DiceType.D8_Consonant);
        for (int i = 0; i < 2; i++) startingDeckBlueprint.Add(DiceType.D20_Rare);
    }

    public void SetupEncounterPool()
    {
        currentDrawPile.Clear();
        currentHand.Clear();
        currentDrawPile.AddRange(startingDeckBlueprint);
        ShuffleDrawPile();
    }

    private void ShuffleDrawPile()
    {
        for (int i = 0; i < currentDrawPile.Count; i++)
        {
            DiceType temp = currentDrawPile[i];
            int randomIndex = Random.Range(i, currentDrawPile.Count);
            currentDrawPile[i] = currentDrawPile[randomIndex];
            currentDrawPile[randomIndex] = temp;
        }
    }

    public void FillHand(int maxHandSize)
    {
        while (currentHand.Count < maxHandSize && currentDrawPile.Count > 0)
        {
            DiceType nextDie = currentDrawPile[0];
            currentDrawPile.RemoveAt(0);
            currentHand.Add(RollDie(nextDie));
        }
    }

    private DieData RollDie(DiceType type)
    {
        char face;
        string facesStr;

        switch (type)
        {
            case DiceType.D4_Vowel: face = _d4Vowels[Random.Range(0, _d4Vowels.Length)]; facesStr = string.Join(",", _d4Vowels); break;
            case DiceType.D8_Consonant: face = _d8Consonant[Random.Range(0, _d8Consonant.Length)]; facesStr = string.Join(",", _d8Consonant); break;
            case DiceType.D20_Rare: face = _d20Rare[Random.Range(0, _d20Rare.Length)]; facesStr = string.Join(",", _d20Rare); break;
            default: face = _d6Standard[Random.Range(0, _d6Standard.Length)]; facesStr = string.Join(",", _d6Standard); break;
        }

        // CIPHER BOSS FIX: Check if Cipher is active, if so, force score to 1.
        int scoreVal = WordValidator.Instance.GetLetterScore(face);
        if (RunManager.Instance != null && RunManager.Instance.activeBossModifier == BossModifier.Cipher)
        {
            scoreVal = 1;
        }

        return new DieData
        {
            Type = type,
            CurrentFace = face,
            PossibleFaces = facesStr,
            ScoreValue = scoreVal
        };
    }

    public string GetDeckSummary()
    {
        int d4 = 0, d6 = 0, d8 = 0, d20 = 0;
        foreach (var d in startingDeckBlueprint)
        {
            if (d == DiceType.D4_Vowel) d4++;
            else if (d == DiceType.D6_Standard) d6++;
            else if (d == DiceType.D8_Consonant) d8++;
            else if (d == DiceType.D20_Rare) d20++;
        }
        return $"Total Dice: {startingDeckBlueprint.Count}\n\n- D4 (Vowels): {d4}\n- D6 (Standard): {d6}\n- D8 (Consonants): {d8}\n- D20 (Rare): {d20}";
    }
}