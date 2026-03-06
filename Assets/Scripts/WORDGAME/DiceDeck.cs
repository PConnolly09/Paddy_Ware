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

    private readonly char[] _d4Vowels = { 'A', 'E', 'I', 'O', 'U', 'Y' };
    private readonly char[] _d6Standard = { 'A', 'E', 'S', 'T', 'R', 'N' };
    private readonly char[] _d8Consonant = { 'B', 'C', 'D', 'F', 'G', 'H', 'L', 'M', 'P' };
    private readonly char[] _d20Rare = { 'K', 'V', 'W', 'X', 'Y', 'Z', 'J', 'Q' };

    public readonly List<DiceType> startingDeckBlueprint = new();

    // Active encounter data
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
        // A generous starting deck for the run
        for (int i = 0; i < 8; i++) startingDeckBlueprint.Add(DiceType.D4_Vowel);
        for (int i = 0; i < 12; i++) startingDeckBlueprint.Add(DiceType.D6_Standard);
        for (int i = 0; i < 8; i++) startingDeckBlueprint.Add(DiceType.D8_Consonant);
        for (int i = 0; i < 2; i++) startingDeckBlueprint.Add(DiceType.D20_Rare);
    }

    public void SetupEncounterPool()
    {
        currentDrawPile.Clear();
        currentHand.Clear();

        // Copy the blueprint into the draw pile and shuffle it
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

            DieData newDie = RollDie(nextDie);
            currentHand.Add(newDie);
        }
    }

    private DieData RollDie(DiceType type)
    {
        char face;
        string facesStr;

        switch (type)
        {
            case DiceType.D4_Vowel:
                face = _d4Vowels[Random.Range(0, _d4Vowels.Length)];
                facesStr = string.Join(",", _d4Vowels);
                break;
            case DiceType.D8_Consonant:
                face = _d8Consonant[Random.Range(0, _d8Consonant.Length)];
                facesStr = string.Join(",", _d8Consonant);
                break;
            case DiceType.D20_Rare:
                face = _d20Rare[Random.Range(0, _d20Rare.Length)];
                facesStr = string.Join(",", _d20Rare);
                break;
            default:
                face = _d6Standard[Random.Range(0, _d6Standard.Length)];
                facesStr = string.Join(",", _d6Standard);
                break;
        }

        return new DieData
        {
            Type = type,
            CurrentFace = face,
            PossibleFaces = facesStr,
            ScoreValue = WordValidator.Instance.GetLetterScore(face)
        };
    }
}