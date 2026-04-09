using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 3. THE CHARACTER BLUEPRINT
// ==========================================
[CreateAssetMenu(fileName = "New Character", menuName = "Lexicon/Character Blueprint")]
public class LexiconCharacterSO : ScriptableObject
{
    [Header("Character Identity")]
    public string className;
    [TextArea(3, 5)]
    public string classDescription;

    [Header("Starting Loadout")]
    public int startingCredits = 100;
    public LexiconDiceSetSO baseDiceSet;
    public List<LexiconDieSO> classSpecialDice;

    [Header("Class Modifiers")]
    public int bonusQueries = 0;       // RESTORED!
    public int bonusStartingHP = 0;    // RESTORED!
}