using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Win condition: cada EnemyHealth suma 1 al iniciar; al morir resta 1.
/// Victoria cuando enemiesLeft llega a 0.
/// ESC: pausa con Resume y Quit.
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] TMP_Text enemiesLeftText;
    [SerializeField] GameObject youWinText;

    int enemiesLeft;
    bool isPaused;
    GameObject pauseMenuRoot;

    StarterAssetsInputs starterAssetsInputs;
    FirstPersonController firstPersonController;
    ActiveWeapon activeWeapon;

    const string EnemiesLeftString = "Enemies Left: ";

    void Awake()
    {
        CacheGameplayComponents();
        BuildPauseMenu();
    }

    void Update()
    {
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
        enemiesLeftText.text = EnemiesLeftString + enemiesLeft;

        if (enemiesLeft <= 0 && youWinText != null)
            youWinText.SetActive(true);
    }

    public void RestartLevelButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitButton()
    {
        QuitGame();
    }

    void TogglePause()
    {
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

        if (firstPersonController != null)
            firstPersonController.enabled = !paused;

        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.enabled = !paused;
            starterAssetsInputs.SetCursorState(!paused);
        }
        else
        {
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        if (activeWeapon != null)
            activeWeapon.enabled = !paused;
    }

    void BuildPauseMenu()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        GameObject canvasObject = new GameObject("Pause Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        pauseMenuRoot = new GameObject("Pause Menu");
        pauseMenuRoot.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = pauseMenuRoot.AddComponent<RectTransform>();
        StretchRect(panelRect);

        Image overlay = pauseMenuRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.65f);

        CreatePauseButton(pauseMenuRoot.transform, "Resume", new Vector2(0f, 40f), ResumeGame);
        CreatePauseButton(pauseMenuRoot.transform, "Quit", new Vector2(0f, -40f), QuitGame);

        pauseMenuRoot.SetActive(false);
    }

    void CreatePauseButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(320f, 64f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.2f, 0.28f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        StretchRect(textRect);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
#endif
        return false;
    }

    void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
