using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WordUIManager : MonoBehaviour
{
    public static WordUIManager Instance { get; private set; }

    [Header("Run HUD")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI firewallHpText;
    public TextMeshProUGUI queriesText;
    public TextMeshProUGUI dicePoolText;
    public TextMeshProUGUI damageLogText;

    [Header("Drafting Area")]
    public Transform availableLettersContainer;
    public GameObject letterButtonPrefab;
    public TextMeshProUGUI spelledWordText;
    public TextMeshProUGUI selectedDieInfoText;

    [Header("Menus (Assign Panels here)")]
    public GameObject burnedWordsPanel;
    public TextMeshProUGUI burnedWordsListText;
    public GameObject deckReviewPanel;
    public TextMeshProUGUI deckReviewText;

    private string _currentSpelledWord = "";
    private readonly List<GameObject> _activeLetterButtons = new();
    private readonly List<DieData> _selectedDiceData = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (burnedWordsPanel != null) burnedWordsPanel.SetActive(false);
        if (deckReviewPanel != null) deckReviewPanel.SetActive(false);
    }

    public void UpdateRunStats(int level, double currentDamage, double targetHp, int queries, int diceInPool)
    {
        if (levelText != null) levelText.text = $"MAINFRAME LEVEL {level}";
        if (queriesText != null) queriesText.text = $"QUERIES: {queries}";
        if (dicePoolText != null) dicePoolText.text = $"DICE POOL: {diceInPool}";

        if (firewallHpText != null)
        {
            double hpRemaining = System.Math.Max(0, targetHp - currentDamage);
            firewallHpText.text = $"FIREWALL HP: {hpRemaining:N0} / {targetHp:N0}";
        }
    }

    // Updated to show the Modifier multiplier and explicitly highlight Part of Speech!
    public void LogDamage(string word, string pos, string def, int baseScore, long hits, int tomeSize, double multiplier, double totalDamage)
    {
        if (damageLogText != null)
        {
            string multiStr = multiplier > 1.0 ? $" <color=#00FF00>(x{multiplier} Relic Multiplier!)</color>" : "";

            damageLogText.text = $"[<color=#FFD700>{word.ToUpper()}</color>]\n" +
                                 $"<color=#00FFFF>PART OF SPEECH: {pos}</color>\n" +
                                 $"<color=#AAAAAA><i>{def}</i></color>\n\n" +
                                 $"Base: {baseScore} | Global Hits: {hits:N0} | Tome: {tomeSize:N0}{multiStr}\n" +
                                 $"<color=#00FF00>DAMAGE DEALT: {totalDamage:N0}</color>";
        }
    }

    public void LogError(string message)
    {
        if (damageLogText != null) damageLogText.text = $"<color=#FF0000>[SYSTEM]</color> {message}";
    }

    public void SpawnRolledLetters(List<DieData> hand)
    {
        ClearDraftingArea();

        foreach (DieData die in hand)
        {
            GameObject btnObj = Instantiate(letterButtonPrefab, availableLettersContainer);
            _activeLetterButtons.Add(btnObj);

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                // Guaranteed format that won't break on small buttons
                btnText.text = $"{die.CurrentFace}\n<size=50%><color=#AAAAAA>({die.ScoreValue})</color></size>";
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                DieData capturedDie = die;
                GameObject capturedBtn = btnObj;
                btn.onClick.AddListener(() => OnLetterClicked(capturedDie, capturedBtn));
            }
        }
    }

    private void OnLetterClicked(DieData die, GameObject buttonObj)
    {
        _currentSpelledWord += die.CurrentFace;
        _selectedDiceData.Add(die);

        // LIVE PREVIEW of the Scrabble Score!
        if (spelledWordText != null)
        {
            int currentBase = WordValidator.Instance.CalculateBaseScore(_currentSpelledWord);
            spelledWordText.text = $"{_currentSpelledWord} <color=#AAAAAA>(Base: {currentBase})</color>";
        }

        if (selectedDieInfoText != null)
        {
            selectedDieInfoText.text = $"Die Type: {die.Type}\nPossible Faces: {die.PossibleFaces}";
        }

        buttonObj.SetActive(false);
    }

    public void OnClearButtonClicked()
    {
        _currentSpelledWord = "";
        _selectedDiceData.Clear();
        if (spelledWordText != null) spelledWordText.text = "";
        if (selectedDieInfoText != null) selectedDieInfoText.text = "Select a die to inspect.";

        foreach (GameObject btn in _activeLetterButtons) btn.SetActive(true);
    }

    public void OnSubmitButtonClicked()
    {
        if (string.IsNullOrEmpty(_currentSpelledWord)) return;
        if (RunManager.Instance != null) RunManager.Instance.SubmitWord(_currentSpelledWord, _selectedDiceData);
    }

    public void OnDiscardButtonClicked()
    {
        if (_selectedDiceData.Count == 0) return;
        if (RunManager.Instance != null) RunManager.Instance.DiscardSelectedLetters(_selectedDiceData);
    }

    public void ClearDraftingArea()
    {
        _currentSpelledWord = "";
        _selectedDiceData.Clear();
        if (spelledWordText != null) spelledWordText.text = "";
        if (selectedDieInfoText != null) selectedDieInfoText.text = "";

        foreach (GameObject btn in _activeLetterButtons) Destroy(btn);
        _activeLetterButtons.Clear();
    }

    // --- MENUS ---

    public void ToggleBurnedWordsPanel()
    {
        if (burnedWordsPanel == null) return;
        bool isActive = !burnedWordsPanel.activeSelf;
        burnedWordsPanel.SetActive(isActive);

        if (isActive && burnedWordsListText != null)
        {
            List<string> burned = WordValidator.Instance.GetBurnedWordsList();
            burnedWordsListText.text = burned.Count == 0 ? "No words burned yet." : string.Join("\n", burned);
        }
    }

    public void ToggleDeckReviewPanel()
    {
        if (deckReviewPanel == null) return;
        bool isActive = !deckReviewPanel.activeSelf;
        deckReviewPanel.SetActive(isActive);

        if (isActive && deckReviewText != null && DiceDeck.Instance != null)
        {
            deckReviewText.text = DiceDeck.Instance.GetDeckSummary();
        }
    }
}