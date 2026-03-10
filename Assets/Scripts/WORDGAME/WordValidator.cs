using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WordValidator : MonoBehaviour
{
    public static WordValidator Instance { get; private set; }

    // Tracks how many times a word was played THIS run to check against burn limits
    private readonly Dictionary<string, int> _wordsPlayedThisRun = new(System.StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<char, int> _letterScores = new()
    {
        {'A', 1}, {'B', 3}, {'C', 3}, {'D', 2}, {'E', 1}, {'F', 4}, {'G', 2}, {'H', 4},
        {'I', 1}, {'J', 8}, {'K', 5}, {'L', 1}, {'M', 3}, {'N', 1}, {'O', 1}, {'P', 3},
        {'Q', 10}, {'R', 1}, {'S', 1}, {'T', 1}, {'U', 1}, {'V', 4}, {'W', 4}, {'X', 8},
        {'Y', 4}, {'Z', 10}
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool IsWordBurned(string word)
    {
        string upper = word.ToUpper();

        _wordsPlayedThisRun.TryGetValue(upper, out int playsThisRun);

        int allowedPlays = 1;
        if (LexiconSaveManager.Instance != null && LexiconSaveManager.Instance.GetWordPlayCount(upper) >= LexiconSaveManager.Instance.favoriteThreshold)
        {
            allowedPlays = 2;
        }

        return playsThisRun >= allowedPlays;
    }

    public void BurnWord(string word)
    {
        string upper = word.ToUpper();
        if (_wordsPlayedThisRun.ContainsKey(upper)) _wordsPlayedThisRun[upper]++;
        else _wordsPlayedThisRun[upper] = 1;
    }

    public void ResetBurnedWordsForNewRun() { _wordsPlayedThisRun.Clear(); }

    public List<string> GetBurnedWordsList()
    {
        return _wordsPlayedThisRun.Select(kvp => $"{kvp.Key} (Played {kvp.Value}x)").ToList();
    }

    public int GetLetterScore(char c)
    {
        if (_letterScores.TryGetValue(char.ToUpper(c), out int val)) return val;
        return 0;
    }

    public int CalculateBaseScore(string word)
    {
        int total = 0;
        foreach (char c in word) total += GetLetterScore(c);
        return total;
    }
}