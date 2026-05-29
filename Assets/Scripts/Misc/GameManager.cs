using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Win condition: cada EnemyHealth suma 1 al iniciar; al morir resta 1.
/// Victoria cuando enemiesLeft llega a 0.
/// </summary>
public class GameManager : MonoBehaviour
{
    const string EnemiesLeftString = "Enemies Left: ";
    [SerializeField] TMP_Text enemiesLeftText;
    [SerializeField] GameObject youWinText;

    int enemiesLeft;
    bool hasWon;
    bool isPaused;
    GameObject pauseMenuRoot;
    StarterAssetsInputs starterAssetsInputs;
    FirstPersonController firstPersonController;
    ActiveWeapon activeWeapon;

    public int EnemiesLeft => enemiesLeft;
    public bool HasWon => hasWon;
    public bool IsPaused => isPaused;

    void Awake()
    {
        Time.timeScale = 1f;
        CacheGameplayComponents();
        EnsurePauseMenu();
    }

    void Start()
    {
        UpdateEnemiesLeftUI();
        if (youWinText != null)
            youWinText.SetActive(false);
    }

    void Update()
    {
        if (hasWon) return;

        if (WasEscapePressed())
            TogglePause();
    }

    void CacheGameplayComponents()
    {
        starterAssetsInputs = FindFirstObjectByType<StarterAssetsInputs>();
        firstPersonController = FindFirstObjectByType<FirstPersonController>();
        activeWeapon = FindFirstObjectByType<ActiveWeapon>();
    }

    public void AdjustEnemiesLeft(int amount)
    {
        enemiesLeft += amount;
        enemiesLeft = Mathf.Max(0, enemiesLeft);
        UpdateEnemiesLeftUI();

        if (enemiesLeft <= 0)
            TriggerWin();
    }

    void UpdateEnemiesLeftUI()
    {
        if (enemiesLeftText != null)
            enemiesLeftText.text = EnemiesLeftString + enemiesLeft;
    }

    void TriggerWin()
    {
        if (hasWon) return;

        hasWon = true;
        SetPaused(true);

        if (youWinText != null)
            youWinText.SetActive(true);
    }

    void EnsurePauseMenu()
    {
        if (pauseMenuRoot != null) return;

        Canvas canvas = GameplayUIBuilder.CreateScreenCanvas("Pause Menu Canvas", 50);
        pauseMenuRoot = canvas.gameObject;

        Transform root = canvas.transform;
        GameObject panel = new GameObject("Pause Panel", typeof(RectTransform));
        panel.transform.SetParent(root, false);
        GameplayUIBuilder.CreatePanel(panel.transform, "Overlay", new Color(0f, 0f, 0f, 0.65f));

        RectTransform titleRect = GameplayUIBuilder.CreateText(
            panel.transform, "Pause Title", "PAUSED", 64, TextAlignmentOptions.Center).rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.72f);
        titleRect.anchorMax = new Vector2(0.5f, 0.72f);
        titleRect.sizeDelta = new Vector2(700f, 90f);

        Vector2 buttonSize = new Vector2(360f, 72f);
        GameplayUIBuilder.CreateButton(panel.transform, "Resume", new Vector2(0f, 40f), buttonSize, ResumeGame);
        GameplayUIBuilder.CreateButton(panel.transform, "Main Menu", new Vector2(0f, -50f), buttonSize, LoadMainMenu);
        GameplayUIBuilder.CreateButton(panel.transform, "Quit", new Vector2(0f, -140f), buttonSize, QuitButton);

        pauseMenuRoot.SetActive(false);
    }

    public void TogglePause()
    {
        if (hasWon) return;
        SetPaused(!isPaused);
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(paused);

        SetGameplayEnabled(!paused);

        if (starterAssetsInputs != null)
            starterAssetsInputs.SetCursorState(!paused);
        else
        {
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }
    }

    void SetGameplayEnabled(bool enabled)
    {
        if (firstPersonController != null)
            firstPersonController.enabled = enabled;

        if (starterAssetsInputs != null)
            starterAssetsInputs.enabled = enabled;

        if (activeWeapon != null)
            activeWeapon.enabled = enabled;
    }

    bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
#endif
        return Input.GetKeyDown(KeyCode.Escape);
    }

    public void RestartLevelButton()
    {
        GameSession.EnteredLevelFromMenu = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        isPaused = false;
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        GameSession.EnteredLevelFromMenu = false;
        SceneNavigation.LoadMainMenu();
    }

    public void QuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
