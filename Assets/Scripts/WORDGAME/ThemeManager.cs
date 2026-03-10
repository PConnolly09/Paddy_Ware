using UnityEngine;
using TMPro;

public enum TextCategory { Title, Dialogue, Button, HUD, Default }

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }

    [Header("Global Font Settings")]
    [Tooltip("The fallback font if a specific one isn't assigned.")]
    public TMP_FontAsset defaultFont;

    [Space(10)]
    public TMP_FontAsset titleFont;
    public TMP_FontAsset dialogueFont;
    public TMP_FontAsset buttonFont;
    public TMP_FontAsset hudFont;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public TMP_FontAsset GetFontForCategory(TextCategory category)
    {
        switch (category)
        {
            case TextCategory.Title: return titleFont != null ? titleFont : defaultFont;
            case TextCategory.Dialogue: return dialogueFont != null ? dialogueFont : defaultFont;
            case TextCategory.Button: return buttonFont != null ? buttonFont : defaultFont;
            case TextCategory.HUD: return hudFont != null ? hudFont : defaultFont;
            default: return defaultFont;
        }
    }

    // Call this if you ever add a feature to let the user change fonts in the settings menu!
    public void RefreshAllTextInScene()
    {
        ThemedText[] allTextElements = FindObjectsOfType<ThemedText>(true); // true includes inactive objects
        foreach (ThemedText textObj in allTextElements)
        {
            textObj.ApplyTheme();
        }
    }
}