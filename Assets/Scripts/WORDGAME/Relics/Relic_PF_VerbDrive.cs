using UnityEngine;
using System.Linq;

// --- 1. THE VERB DRIVE (Modifies Raw Hits PRE-MATH) ---
[CreateAssetMenu(fileName = "Relic_VerbDrive", menuName = "Lexicon/Relics/Verb Drive")]
public class Relic_VerbDrive : RelicSO
{
    public override void OnPreMath(RunManager.ScoreBreakdown bd)
    {
        if (bd.pos.Contains("VERB"))
        {
            bd.finalHits += 5000000;
            // Pushes to Hit Logs!
            bd.hitLogs.Add("<color=#00FFFF>+5,000,000 Raw Hits (Verb Drive)</color>");
        }
    }
}