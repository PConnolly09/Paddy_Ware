using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class KeywordCrawler : MonoBehaviour
{
    [Header("Bot Data")]
    public string keyword = "";
    public int buyInCost = 0;
    public double botTotalHype = 0;
    public string targetSubreddit = "popular";

    [Header("Local Bot UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI headlineText;
    public TextMeshProUGUI hypeStatsText;
    public TextMeshProUGUI cycleStatsText;
    public TextMeshProUGUI trendLineText;
    public TextMeshProUGUI upvotesText;

    [Header("Visual Line Graph UI")]
    public RectTransform graphContainer;
    public GameObject pointPrefab; // NEW: The dot
    public GameObject linePrefab;  // NEW: The connecting line

    // Limits the graph to exactly 10 points
    private readonly int _maxGraphBars = 10;

    private int _lastCycleUpvoteSum = 0;
    private int _cyclesInFallingState = 0;
    private int _totalUpvotesFarmed = 0;

    private enum TrendState { SCANNING, SURGING, STABLE, STALLING, FALLING }
    private TrendState _currentState = TrendState.SCANNING;

    private List<int> _recentHypeGains = new();

    // Holds the reference to its own save file to write graph data instantly
    private BotSaveData _mySaveData;

    public void InitializeBot(BotSaveData data)
    {
        _mySaveData = data;
        keyword = data.keyword;
        buyInCost = data.buyInCostCreds;
        botTotalHype = data.totalHypeMined;
        targetSubreddit = string.IsNullOrEmpty(data.targetSubreddit) ? "popular" : data.targetSubreddit;

        // Restore saved graph history if it exists
        if (data.hypeHistory != null)
        {
            _recentHypeGains = new List<int>(data.hypeHistory);
        }

        if (titleText != null)
        {
            string subText = targetSubreddit == "popular" ? "[GLOBAL]" : $"[r/{targetSubreddit}]";
            titleText.text = $"{subText} {keyword.ToUpper()}";
        }

        if (upvotesText != null) upvotesText.text = "Upvotes: 0";

        GameManager.Instance.OnGlobalCycleTick += TriggerCrawl;
        TriggerCrawl();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGlobalCycleTick -= TriggerCrawl;
        }
    }

    private void TriggerCrawl()
    {
        StartCoroutine(FetchRedditData());
    }

    private IEnumerator FetchRedditData()
    {
        string target = string.IsNullOrEmpty(targetSubreddit) ? "popular" : targetSubreddit;
        string url;

        if (target.ToLower() == "popular" || target.ToLower() == "all")
        {
            url = $"https://www.reddit.com/search.json?q={keyword}&sort=hot&t=day&limit=10";
        }
        else
        {
            url = $"https://www.reddit.com/r/{target}/search.json?q={keyword}&restrict_sr=on&sort=hot&t=day&limit=10";
        }

        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.SetRequestHeader("User-Agent", "android:com.idlebroker:v0.2");

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            ProcessCrawledData(JsonUtility.FromJson<RedditResponse>(webRequest.downloadHandler.text));
        }
    }

    private void ProcessCrawledData(RedditResponse response)
    {
        if (response?.data?.children == null || response.data.children.Length == 0) return;

        int currentUpvoteSum = 0;
        int currentCommentSum = 0;
        string bestHeadline = "";
        int bestHeadlineUpvotes = 0;

        foreach (RedditPost post in response.data.children)
        {
            currentUpvoteSum += post.data.score;
            currentCommentSum += post.data.num_comments;

            if (post.data.score > bestHeadlineUpvotes)
            {
                bestHeadlineUpvotes = post.data.score;
                bestHeadline = post.data.title;
            }
        }

        int upvoteDelta = Mathf.Max(0, currentUpvoteSum - _lastCycleUpvoteSum);
        _totalUpvotesFarmed += upvoteDelta;

        if (upvoteDelta > 100) _currentState = TrendState.SURGING;
        else if (upvoteDelta > 10) _currentState = TrendState.STABLE;
        else if (upvoteDelta <= 5 && _currentState == TrendState.STABLE) _currentState = TrendState.STALLING;
        else if (upvoteDelta == 0 && (_currentState == TrendState.STALLING || _currentState == TrendState.FALLING))
        {
            _currentState = TrendState.FALLING;
            _cyclesInFallingState++;
        }
        else if (upvoteDelta > 10 && _currentState == TrendState.FALLING)
        {
            _currentState = TrendState.STABLE;
            _cyclesInFallingState = 0;
        }

        _lastCycleUpvoteSum = currentUpvoteSum;

        double rawHype = 10 + (upvoteDelta * 2.0) + (currentCommentSum * 0.1);

        if (_currentState == TrendState.SURGING) rawHype *= 2.0;
        else if (_currentState == TrendState.STALLING) rawHype *= 0.5;
        else if (_currentState == TrendState.FALLING) rawHype *= 0.1;

        int finalHype = Mathf.CeilToInt((float)rawHype);
        botTotalHype += finalHype;

        _recentHypeGains.Add(finalHype);
        if (_recentHypeGains.Count > _maxGraphBars) _recentHypeGains.RemoveAt(0);

        // Instantly write history back to the save object
        if (_mySaveData != null)
        {
            _mySaveData.hypeHistory = new List<int>(_recentHypeGains);
            _mySaveData.totalHypeMined = botTotalHype;
        }

        UpdateBotUI(finalHype, bestHeadline, bestHeadlineUpvotes);

        if (finalHype > 0 && GameManager.Instance != null) GameManager.Instance.AddHype(finalHype);
    }

    private void UpdateBotUI(int lastHype, string headline, int currentUpvotes)
    {
        if (hypeStatsText != null) hypeStatsText.text = $"Hype Mined: {botTotalHype:N0} MB";
        if (upvotesText != null) upvotesText.text = $"Upvotes: {_totalUpvotesFarmed:N0}";

        string stateColor = "#FFFFFF";
        if (_currentState == TrendState.SURGING) stateColor = "#00FF00";
        else if (_currentState == TrendState.STALLING) stateColor = "#FFA500";
        else if (_currentState == TrendState.FALLING) stateColor = "#FF0000";

        if (cycleStatsText != null)
            cycleStatsText.text = $"State: <color={stateColor}>[{_currentState}]</color> | Yield: +{lastHype} MB";

        if (headlineText != null)
        {
            headlineText.text = $"<color={stateColor}>Top Intel ({currentUpvotes:N0} Upvotes):</color>\n{headline}";
        }

        if (trendLineText != null && _recentHypeGains.Count >= 2)
        {
            string line = "Trend: ";
            for (int i = 1; i < _recentHypeGains.Count; i++)
            {
                if (_recentHypeGains[i] > _recentHypeGains[i - 1]) line += "<color=#00FF00>+</color> ";
                else if (_recentHypeGains[i] < _recentHypeGains[i - 1]) line += "<color=#FF0000>-</color> ";
                else line += "<color=#AAAAAA>*</color> ";
            }
            trendLineText.text = line;
        }

        DrawLineGraph();
    }

    private void DrawLineGraph()
    {
        if (graphContainer == null || pointPrefab == null || linePrefab == null) return;

        foreach (Transform child in graphContainer) Destroy(child.gameObject);

        int localPeak = 10;
        foreach (int h in _recentHypeGains) if (h > localPeak) localPeak = h;

        float width = graphContainer.rect.width;
        float height = graphContainer.rect.height;
        Vector2 prevPos = Vector2.zero;

        for (int i = 0; i < _recentHypeGains.Count; i++)
        {
            int hypeVal = _recentHypeGains[i];

            // Calculate absolute X and Y relative to center pivot (0.5, 0.5)
            float x = _recentHypeGains.Count > 1 ? ((float)i / (_maxGraphBars - 1)) * width : width / 2f;
            float y = ((float)hypeVal / localPeak) * height;
            Vector2 currentPos = new Vector2(x - (width / 2f), y - (height / 2f));

            Color trendColor = new Color(0.2f, 0.8f, 0.2f); // Green
            if (i > 0 && hypeVal < _recentHypeGains[i - 1])
            {
                trendColor = new Color(0.8f, 0.2f, 0.2f); // Red
            }

            // Spawn Point
            GameObject pt = Instantiate(pointPrefab, graphContainer);
            RectTransform ptRect = pt.GetComponent<RectTransform>();
            if (ptRect != null) ptRect.anchoredPosition = currentPos;
            Image ptImg = pt.GetComponent<Image>();
            if (ptImg != null) ptImg.color = trendColor;

            // Spawn Line between points
            if (i > 0)
            {
                GameObject line = Instantiate(linePrefab, graphContainer);
                RectTransform lineRect = line.GetComponent<RectTransform>();
                if (lineRect != null)
                {
                    Vector2 dir = currentPos - prevPos;
                    float dist = dir.magnitude;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    lineRect.anchoredPosition = prevPos + dir / 2f; // Position at midpoint
                    lineRect.sizeDelta = new Vector2(dist, 2f); // Stretch width to distance, 2 units thick
                    lineRect.localRotation = Quaternion.Euler(0, 0, angle);
                }
                Image lineImg = line.GetComponent<Image>();
                if (lineImg != null) lineImg.color = trendColor;
            }

            prevPos = currentPos;
        }
    }

    public void OnLiquidateButtonClicked()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.LiquidateBot(keyword, buyInCost, botTotalHype, _cyclesInFallingState);

        if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
        {
            // Now we cleanly remove the exact object reference!
            if (_mySaveData != null)
            {
                SaveManager.Instance.currentData.activeBots.Remove(_mySaveData);
                SaveManager.Instance.SaveGame();
            }
        }
        Destroy(gameObject);
    }
}