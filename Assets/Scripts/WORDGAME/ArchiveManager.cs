using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class ArchiveManager : MonoBehaviour
{
    public static ArchiveManager Instance { get; private set; }

    [Header("The Grand Library")]
    public List<ArchiveTomeSO> allTomesInGame;
    public ArchiveTomeSO currentlyTrackedTome;

    // Caches the unique words required for each book so we don't have to calculate it every frame
    private Dictionary<string, HashSet<string>> tomeUniqueWordsCache = new Dictionary<string, HashSet<string>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeTomes();
        UpdateTrackerUI();
    }

    // Parses the massive text blocks into a clean list of required unique words
    private void InitializeTomes()
    {
        if (allTomesInGame == null) return;

        foreach (ArchiveTomeSO tome in allTomesInGame)
        {
            if (tome == null || string.IsNullOrEmpty(tome.fullText)) continue;

            HashSet<string> uniqueWords = new HashSet<string>();

            // Regex magic: Grabs only letters, ignores spaces, periods, commas, etc.
            MatchCollection matches = Regex.Matches(tome.fullText, @"[a-zA-Z]+");
            foreach (Match m in matches)
            {
                uniqueWords.Add(m.Value.ToUpper());
            }

            tomeUniqueWordsCache[tome.tomeID] = uniqueWords;
        }
    }

    // Called by RunManager when the player plays a word for the very first time!
    public void OnNewWordDiscovered(string newWord)
    {
        string wordUpper = newWord.ToUpper();
        bool updateHUD = false;

        foreach (ArchiveTomeSO tome in allTomesInGame)
        {
            if (tome == null) continue;

            // Skip if this tome is already 100% complete
            if (LexiconSaveManager.Instance.currentData.completedTomes.Contains(tome.tomeID)) continue;

            // If this new word belongs to this book...
            if (tomeUniqueWordsCache.ContainsKey(tome.tomeID) && tomeUniqueWordsCache[tome.tomeID].Contains(wordUpper))
            {
                WordUIManager.Instance.ShowTransientMessage($"<color=#FFD700>MANUSCRIPT FRAGMENT RESTORED: {wordUpper}</color>");

                if (currentlyTrackedTome == tome) updateHUD = true;

                CheckTomeCompletion(tome);
            }
        }

        if (updateHUD) UpdateTrackerUI();
    }

    private void CheckTomeCompletion(ArchiveTomeSO tome)
    {
        bool isComplete = true;

        foreach (string w in tomeUniqueWordsCache[tome.tomeID])
        {
            if (LexiconSaveManager.Instance.GetWordPlayCount(w) <= 0)
            {
                isComplete = false;
                break;
            }
        }

        if (isComplete)
        {
            LexiconSaveManager.Instance.currentData.completedTomes.Add(tome.tomeID);

            // Grant rewards!
            LexiconSaveManager.Instance.currentData.dataCores += tome.completionDustReward;
            if (tome.unlockableRelic != null)
            {
                LexiconSaveManager.Instance.UnlockRelic(tome.unlockableRelic.relicID, tome.unlockableRelic.relicName);
            }

            LexiconSaveManager.Instance.SaveGame();
            WordUIManager.Instance.ShowTransientMessage($"<color=#00FFFF>TOME FULLY RESTORED: {tome.tomeTitle.ToUpper()}!</color>");
        }
    }

    public string GetFormattedTomeText(ArchiveTomeSO tome)
    {
        if (tome == null || string.IsNullOrEmpty(tome.fullText)) return "";

        return Regex.Replace(tome.fullText, @"[a-zA-Z]+", match =>
        {
            string wordUpper = match.Value.ToUpper();
            if (LexiconSaveManager.Instance.GetWordPlayCount(wordUpper) > 0)
            {
                return $"<color=#FFFFFF>{match.Value}</color>";
            }
            else
            {
                return $"<color=#555555>{new string('_', match.Value.Length)}</color>";
            }
        });
    }

    public void UpdateTrackerUI()
    {
        if (currentlyTrackedTome != null && WordUIManager.Instance.tomeTrackerText != null)
        {
            if (LexiconSaveManager.Instance.currentData.completedTomes.Contains(currentlyTrackedTome.tomeID))
            {
                WordUIManager.Instance.tomeTrackerText.text = $"<color=#00FF00>TOME RESTORED: {currentlyTrackedTome.tomeTitle}</color>";
                return;
            }

            if (!tomeUniqueWordsCache.ContainsKey(currentlyTrackedTome.tomeID)) return;

            HashSet<string> uniqueWords = tomeUniqueWordsCache[currentlyTrackedTome.tomeID];
            List<string> missingWords = new List<string>();

            foreach (string w in uniqueWords)
            {
                if (LexiconSaveManager.Instance.GetWordPlayCount(w) == 0) missingWords.Add(w);
            }

            int foundCount = uniqueWords.Count - missingWords.Count;
            float percent = uniqueWords.Count > 0 ? ((float)foundCount / uniqueWords.Count) * 100f : 0f;

            var hints = missingWords.OrderBy(x => UnityEngine.Random.value).Take(3).ToList();

            WordUIManager.Instance.tomeTrackerText.text =
                $"<color=#FFD700>TRACKING: {currentlyTrackedTome.tomeTitle} ({percent:F1}%)</color>\n" +
                $"<size=80%>SEEKING: {string.Join(", ", hints)}...</size>";
        }
    }
}