using UnityEngine;
using System.Linq;


// --- 2. NOUN OVERCLOCK (Modifies Multipliers POST-MATH) ---
[CreateAssetMenu(fileName = "Relic_NounOverclock", menuName = "Lexicon/Relics/Noun Overclock")]
public class Relic_NounOverclock : RelicSO
{
    public override void OnPostMath(RunManager.ScoreBreakdown bd)
    {
        if (bd.pos.Contains("NOUN"))
        {
            bd.globalMult *= 1.5f;
            // Pushes to Global Logs!
            bd.globalLogs.Add("<color=#00FFFF>x1.5 (Noun Overclock)</color>");
        }
    }
}