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

    // Simplified to just return the raw data, math is done by RunManager now!
    public IEnumerator PingWikipedia(string word, Action<long, int> onComplete)
    {
        string url = $"https://en.wikipedia.org/w/api.php?action=query&list=search&srsearch=\"{word}\"&srprop=wordcount&utf8=&format=json";

        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.SetRequestHeader("User-Agent", "android:com.lexiconbreach:v0.1");

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            WikiResponse parsedData = JsonUtility.FromJson<WikiResponse>(webRequest.downloadHandler.text);

            long totalHits = 0;
            int topArticleWordCount = 1;

            if (parsedData?.query != null)
            {
                if (parsedData.query.searchinfo != null)
                    totalHits = parsedData.query.searchinfo.totalhits;

                if (parsedData.query.search != null && parsedData.query.search.Length > 0)
                    topArticleWordCount = Mathf.Max(1, parsedData.query.search[0].wordcount);
            }

            onComplete?.Invoke(totalHits, topArticleWordCount);
        }
        else
        {
            Debug.LogError($"[API ERROR] {webRequest.error}");
            onComplete?.Invoke(0, 1);
        }
    }
}