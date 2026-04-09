using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 2. THE DICE SET BLUEPRINT
// ==========================================
[CreateAssetMenu(fileName = "New Dice Set", menuName = "Lexicon/Dice Set")]
public class LexiconDiceSetSO : ScriptableObject
{
    public List<LexiconDieSO> diceInSet;
}