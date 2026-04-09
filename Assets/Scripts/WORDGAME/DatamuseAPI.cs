using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

public class DatamuseAPI : MonoBehaviour
{
    public static DatamuseAPI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public IEnumerator GetWordData(string word, Action<bool, long, float, List<string>, string> callback)
    {
        // md=fp asks Datamuse for Frequency (f) and Parts of Speech (p)
        string url = "https://api.datamuse.com/words?sp=" + word + "&md=fp&max=1";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                // Network error fallback
                callback(false, 0, 0f, new List<string> { "noun" }, "");
            }
            else
            {
                string json = webRequest.downloadHandler.text;

                if (json == "[]" || string.IsNullOrWhiteSpace(json))
                {
                    // Word not found in dictionary (Anomaly!)
                    callback(true, 0, 0f, new List<string> { "anomaly" }, "anomaly");
                }
                else
                {
                    // Parse the JSON safely without needing external plugins
                    float freq = 0f;
                    List<string> posList = new List<string>();

                    // Extract Frequency
                    Match freqMatch = Regex.Match(json, @"f:([0-9.]+)");
                    if (freqMatch.Success) float.TryParse(freqMatch.Groups[1].Value, out freq);

                    // Extract Parts of Speech
                    if (json.Contains("\"n\"")) posList.Add("noun");
                    if (json.Contains("\"v\"")) posList.Add("verb");
                    if (json.Contains("\"adj\"")) posList.Add("adj");
                    if (json.Contains("\"adv\"")) posList.Add("adv");

                    if (posList.Count == 0) posList.Add("noun"); // Fallback

                    // Flavor Math: Convert frequency-per-million into raw "Hits"
                    long hits = (long)(freq * 100000);
                    if (hits == 0 && freq > 0) hits = 100;

                    callback(true, hits, freq, posList, "parsed");
                }
            }
        }
    }
}