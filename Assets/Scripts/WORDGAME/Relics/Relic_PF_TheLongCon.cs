using UnityEngine;
using System.Linq;


// --- 3. THE LONG CON (Modifies Base Score & Multipliers) ---
[CreateAssetMenu(fileName = "Relic_TheLongCon", menuName = "Lexicon/Relics/The Long Con")]
public class Relic_TheLongCon : RelicSO
{
    public override void OnPostMath(RunManager.ScoreBreakdown bd)
    {
        if (bd.word.Length >= 7)
        {
            bd.finalBaseScore += 10;
            bd.globalMult *= 1.5f;
            // Splits the logs correctly!
            bd.baseLogs.Add("<color=#00FFFF>+10 Base (The Long Con)</color>");
            bd.globalLogs.Add("<color=#00FFFF>x1.5 (The Long Con)</color>");
        }
    }
}