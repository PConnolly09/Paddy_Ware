using System.Collections.Generic;
using static RunManager;

public enum NodeType { Combat, Elite, Shop, Vault, Boss }

public class MapPath
{
   // public MapNode destinationNode;
    public LinguisticLock requiredLock; // Null if the path is open to everyone
}

// The Lock logic
public class LinguisticLock
{
    public int requiredLength = 0; // e.g., 7
    public string requiredPOS = ""; // e.g., "VERB"
    public bool requiresPalindrome = false;

    // Checks the RunManager to see if the player has the "Key"
    public bool IsUnlocked(RunLinguisticHistory history)
    {
        if (requiredLength > 0 && !history.wordLengthsPlayed.Contains(requiredLength)) return false;
        if (!string.IsNullOrEmpty(requiredPOS) && !history.partsOfSpeechPlayed.Contains(requiredPOS)) return false;
        if (requiresPalindrome && !history.hasPlayedPalindrome) return false;

        return true; // The path is open!
    }
}

[System.Serializable]
public class MapNode
{
    public string nodeID; // e.g., "Node_1_A"
    public NodeType type; // Combat, Elite, Shop, Vault

    // Coordinates for drawing it on the screen later
    public int gridColumn;
    public int gridRow;

    // The lines drawing OUT of this node to the next choices
    public List<MapPath> outgoingPaths = new List<MapPath>();
}