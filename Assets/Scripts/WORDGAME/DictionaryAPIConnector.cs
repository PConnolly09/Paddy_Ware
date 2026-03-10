using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable] public class DatamuseWrapper { public DatamuseWord[] words; }
[Serializable] public class DatamuseWord { public string word; public string[] tags; public string[] defs; }

public class DictionaryAPIConnector : MonoBehaviour
{
    public static DictionaryAPIConnector Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Now returns a List of ALL parts of speech it qualifies as
    public IEnumerator FetchDefinition(string word, Action<List<string>, string, bool> onComplete)
    {
        string url = $"https://api.datamuse.com/words?sp={word}&md=dp&max=1";

        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string json = "{\"words\":" + webRequest.downloadHandler.text + "}";
            DatamuseWrapper parsedData = JsonUtility.FromJson<DatamuseWrapper>(json);

            if (parsedData?.words != null && parsedData.words.Length > 0 && parsedData.words[0].word.ToLower() == word.ToLower())
            {
                DatamuseWord data = parsedData.words[0];
                List<string> posList = new List<string>();
                string def = "Definition unavailable.";

                if (data.tags != null)
                {
                    if (Array.Exists(data.tags, t => t == "n")) posList.Add("NOUN");
                    if (Array.Exists(data.tags, t => t == "v")) posList.Add("VERB");
                    if (Array.Exists(data.tags, t => t == "adj")) posList.Add("ADJECTIVE");
                    if (Array.Exists(data.tags, t => t == "adv")) posList.Add("ADVERB");
                }

                if (posList.Count == 0) posList.Add("PROPER/UNKNOWN");

                if (data.defs != null && data.defs.Length > 0)
                {
                    string rawDef = data.defs[0];
                    string[] splitDef = rawDef.Split('\t');
                    def = splitDef.Length > 1 ? splitDef[1] : rawDef;
                    if (def.Length > 0) def = char.ToUpper(def[0]) + def.Substring(1);
                }

                // If Datamuse found it, it IS a valid word (Fixes the Proper Noun issue!)
                onComplete?.Invoke(posList, def, true);
                yield break;
            }
        }

        onComplete?.Invoke(new List<string> { "UNKNOWN" }, "Definition unavailable.", false);
    }
}