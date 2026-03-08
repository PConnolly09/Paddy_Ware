using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Rebuilt Data Models for the Datamuse API
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

    public IEnumerator FetchDefinition(string word, Action<string, string> onComplete)
    {
        // md=d (definitions), md=p (parts of speech), max=1 (top result only), sp= (exact spelling)
        string url = $"https://api.datamuse.com/words?sp={word}&md=dp&max=1";

        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            // Wrap the JSON array in an object so Unity can parse it
            string json = "{\"words\":" + webRequest.downloadHandler.text + "}";
            DatamuseWrapper parsedData = JsonUtility.FromJson<DatamuseWrapper>(json);

            if (parsedData?.words != null && parsedData.words.Length > 0 && parsedData.words[0].word.ToLower() == word.ToLower())
            {
                DatamuseWord data = parsedData.words[0];
                string pos = "UNKNOWN";
                string def = "Definition unavailable in archive.";

                // Datamuse uses simple tags: n (noun), v (verb), adj (adjective), adv (adverb)
                if (data.tags != null)
                {
                    if (Array.Exists(data.tags, t => t == "n")) pos = "NOUN";
                    else if (Array.Exists(data.tags, t => t == "v")) pos = "VERB";
                    else if (Array.Exists(data.tags, t => t == "adj")) pos = "ADJECTIVE";
                    else if (Array.Exists(data.tags, t => t == "adv")) pos = "ADVERB";
                }

                // Datamuse format is "pos\tDefinition text" (e.g. "n\ta large body of water")
                if (data.defs != null && data.defs.Length > 0)
                {
                    string rawDef = data.defs[0];
                    string[] splitDef = rawDef.Split('\t');

                    if (splitDef.Length > 1) def = splitDef[1];
                    else def = rawDef;

                    // Capitalize the first letter for UI polish
                    if (def.Length > 0) def = char.ToUpper(def[0]) + def.Substring(1);
                }

                onComplete?.Invoke(pos, def);
                yield break;
            }
        }

        onComplete?.Invoke("UNKNOWN", "Definition unavailable in public archives.");
    }
}