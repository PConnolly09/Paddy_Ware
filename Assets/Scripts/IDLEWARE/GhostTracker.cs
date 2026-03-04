using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GhostTracker : MonoBehaviour
{
    public static GhostTracker Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartGhostTracking(string keyword, int peakUpvotesAtSellTime)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification($"<color=#888888>[GHOST TRACKER]</color> Now monitoring '{keyword}' in the shadows...");
        }
        StartCoroutine(TrackMissedOpportunities(keyword, peakUpvotesAtSellTime));
    }

    private IEnumerator TrackMissedOpportunities(string keyword, int sellPeak)
    {
        // Sped up for testing: checks 3 times, waiting 15 seconds between checks.
        int cyclesToWatch = 3;
        float timeBetweenChecks = 15f;
        int highestGhostPeak = sellPeak;
        string missedHeadline = "";

        for (int i = 0; i < cyclesToWatch; i++)
        {
            yield return new WaitForSeconds(timeBetweenChecks);

            string url = $"https://www.reddit.com/search.json?q={keyword}&sort=new&limit=3";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("User-Agent", "android:com.yourname.idlecrawler:v0.1");
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    RedditResponse parsedData = JsonUtility.FromJson<RedditResponse>(jsonResponse);

                    if (parsedData?.data?.children != null)
                    {
                        foreach (RedditPost post in parsedData.data.children)
                        {
                            if (post.data.score > highestGhostPeak)
                            {
                                highestGhostPeak = post.data.score;
                                missedHeadline = post.data.title;
                            }
                        }
                    }
                }
            }
        }

        EvaluateSellDecision(keyword, sellPeak, highestGhostPeak, missedHeadline);
    }

    private void EvaluateSellDecision(string keyword, int sellPeak, int ghostPeak, string headline)
    {
        if (UIManager.Instance == null) return;

        if (ghostPeak > (sellPeak * 1.5f) && ghostPeak > 100)
        {
            UIManager.Instance.ShowNotification($"<color=red>PAPER HANDS!</color> '{keyword}' went viral after you sold! Missed {ghostPeak} upvotes.");
        }
        else
        {
            UIManager.Instance.ShowNotification($"<color=green>DIAMOND HANDS!</color> Perfect sell on '{keyword}'. The trend died down.");
        }
    }
}