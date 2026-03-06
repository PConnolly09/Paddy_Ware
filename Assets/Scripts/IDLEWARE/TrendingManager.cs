using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Text.RegularExpressions;

public class TrendingManager : MonoBehaviour
{
    public static TrendingManager Instance { get; private set; }

    [Header("Settings")]
    public float refreshIntervalSeconds = 60f;

    [Header("Trending Feed UI")]
    public Transform trendingListContainer;
    public GameObject trendingButtonPrefab;

    [Header("Keyword Detail Panel UI")]
    public GameObject detailPanel;
    public TextMeshProUGUI detailTitleText;
    public TextMeshProUGUI detailHeadlinesText;
    public TextMeshProUGUI detailStatsText;
    public TextMeshProUGUI detailCostText;

    [Header("Subreddit Search UI")]
    public TMP_InputField subredditInputField;
    public Button searchSubredditButton;

    private class WordData
    {
        public int Score;
        public int Rank;
    }

    private readonly Dictionary<string, WordData> _wordMemory = new();

    private string _currentlySelectedKeyword = "";
    private int _currentlySelectedCost = 0;
    private string _currentSubreddit = "popular"; // Default target

    private class KeywordTrend
    {
        public string Keyword;
        public int Delta;
        public int Status;
        public int Score;
        public int PrevRank;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (detailPanel != null) detailPanel.SetActive(false);

        if (searchSubredditButton != null && subredditInputField != null)
        {
            searchSubredditButton.onClick.AddListener(() => SetTargetSubreddit(subredditInputField.text));
        }

        StartCoroutine(TrendingLoop());
    }

    public void SetTargetSubreddit(string newSub)
    {
        if (string.IsNullOrWhiteSpace(newSub))
        {
            _currentSubreddit = "popular";
        }
        else
        {
            _currentSubreddit = newSub.Replace("r/", "").Replace("/", "").Trim();
        }

        _wordMemory.Clear(); // Clear memory to avoid crossing ranks between different subs

        StopAllCoroutines();
        StartCoroutine(TrendingLoop());
    }

    private IEnumerator TrendingLoop()
    {
        while (true)
        {
            yield return StartCoroutine(FetchTrendingTopics());
            yield return new WaitForSeconds(refreshIntervalSeconds);
        }
    }

    private IEnumerator FetchTrendingTopics()
    {
        string target = string.IsNullOrEmpty(_currentSubreddit) ? "popular" : _currentSubreddit;
        string url = $"https://www.reddit.com/r/{target}.json?limit=50";

        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.SetRequestHeader("User-Agent", "android:com.idlebroker:v0.2");

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            RedditResponse parsedData = JsonUtility.FromJson<RedditResponse>(webRequest.downloadHandler.text);
            ProcessTrendingData(parsedData);
        }
        else
        {
            Debug.LogWarning($"[TrendingManager] Failed to fetch trending: {webRequest.error}");
        }
    }

    private void ProcessTrendingData(RedditResponse response)
    {
        foreach (Transform child in trendingListContainer)
        {
            Destroy(child.gameObject);
        }

        Dictionary<string, int> currentScores = new();

        foreach (RedditPost post in response.data.children)
        {
            string[] words = post.data.title.Split(' ');
            string bestWord = "";

            foreach (string word in words)
            {
                string cleanWord = Regex.Replace(word, "[^a-zA-Z0-9]", "");
                if (cleanWord.Length > bestWord.Length && cleanWord.Length > 4)
                {
                    bestWord = cleanWord.ToLower();
                }
            }

            if (!string.IsNullOrEmpty(bestWord))
            {
                if (!currentScores.ContainsKey(bestWord))
                {
                    currentScores[bestWord] = 0;
                }
                currentScores[bestWord] += post.data.score + post.data.num_comments;
            }
        }

        List<KeyValuePair<string, int>> sortedScores = new(currentScores);
        sortedScores.Sort((a, b) => b.Value.CompareTo(a.Value));

        List<KeywordTrend> trendsToDisplay = new();
        Dictionary<string, WordData> newMemory = new();

        for (int i = 0; i < sortedScores.Count; i++)
        {
            string keyword = sortedScores[i].Key;
            int score = sortedScores[i].Value;
            int currentRank = i + 1;

            int delta = 0;
            int status = 0;
            int prevRank = 0;

            if (_wordMemory.TryGetValue(keyword, out WordData prevData))
            {
                delta = score - prevData.Score;
                prevRank = prevData.Rank;

                if (currentRank < prevRank) status = 1; // Rising 
                else if (currentRank == prevRank) status = 2; // Stable 
                else status = 3; // Falling 
            }
            else
            {
                status = 0; // New
                delta = score;
            }

            if (i < 50)
            {
                newMemory[keyword] = new WordData { Score = score, Rank = currentRank };
            }

            if (i < 20)
            {
                trendsToDisplay.Add(new KeywordTrend
                {
                    Keyword = keyword,
                    Delta = delta,
                    Status = status,
                    Score = score,
                    PrevRank = prevRank
                });
            }
        }

        _wordMemory.Clear();
        foreach (var kvp in newMemory)
        {
            _wordMemory[kvp.Key] = kvp.Value;
        }

        trendsToDisplay.Sort((a, b) => {
            int sortA = GetSortOrder(a.Status);
            int sortB = GetSortOrder(b.Status);

            if (sortA != sortB) return sortA.CompareTo(sortB);
            return b.Delta.CompareTo(a.Delta);
        });

        foreach (var trend in trendsToDisplay)
        {
            Color btnColor = new Color(0.8f, 0.8f, 0.2f);
            if (trend.Status == 1) btnColor = new Color(0.2f, 0.8f, 0.2f);
            else if (trend.Status == 2) btnColor = new Color(0.8f, 0.8f, 0.2f);
            else if (trend.Status == 3) btnColor = new Color(0.8f, 0.2f, 0.2f);

            GameObject btnObj = Instantiate(trendingButtonPrefab, trendingListContainer);
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            Button btn = btnObj.GetComponent<Button>();
            Image btnImage = btnObj.GetComponent<Image>();

            if (btnText != null)
            {
                string deltaString = trend.Status == 0 ? "NEW" : (trend.Delta > 0 ? $"+{trend.Delta}" : trend.Delta.ToString());
                string rankString = trend.PrevRank > 0 ? trend.PrevRank.ToString() : "-";

                btnText.text = $"{trend.Keyword.ToUpper()} ({deltaString}) ({rankString})";
            }

            if (btnImage != null) btnImage.color = btnColor;

            if (btn != null)
            {
                string capturedKeyword = trend.Keyword;
                btn.onClick.AddListener(() => OpenKeywordDetails(capturedKeyword));
            }
        }
    }

    private int GetSortOrder(int status)
    {
        if (status == 1) return 0;
        if (status == 0) return 1;
        if (status == 2) return 2;
        return 3;
    }

    public void OpenKeywordDetails(string keyword)
    {
        _currentlySelectedKeyword = keyword;

        if (detailPanel != null)
        {
            detailPanel.SetActive(true);
            if (detailTitleText != null) detailTitleText.text = $"TARGET: {keyword.ToUpper()}";
            if (detailHeadlinesText != null) detailHeadlinesText.text = "Pulling live data...";
            if (detailStatsText != null) detailStatsText.text = "";
            if (detailCostText != null) detailCostText.text = "Calculating cost...";
        }

        StartCoroutine(FetchKeywordSnapshot(keyword));
    }

    public void CloseKeywordDetails()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    private IEnumerator FetchKeywordSnapshot(string keyword)
    {
        string target = string.IsNullOrEmpty(_currentSubreddit) ? "popular" : _currentSubreddit;
        string url;

        // If filtering by a specific sub, we must append restrict_sr=on
        if (target.ToLower() == "popular" || target.ToLower() == "all")
        {
            url = $"https://www.reddit.com/search.json?q={keyword}&sort=hot&t=day&limit=5";
        }
        else
        {
            url = $"https://www.reddit.com/r/{target}/search.json?q={keyword}&restrict_sr=on&sort=hot&t=day&limit=5";
        }

        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.SetRequestHeader("User-Agent", "android:com.idlebroker:v0.2");

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            RedditResponse parsedData = JsonUtility.FromJson<RedditResponse>(webRequest.downloadHandler.text);

            int totalUpvotes = 0;
            int totalComments = 0;
            string headlineBlock = "";

            if (parsedData?.data?.children != null)
            {
                for (int i = 0; i < parsedData.data.children.Length; i++)
                {
                    RedditPostData postData = parsedData.data.children[i].data;
                    totalUpvotes += postData.score;
                    totalComments += postData.num_comments;

                    headlineBlock += $"<color=#FFD700>[r/{postData.subreddit}]</color> {postData.title}\n";
                    headlineBlock += $"<color=#AAAAAA><size=80%>Score: {postData.score:N0} | Comments: {postData.num_comments:N0} | Ratio: {(postData.upvote_ratio * 100):F0}%</size></color>\n\n";
                }
            }

            _currentlySelectedCost = EconomyManager.Instance.CalculateKeywordCost(keyword, totalUpvotes);

            if (detailHeadlinesText != null) detailHeadlinesText.text = headlineBlock;
            if (detailStatsText != null) detailStatsText.text = $"Live Upvotes: {totalUpvotes:N0} | Comments: {totalComments:N0}";
            if (detailCostText != null) detailCostText.text = $"Deploy Cost: {_currentlySelectedCost} Creds";
        }
        else
        {
            if (detailHeadlinesText != null) detailHeadlinesText.text = "<color=red>Connection failed.</color>";
        }
    }

    public void OnDeployButtonClicked()
    {
        if (!string.IsNullOrEmpty(_currentlySelectedKeyword))
        {
            // NEW: Passes the targeted subreddit into the game manager
            GameManager.Instance.TryDeployBot(_currentlySelectedKeyword, _currentlySelectedCost, _currentSubreddit);
            CloseKeywordDetails();
        }
    }
}