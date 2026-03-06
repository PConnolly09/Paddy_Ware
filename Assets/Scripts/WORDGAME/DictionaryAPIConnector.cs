using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// JSON Wrappers for the Free Dictionary API
[Serializable] public class DictResponseWrapper { public DictEntry[] entries; }
[Serializable] public class DictEntry { public string word; public DictMeaning[] meanings; }
[Serializable] public class DictMeaning { public string partOfSpeech; public DictDef[] definitions; }
[Serializable] public class DictDef { public string definition; }

public class DictionaryAPIConnector : MonoBehaviour
{
    public static DictionaryAPIConnector Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public IEnumerator FetchDefinition(string word, Action<string, string> onComplete)
    {
        string url = $"https://api.dictionaryapi.dev/api/v2/entries/en/{word}";

        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            // The API returns a raw JSON array. We wrap it in an object so Unity's JsonUtility can parse it.
            string json = "{\"entries\":" + webRequest.downloadHandler.text + "}";
            DictResponseWrapper parsedData = JsonUtility.FromJson<DictResponseWrapper>(json);

            if (parsedData?.entries != null && parsedData.entries.Length > 0 && parsedData.entries[0].meanings.Length > 0)
            {
                string pos = parsedData.entries[0].meanings[0].partOfSpeech;
                string def = parsedData.entries[0].meanings[0].definitions[0].definition;
                onComplete?.Invoke(pos.ToUpper(), def);
                yield break;
            }
        }

        // Fallback if the word is valid in Scrabble but the dictionary API doesn't have a definition for it
        onComplete?.Invoke("UNKNOWN", "Definition unavailable in public archives.");
    }
}