using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable] public class WikiResponse { public WikiQuery query; }
[Serializable] public class WikiQuery { public WikiSearchInfo searchinfo; public WikiSearchResult[] search; }
[Serializable] public class WikiSearchInfo { public int totalhits; }
[Serializable] public class WikiSearchResult { public int wordcount; public int size; }

public class WikiAPIConnector : MonoBehaviour
{
    public static WikiAPIConnector Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Notice the updated Action signature to pass back all the juicy data stats
    public IEnumerator PingWikipediaForDamage(string word, int baseScore, Action<double, string, int, int, int> onDamageCalculated)
    {
        string url = $"https://en.wikipedia.org/w/api.php?action=query&list=search&srsearch=\"{word}\"&srprop=wordcount&utf8=&format=json";

        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.SetRequestHeader("User-Agent", "android:com.lexiconbreach:v0.1");

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            WikiResponse parsedData = JsonUtility.FromJson<WikiResponse>(webRequest.downloadHandler.text);

            int totalHits = 0;
            int topArticleWordCount = 1;

            if (parsedData?.query != null)
            {
                if (parsedData.query.searchinfo != null)
                    totalHits = parsedData.query.searchinfo.totalhits;

                if (parsedData.query.search != null && parsedData.query.search.Length > 0)
                    topArticleWordCount = Mathf.Max(1, parsedData.query.search[0].wordcount);
            }

            double finalDamage = (double)baseScore * totalHits * topArticleWordCount;

            // Pass all the data back to the RunManager so it can be logged in the UI!
            onDamageCalculated?.Invoke(finalDamage, word, baseScore, totalHits, topArticleWordCount);
        }
        else
        {
            Debug.LogError($"[API ERROR] {webRequest.error}");
            onDamageCalculated?.Invoke(0, word, baseScore, 0, 0);
        }
    }
}