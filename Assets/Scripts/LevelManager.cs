// LevelManager.cs - NEW SCRIPT
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public string[] levelScenes = { "Level1", "Level2" };
    private int currentLevelIndex = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CompleteLevel()
    {
        Debug.Log($"Level {currentLevelIndex + 1} complete!");

        currentLevelIndex++;

        if (currentLevelIndex < levelScenes.Length)
        {
            Debug.Log($"Loading Level {currentLevelIndex + 1}");
            LoadLevel(currentLevelIndex);
        }
        else
        {
            Debug.Log("All levels complete! GAME WON!");
            // TODO: End game screen
        }
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= levelScenes.Length)
        {
            Debug.LogError($"Invalid level index: {index}");
            return;
        }

        currentLevelIndex = index;

        // Clear run recorder for new level
        if (RunRecorder.Instance != null)
        {
            Destroy(RunRecorder.Instance.gameObject);
        }

        SceneManager.LoadScene(levelScenes[index]);
    }

    public void RestartCurrentLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }
}