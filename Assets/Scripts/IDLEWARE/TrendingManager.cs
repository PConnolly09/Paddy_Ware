using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TrendingManager : MonoBehaviour
{
    [Header("Trending Suggestions")]
    public List<string> currentTrendingKeywords = new List<string>();

    void Start()
    {
        // Fetch trending topics when the game starts
        StartCoroutine(FetchTrendingTopics());
    }

    private IEnumerator FetchTrendingTopics()
    {
        // We use the /r/popular endpoint to see what the entire internet is talking about right now
        string url = "https://www.reddit.com/r/popular.json?limit=10";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // --- THE FIX IS HERE ---
            // Set a custom User-Agent so Reddit knows we aren't a malicious spam bot.
            // Change "yourname" and "your_reddit_username" to your actual info!
            webRequest.SetRequestHeader("User-Agent", "android:com.patconnolly.idlecrawler:v0.1 (by /u/alcindorthebutcher)");
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;

                // We reuse the RedditResponse classes we built earlier!
                RedditResponse parsedData = JsonUtility.FromJson<RedditResponse>(jsonResponse);

                ExtractKeywordsFromFrontPage(parsedData);
            }
            else
            {
                Debug.LogWarning("Could not fetch trending topics.");
            }
        }
    }

    private void ExtractKeywordsFromFrontPage(RedditResponse response)
    {
        currentTrendingKeywords.Clear();
        HashSet<string> uniqueTopics = new HashSet<string>();

        foreach (RedditPost post in response.data.children)
        {
            // The JSON structure for Reddit actually includes a "subreddit" field. 
            // We'll need to add `public string subreddit;` to our RedditPostData class from the previous step!
            // For now, we simulate extracting an interesting word from the title.

            string title = post.data.title;
            string[] words = title.Split(' ');

            // Simple logic: Find the longest word in the top post's title to suggest as a keyword
            string bestWord = "";
            foreach (string word in words)
            {
                // Strip punctuation and check length
                string cleanWord = System.Text.RegularExpressions.Regex.Replace(word, "[^a-zA-Z0-9]", "");
                if (cleanWord.Length > bestWord.Length && cleanWord.Length > 4) // Ignore short words like "the", "and"
                {
                    bestWord = cleanWord.ToLower();
                }
            }

            if (!string.IsNullOrEmpty(bestWord) && !uniqueTopics.Contains(bestWord))
            {
                uniqueTopics.Add(bestWord);
                currentTrendingKeywords.Add(bestWord);
            }

            if (UIManager.Instance != null) UIManager.Instance.UpdateTrendingDisplay(currentTrendingKeywords);
        }

        Debug.Log("--- TRENDING KEYWORDS ---");
        foreach (string keyword in currentTrendingKeywords)
        {
            Debug.Log("HOT " + keyword);
            // TODO: Instantiate a UI button here that says "Track [Keyword]"
        }
    }
}