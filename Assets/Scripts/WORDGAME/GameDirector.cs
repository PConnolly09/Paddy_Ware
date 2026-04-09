using UnityEngine;

public enum GameState { MainMenu, CharacterSelection, PreRunShop, Combat, PostCombatShop, GameOverSummary, PermanentUpgrades }

public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance { get; private set; }

    [Header("Current State")]
    public GameState currentState;

    [Header("UI Panel References")]
    public GameObject mainMenuPanel;
    public GameObject characterSelectPanel;
    public GameObject shopPanel;
    public GameObject combatPanel;
    public GameObject gameOverPanel;
    public GameObject permanentUpgradesPanel;

    private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }
    private void Start() { TransitionToState(GameState.MainMenu); }

    public void TransitionToState(GameState newState)
    {
        currentState = newState;
        HideAllPanels();

        switch (currentState)
        {
            case GameState.MainMenu: mainMenuPanel.SetActive(true); break;
            case GameState.CharacterSelection: characterSelectPanel.SetActive(true); break;
            case GameState.PreRunShop: if (ShopManager.Instance != null) ShopManager.Instance.GeneratePreRunShop(); shopPanel.SetActive(true); break;
            case GameState.Combat: combatPanel.SetActive(true); RunManager.Instance.GenerateEncounter(); break;
            case GameState.PostCombatShop: shopPanel.SetActive(true); break; // Both Draft and Shop use this now!
            case GameState.GameOverSummary: gameOverPanel.SetActive(true); RunManager.Instance.TriggerDefeatSummary(); break;
            case GameState.PermanentUpgrades: permanentUpgradesPanel.SetActive(true); break;
        }
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (characterSelectPanel) characterSelectPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        if (combatPanel) combatPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (permanentUpgradesPanel) permanentUpgradesPanel.SetActive(false);
    }

    // ==========================================
    // BUTTON HOOKS
    // ==========================================
    public void OnClick_StartGame() { TransitionToState(GameState.CharacterSelection); }
    public void OnClick_ConfirmCharacter() { TransitionToState(GameState.PreRunShop); }
    public void OnClick_LeaveShop() { TransitionToState(GameState.Combat); }
    public void OnCombatDefeat() { TransitionToState(GameState.GameOverSummary); }
    public void OnClick_RetryRun() { RunManager.Instance.RestartCurrentRun(); }
    public void OnClick_GoToPermanentUpgrades() { TransitionToState(GameState.PermanentUpgrades); }
    public void OnClick_ReturnToMenu() { TransitionToState(GameState.MainMenu); }

    // ==========================================
    // BAG / DECK REVIEW TOGGLE
    // ==========================================
    public void OnClick_ViewBag() { WordUIManager.Instance.OpenDeckReview(); }
    public void OnClick_CloseBag() { WordUIManager.Instance.CloseDeckReview(); }
}