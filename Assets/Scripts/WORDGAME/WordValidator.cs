using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WordValidator : MonoBehaviour
{
    public static WordValidator Instance { get; private set; }

    [Header("Validation Data")]
    [Tooltip("Drag a standard valid word list text file here (e.g., Scrabble TWL06.txt)")]
    public TextAsset dictionaryFile;

    // HashSet provides O(1) instant lookup times for tens of thousands of words
    private readonly HashSet<string> _validWords = new(System.StringComparer.OrdinalIgnoreCase);
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

        LoadDictionary();
    }

    /// <summary>
    /// Loads the master list of valid words into memory for instant validation.
    /// Safely handles both clean word lists and frequency lists (like Norvig's count_1w.txt).
    /// </summary>
    private void LoadDictionary()
    {
        if (dictionaryFile == null)
        {
            Debug.LogWarning("No Dictionary File assigned to WordValidator! Validation is running in failsafe mode.");
            return;
        }

        string[] lines = dictionaryFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string cleanWord = line.Trim();

            // If the file is tab-separated or space-separated (like count_1w.txt), grab ONLY the word
            if (cleanWord.Contains('\t'))
            {
                cleanWord = cleanWord.Split('\t')[0];
            }
            else if (cleanWord.Contains(' '))
            {
                cleanWord = cleanWord.Split(' ')[0];
            }

            // Optional: Ensure no numbers snuck in
            bool isAlpha = true;
            foreach (char c in cleanWord) { if (!char.IsLetter(c)) { isAlpha = false; break; } }

            if (isAlpha && cleanWord.Length > 0)
            {
                _validWords.Add(cleanWord.ToUpper());
            }
        }
        Debug.Log($"<color=#00FF00>WordValidator Online: {_validWords.Count} valid words secured in memory.</color>");
    }

    /// <summary>
    /// Checks if a spelled string exists in the official valid word list.
    /// </summary>
    public bool IsValidWord(string word)
    {
        // Failsafe: if the developer forgot to attach the text file, allow everything to prevent softlocking the game
        if (_validWords.Count == 0) return true;

        return _validWords.Contains(word.ToUpper());
    }

    /// <summary>
    /// Checks if the word has already reached its play limit for the current run.
    /// </summary>
    public bool IsWordBurned(string word)
    {
        string upper = word.ToUpper();
        _wordsPlayedThisRun.TryGetValue(upper, out int playsThisRun);

        int allowedPlays = 1;
        if (LexiconSaveManager.Instance != null && LexiconSaveManager.Instance.GetWordPlayCount(upper) >= LexiconSaveManager.Instance.favoriteThreshold)
        {
            allowedPlays = 2; // Favorite words can be played twice!
        }

        return playsThisRun >= allowedPlays;
    }

    /// <summary>
    /// Records a word as played during the current run.
    /// </summary>
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