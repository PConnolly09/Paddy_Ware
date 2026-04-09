using UnityEngine;

[CreateAssetMenu(fileName = "New Archive Tome", menuName = "Lexicon/Archive Tome")]
public class ArchiveTomeSO : ScriptableObject
{
    [Header("Tome Identity")]
    public string tomeID; // e.g., "ALICE_CH1"
    public string tomeTitle;
    public string authorName;

    [Header("The Raw Manuscript")]
    [Tooltip("Paste the ENTIRE text here. Punctuation and capitalization will be preserved automatically!")]
    [TextArea(10, 30)]
    public string fullText;

    [Header("Restoration Rewards")]
    public int completionDustReward = 1000;
    public RelicSO unlockableRelic;
}