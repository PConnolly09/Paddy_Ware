using UnityEngine;
using System.Collections.Generic;

public enum DieEffectType
{
    None,
    LetterMultiplier, // Multiplies just this letter's score (e.g., x3 to 'Z')
    WordMultiplier,   // Multiplies the whole word (e.g., Golden Face)
    HealPlayer,       // Restores HP
    RefundQuery       // Gives back an action point
}

// Add this enum if it isn't defined somewhere else in your project!
public enum DiceType { D4, D6, D8, D10, D12, D20 }

[System.Serializable]
public class DieFace
{
    public string faceText;
    public string altFaceText = "";

    [Header("Standard Upgrades")]
    public int bonusScore = 0;

    [Header("Special Modifiers")]
    public DieEffectType specialEffect = DieEffectType.None;
    public float effectValue = 0f; // Could be a 1.5 multiplier, or 10 HP healed

    public bool playedThisLevel = false;
    public int timesPlayedThisRun = 0;

    public bool HasSplitFace => !string.IsNullOrEmpty(altFaceText);

    // The NEW modern math method
    public int GetBaseLetterScore(string textToScore)
    {
        int baseScore = 0;
        if (WordValidator.Instance != null && !string.IsNullOrEmpty(textToScore))
            baseScore = WordValidator.Instance.GetLetterScore(textToScore[0]);
        return baseScore + bonusScore;
    }

    // ==========================================
    // LEGACY UI FIXES (Overloads)
    // These ensure your old UI and Draft Cards don't break!
    // ==========================================

    // If a script calls GetTotalScore() with NO arguments
    public int GetTotalScore()
    {
        return GetBaseLetterScore(faceText);
    }

    // If a script calls GetTotalScore("A") with a STRING argument
    public int GetTotalScore(string text)
    {
        return GetBaseLetterScore(text);
    }

    // If a script calls GetTotalScore('A') with a CHAR argument
    public int GetTotalScore(char text)
    {
        return GetBaseLetterScore(text.ToString());
    }

    // ==========================================

    public void ToggleSplitFace()
    {
        if (HasSplitFace)
        {
            string temp = faceText;
            faceText = altFaceText;
            altFaceText = temp;
        }
    }
}

[System.Serializable]
public class DieData
{
    public string dieName;
    public DiceType visualShape;
    public Sprite dieSprite;
    public GameObject diePrefab;

    public List<DieFace> faces = new List<DieFace>();
    public DieFace currentFace;

    // ==========================================
    // NEW: THE OVERLOAD MECHANIC
    // ==========================================
    public int consecutivePlays = 0;
    public int maxStress = 3; // 0=Safe, 1=Warm, 2=Cracked, 3=Critical, 4=EXPLOSION

    public void Roll()
    {
        if (faces.Count > 0) currentFace = faces[UnityEngine.Random.Range(0, faces.Count)];
    }
}

public class DiceDeck : MonoBehaviour
{
    public static DiceDeck Instance { get; private set; }

    [Header("Run Data")]
    public List<DieData> allOwnedDice = new List<DieData>();
    public List<DieData> currentHand = new List<DieData>();
    public List<DieData> drawBag = new List<DieData>();
    public List<DieData> discardPile = new List<DieData>(); // NEW: The Discard Queue!

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==========================================
    // ASSEMBLES THE DECK FROM SCRIPTABLE OBJECTS
    // ==========================================
    public void InitializeStartingDeck(LexiconCharacterSO chosenCharacter)
    {
        allOwnedDice.Clear();

        // 1. Load the Base Set (The standard 6 dice)
        if (chosenCharacter.baseDiceSet != null)
        {
            foreach (LexiconDieSO dieBlueprint in chosenCharacter.baseDiceSet.diceInSet)
            {
                allOwnedDice.Add(ForgeDieFromBlueprint(dieBlueprint));
            }
        }

        // 2. Load the Class-Specific Special Dice
        foreach (LexiconDieSO specialDieBlueprint in chosenCharacter.classSpecialDice)
        {
            allOwnedDice.Add(ForgeDieFromBlueprint(specialDieBlueprint));
        }

        Debug.Log($"<color=#00FF00>Dice Deck Assembled: {allOwnedDice.Count} total dice for {chosenCharacter.className}.</color>");
    }

    // Helper method to turn a Blueprint (SO) into active Run Data
    private DieData ForgeDieFromBlueprint(LexiconDieSO blueprint)
    {
        DieData newDie = new DieData();
        newDie.dieName = blueprint.dieName;
        newDie.visualShape = blueprint.visualShape;
        newDie.dieSprite = blueprint.dieSprite; // NEW: Pass the sprite from blueprint to active run data!
        newDie.diePrefab = blueprint.diePrefab;

        foreach (string faceText in blueprint.defaultFaces)
        {
            DieFace newFace = new DieFace();
            newFace.faceText = faceText;
            newFace.bonusScore = 0;
            newFace.playedThisLevel = false;
            newFace.timesPlayedThisRun = 0;

            newDie.faces.Add(newFace);
        }
        return newDie;
    }

    public void AddNewDie(LexiconDieSO blueprint)
    {
        DieData newDie = ForgeDieFromBlueprint(blueprint);
        allOwnedDice.Add(newDie);
        discardPile.Add(newDie); // In deckbuilders, newly acquired items go to the discard pile!

        Debug.Log($"<color=#00FF00>New {blueprint.dieName} added to the deck!</color>");
    }

    // ==========================================
    // COMBAT LOGIC
    // ==========================================
    public void SetupEncounterPool()
    {
        drawBag.Clear();
        discardPile.Clear();
        currentHand.Clear();

        drawBag.AddRange(allOwnedDice);

        foreach (var die in allOwnedDice)
        {
            foreach (var face in die.faces)
            {
                face.playedThisLevel = false;
            }
        }
    }

    public void FillHand(int targetSize)
    {
        while (currentHand.Count < targetSize)
        {
            // If the draw bag is empty, cycle the discard pile back in!
            if (drawBag.Count == 0)
            {
                if (discardPile.Count == 0) break; // We physically have no more dice to draw!

                drawBag.AddRange(discardPile);
                discardPile.Clear();
                Debug.Log("<color=#FF8800>Draw bag empty! Shuffling discard pile back into the bag.</color>");
            }

            // Using UnityEngine.Random to avoid the System.Random ambiguity error
            int randomIndex = UnityEngine.Random.Range(0, drawBag.Count);
            DieData drawnDie = drawBag[randomIndex];
            drawBag.RemoveAt(randomIndex);

            drawnDie.Roll();
            currentHand.Add(drawnDie);
        }
    }

    public void ReturnToBag(DieData die)
    {
        if (currentHand.Contains(die))
        {
            currentHand.Remove(die);
            discardPile.Add(die); // Spent dice go to the discard queue, NOT immediately back to the draw bag!
        }
    }
}