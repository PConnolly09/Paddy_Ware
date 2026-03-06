using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WordValidator : MonoBehaviour
{
    public static WordValidator Instance { get; private set; }

    [Header("Dictionary Source")]
    public TextAsset dictionaryFile;

    private readonly HashSet<string> _validWords = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _burnedWordsThisRun = new(System.StringComparer.OrdinalIgnoreCase);

    // Standard Scrabble letter values
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

        LoadDictionary();
    }

    private void LoadDictionary()
    {
        if (dictionaryFile == null) return;
        string[] lines = dictionaryFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines) _validWords.Add(line.Trim().ToUpper());
    }

    public bool IsValidWord(string word)
    {
        string upper = word.ToUpper();
        if (_validWords.Count == 0) return true; // Fallback for testing without a file
        return _validWords.Contains(upper);
    }

    public bool IsWordBurned(string word)
    {
        return _burnedWordsThisRun.Contains(word.ToUpper());
    }

    public void BurnWord(string word) { _burnedWordsThisRun.Add(word.ToUpper()); }
    public void ResetBurnedWordsForNewRun() { _burnedWordsThisRun.Clear(); }
    public List<string> GetBurnedWordsList() { return _burnedWordsThisRun.ToList(); }

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