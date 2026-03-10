using UnityEngine;
using TMPro;

// This makes sure you can't accidentally put this script on an object without TextMeshPro!
[RequireComponent(typeof(TextMeshProUGUI))]
public class ThemedText : MonoBehaviour
{
    [Tooltip("What kind of text is this? It will automatically grab the right font from the ThemeManager.")]
    public TextCategory category = TextCategory.Default;

    private TextMeshProUGUI _textComponent;

    private void Awake()
    {
        _textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        if (ThemeManager.Instance != null && _textComponent != null)
        {
            TMP_FontAsset assignedFont = ThemeManager.Instance.GetFontForCategory(category);

            if (assignedFont != null)
            {
                _textComponent.font = assignedFont;
            }
        }
    }
}