using UnityEngine;
using System.Collections.Generic;

// ==========================================
// LEXICON BREACH - OFFLINE DATABASE
// ==========================================
// Loads the raw Norvig text data into RAM for instant, zero-lag scoring.

public class LexiconDatabase : MonoBehaviour
{
    public static LexiconDatabase Instance { get; private set; }

    [Header("Data Source")]
    [Tooltip("Drag the count_1w.txt file here!")]
    public TextAsset rawTextDatabase;

    [Tooltip("How many words to load into RAM? (10,000 - 50,000 recommended to prevent load lag)")]
    public int wordsToLoad = 15000;

    // O(1) Lookup speed dictionary for instant math
    private Dictionary<string, CachedWordData> memoryBank = new Dictionary<string, CachedWordData>();

    [System.Serializable]
    public class CachedWordData
    {
        public string word;
        public long hits;
        public float frequency;
        public string pos;
        public string tags;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadOfflineDatabase();
    }

    private void LoadOfflineDatabase()
    {
        if (rawTextDatabase != null)
        {
            // Split the text file by line breaks
            string[] lines = rawTextDatabase.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            int loadedCount = 0;

            foreach (string line in lines)
            {
                if (loadedCount >= wordsToLoad) break;

                // Norvig's file is separated by tabs: "THE    23135851162"
                string[] parts = line.Split('\t');
                if (parts.Length >= 2)
                {
                    string word = parts[0].ToUpper();
                    if (long.TryParse(parts[1], out long hits))
                    {
                        // Filter out pure numbers and weird punctuation
                        bool isAlpha = true;
                        foreach (char c in word) { if (!char.IsLetter(c)) { isAlpha = false; break; } }

                        // Allow single letters A and I, but block others
                        if (isAlpha && (word.Length > 1 || word == "A" || word == "I"))
                        {
                            // Datamuse Frequency is roughly (Norvig Hits / 1 Million)
                            float estimatedFreq = hits / 1000000f;

                            memoryBank[word] = new CachedWordData
                            {
                                word = word,
                                hits = hits,
                                frequency = estimatedFreq,
                                pos = "UNKNOWN", // We don't have this from Norvig, so we use a placeholder
                                tags = "STANDARD DATA"
                            };
                            loadedCount++;
                        }
                    }
                }
            }
            Debug.Log($"<color=#00FF00>Lexicon Database Booted: {memoryBank.Count} offline words loaded into RAM.</color>");
        }
        else
        {
            Debug.LogWarning("No offline database assigned! All words will be forced to ping the external APIs.");
        }
    }

    // Called by RunManager to instantly get math data
    public bool TryGetWordData(string word, out CachedWordData data)
    {
        return memoryBank.TryGetValue(word.ToUpper(), out data);
    }

    // If the player pings the API for a weird word, we cache it here
    // so if they play it again in the same run, it's instant!
    public void CacheWordData(string word, long hits, float frequency, string pos, string tags)
    {
        CachedWordData newData = new CachedWordData
        {
            word = word.ToUpper(),
            hits = hits,
            frequency = frequency,
            pos = pos,
            tags = tags
        };
        memoryBank[word.ToUpper()] = newData;
    }
}