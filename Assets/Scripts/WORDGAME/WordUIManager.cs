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
    public TextMeshProUGUI dicePoolText; // NEW: Shows dice left to draw
    public TextMeshProUGUI damageLogText;

    [Header("Drafting Area")]
    public Transform availableLettersContainer;
    public GameObject letterButtonPrefab;
    public TextMeshProUGUI spelledWordText;
    public TextMeshProUGUI selectedDieInfoText; // NEW: Shows possible faces when a die is clicked

    [Header("Burned Words UI")]
    public GameObject burnedWordsPanel;
    public TextMeshProUGUI burnedWordsListText;

    private string _currentSpelledWord = "";
    private readonly List<GameObject> _activeLetterButtons = new();
    private readonly List<DieData> _selectedDiceData = new(); // Tracks the actual dice objects used

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (burnedWordsPanel != null) burnedWordsPanel.SetActive(false);
    }

    // --- HUD UPDATES ---

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

    public void LogDamage(string word, string pos, string def, int baseScore, int hits, int tomeSize, double totalDamage)
    {
        if (damageLogText != null)
        {
            damageLogText.text = $"[<color=#FFD700>{word.ToUpper()}</color>]\n" +
                                 $"<color=#AAAAAA><i>{pos}</i> - {def}</color>\n\n" +
                                 $"Base: {baseScore} | Global Hits: {hits:N0} | Tome: {tomeSize:N0}\n" +
                                 $"<color=#00FF00>DAMAGE DEALT: {totalDamage:N0}</color>";
        }
    }

    public void LogError(string message)
    {
        if (damageLogText != null) damageLogText.text = $"<color=#FF0000>[SYSTEM MESSAGE]</color>\n{message}";
    }

    // --- DRAFTING LOGIC ---

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
                // Uses Unity Rich Text to put the small score number underneath the big letter!
                btnText.text = $"{die.CurrentFace}\n<size=40%><color=#AAAAAA>{die.ScoreValue}</color></size>";
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

        if (spelledWordText != null) spelledWordText.text = _currentSpelledWord;

        // Show the tooltip info for the possible faces of this specific die
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

        if (RunManager.Instance != null)
        {
            RunManager.Instance.SubmitWord(_currentSpelledWord, _selectedDiceData);
        }
    }

    public void OnDiscardButtonClicked()
    {
        if (_selectedDiceData.Count == 0)
        {
            LogError("Select dice first to discard them (Costs 1 Query).");
            return;
        }

        if (RunManager.Instance != null)
        {
            RunManager.Instance.DiscardSelectedLetters(_selectedDiceData);
        }
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

    // --- BURNED WORDS UI ---

    public void ToggleBurnedWordsPanel()
    {
        if (burnedWordsPanel == null) return;

        bool isActive = !burnedWordsPanel.activeSelf;
        burnedWordsPanel.SetActive(isActive);

        if (isActive && burnedWordsListText != null)
        {
            List<string> burned = WordValidator.Instance.GetBurnedWordsList();
            if (burned.Count == 0) burnedWordsListText.text = "No words burned yet.";
            else burnedWordsListText.text = string.Join("\n", burned);
        }
    }
}