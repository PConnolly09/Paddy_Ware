using UnityEngine;
using System.Linq;

// --- 4. TRASH TO TREASURE (Triggers on Discard, entirely ignores math!) ---
[CreateAssetMenu(fileName = "Relic_TrashToTreasure", menuName = "Lexicon/Relics/Trash To Treasure")]
public class Relic_TrashToTreasure : RelicSO
{
    public override void OnDiscard(DieData discardedDie)
    {
        RunManager.Instance.currentCredits += 5;
        WordUIManager.Instance.ShowTransientMessage("<color=#FFD700>Trash to Treasure: +5 Credits!</color>");
        WordUIManager.Instance.ForceUpdateHUD();
    }
}