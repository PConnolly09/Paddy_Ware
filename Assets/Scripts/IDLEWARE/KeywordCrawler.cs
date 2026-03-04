using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System;
using System.Text.RegularExpressions;

public class KeywordCrawler : MonoBehaviour
{
    [Header("Bot Settings")]
    public string keyword = "artificial intelligence";
    public float crawlIntervalSeconds = 10f;
    public int botLevel = 1;

    [Header("Local Bot UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI headlineText;
    public TextMeshProUGUI hypeStatsText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI cycleStatsText;
    public TextMeshProUGUI upvotesText;

    // --- DEEP MEMORY TRACKING ---
    private class PostMemory
    {
        public int lastUpvotes;
        public int lastComments;
        public int cyclesWithoutGrowth;
    }
    private Dictionary<string, PostMemory> _trackedPosts = new Dictionary<string, PostMemory>();

    // --- BOT STATS ---
    private float _timer = 0f;
    public double botTotalHype { get; private set; } = 0;
    private int _totalUpvotesFarmed = 0;
    private int _totalCyclesRan = 0;
    public int allTimeHighestScore { get; private set; } = -1;
    private string _currentTopHeadline = "";

    // Trend State
    private enum TrendState { SCANNING, SURGING, STABLE, STALLING, FALLING }
    private TrendState _currentState = TrendState.SCANNING;

    void Start()
    {
        if (titleText != null) titleText.text = $"Crawler: {keyword.ToUpper()}";
        if (upvotesText != null) upvotesText.text = "Upvotes: 0";
        StartCoroutine(FetchRedditData());
    }

    void Update()
    {
        _timer += Time.deltaTime;
        float timeLeft = crawlIntervalSeconds - _timer;
        if (timerText != null) timerText.text = $"Cycle in: {Mathf.Max(0, timeLeft):F1}s";

        if (_timer >= crawlIntervalSeconds)
        {
            _timer = 0f;
            StartCoroutine(FetchRedditData());
        }
    }

    private IEnumerator FetchRedditData()
    {
        string url = $"https://www.reddit.com/search.json?q={keyword}&sort=hot&t=day&limit=15";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.SetRequestHeader("User-Agent", "android:com.yourname.idlecrawler:v0.1");
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                RedditResponse parsedData = JsonUtility.FromJson<RedditResponse>(jsonResponse);
                ProcessCrawledData(parsedData);
            }
        }
    }

    private void ProcessCrawledData(RedditResponse response)
    {
        if (response?.data?.children == null || response.data.children.Length == 0) return;

        _totalCyclesRan++;
        double cycleHypeGenerated = 0;
        int cycleUpvotesFarmed = 0;

        int highestScoreInBatch = -1;
        string bestHeadlineInBatch = "";

        int totalNewEngagements = 0; // Tracks total new upvotes + comments across all 5 posts this cycle

        foreach (RedditPost post in response.data.children)
        {
            RedditPostData data = post.data;

            // 1. Calculate Deltas (Velocity)
            int newUpvotes = 0;
            int newComments = 0;

            if (!_trackedPosts.ContainsKey(data.title))
            {
                newUpvotes = data.score;
                newComments = data.num_comments;
                _trackedPosts.Add(data.title, new PostMemory { lastUpvotes = data.score, lastComments = data.num_comments, cyclesWithoutGrowth = 0 });
            }
            else
            {
                PostMemory memory = _trackedPosts[data.title];
                newUpvotes = Mathf.Max(0, data.score - memory.lastUpvotes);
                newComments = Mathf.Max(0, data.num_comments - memory.lastComments);

                if (newUpvotes == 0 && newComments == 0) memory.cyclesWithoutGrowth++;
                else memory.cyclesWithoutGrowth = 0;

                memory.lastUpvotes = data.score;
                memory.lastComments = data.num_comments;
            }

            totalNewEngagements += (newUpvotes + newComments);
            cycleUpvotesFarmed += newUpvotes;

            // 2. THE ALGORITHM: Calculate Post Multipliers

            // A. Keyword Relevance (How many times does the keyword appear?)
            string combinedText = (data.title + " " + data.selftext).ToLower();
            int keywordHits = Regex.Matches(combinedText, keyword.ToLower()).Count;
            float relevanceMult = 1.0f + (keywordHits * 0.1f);

            // B. Age Multiplier (Freshness)
            DateTime postTime = DateTimeOffset.FromUnixTimeSeconds((long)data.created_utc).UtcDateTime;
            double ageInMinutes = (DateTime.UtcNow - postTime).TotalMinutes;
            float ageMult = 1.0f;
            if (ageInMinutes < 60) ageMult = 2.0f; // Breaking News!
            else if (ageInMinutes > 1440) ageMult = 0.5f; // Over 24 hours old, dying.

            // C. Sentiment (Ratio)
            float sentimentMult = data.upvote_ratio; // 0.99 is great, 0.40 is terrible

            // D. Market Cap (Subreddit Density)
            int subs = Mathf.Max(1, data.subreddit_subscribers);
            float densityMult = 1.0f;
            if (data.score > 0)
            {
                // If a post gets 500 upvotes in a sub with 1000 people, density is massive (0.5).
                float density = (float)data.score / subs;
                if (density > 0.1f) densityMult = 3.0f; // Viral within community
            }

            // 3. Calculate Final Hype for this specific post
            // Base value = New Upvotes + (New Comments * 2) + 5 Base Data for checking
            double rawValue = 5 + newUpvotes + (newComments * 2.0);

            // Apply Algorithm
            double finalPostHype = rawValue * relevanceMult * ageMult * sentimentMult * densityMult;

            // Apply Stagnation Penalty (If post hasn't moved in 3 cycles, it generates 90% less)
            if (_trackedPosts[data.title].cyclesWithoutGrowth >= 3) finalPostHype *= 0.1f;

            cycleHypeGenerated += finalPostHype;

            // Track All-Time Highs
            if (data.score > highestScoreInBatch)
            {
                highestScoreInBatch = data.score;
                bestHeadlineInBatch = data.title;
            }
        }

        // 4. DETERMINE OVERALL TREND STATE
        if (totalNewEngagements > 50) _currentState = TrendState.SURGING;
        else if (totalNewEngagements > 5) _currentState = TrendState.STABLE;
        else if (totalNewEngagements == 0 && _totalCyclesRan > 3) _currentState = TrendState.STALLING;

        // If stalling for a long time, it becomes falling
        bool allPostsDead = true;
        foreach (var mem in _trackedPosts.Values) { if (mem.cyclesWithoutGrowth < 5) allPostsDead = false; }
        if (allPostsDead && _totalCyclesRan > 5) _currentState = TrendState.FALLING;


        // 5. UPDATE UI AND GLOBAL ECONOMY
        int finalHype = Mathf.CeilToInt((float)cycleHypeGenerated);
        botTotalHype += finalHype;
        _totalUpvotesFarmed += cycleUpvotesFarmed;

        if (highestScoreInBatch > allTimeHighestScore) allTimeHighestScore = highestScoreInBatch;

        UpdateBotUI(finalHype, bestHeadlineInBatch);

        if (finalHype > 0 && GameManager.Instance != null) GameManager.Instance.AddHype(finalHype);
    }

    private void UpdateBotUI(int lastCycleHype, string bestHeadline)
    {
        if (hypeStatsText != null) hypeStatsText.text = $"Mined: {botTotalHype:F0} MB";
        if (upvotesText != null) upvotesText.text = $"Upvotes: {_totalUpvotesFarmed:N0}";

        string stateColor = "#FFFFFF";
        string stateText = _currentState.ToString();

        switch (_currentState)
        {
            case TrendState.SURGING: stateColor = "#00FF00"; break; // Green
            case TrendState.STABLE: stateColor = "#AAAAAA"; break;  // Grey
            case TrendState.STALLING: stateColor = "#FFA500"; break; // Orange
            case TrendState.FALLING: stateColor = "#FF0000"; break;  // Red
        }

        if (cycleStatsText != null)
            cycleStatsText.text = $"Last: +{lastCycleHype} MB | Status: <color={stateColor}>[{stateText}]</color>";

        if (headlineText != null)
        {
            headlineText.text = $"<color={stateColor}>Top Intel:</color>\n{bestHeadline}";
        }
    }

    public void OnLiquidateButtonClicked()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.LiquidateKeyword(keyword, botTotalHype, allTimeHighestScore);

        if (UIManager.Instance != null) UIManager.Instance.ClearHeadline();

        if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
        {
            var botSave = SaveManager.Instance.currentData.activeBots.Find(b => b.keyword == keyword);
            if (botSave != null)
            {
                SaveManager.Instance.currentData.activeBots.Remove(botSave);
                SaveManager.Instance.SaveGame();
            }
        }
        Destroy(gameObject);
    }
}