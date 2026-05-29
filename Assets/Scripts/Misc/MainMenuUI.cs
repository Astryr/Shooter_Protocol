using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú principal: Start, How To Play, Credits, Quit.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    GameObject howToPlayPanel;
    GameObject creditsPanel;

    void Awake()
    {
        if (FindObjectsByType<MainMenuUI>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        Time.timeScale = 1f;
        BuildMenu();
    }

    void BuildMenu()
    {
        Canvas canvas = GameplayUIBuilder.CreateScreenCanvas("Main Menu Canvas", 10);
        Transform root = canvas.transform;

        GameplayUIBuilder.CreatePanel(root, "Background", new Color(0.04f, 0.06f, 0.1f, 1f));

        RectTransform titleRect = GameplayUIBuilder.CreateText(
            root, "Title", "SHOOTER PROTOCOL", 72, TextAlignmentOptions.Center).rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -120f);
        titleRect.sizeDelta = new Vector2(1200f, 100f);

        float y = 80f;
        const float spacing = 110f;
        Vector2 buttonSize = new Vector2(420f, 80f);

        GameplayUIBuilder.CreateButton(root, "Start", new Vector2(0f, y), buttonSize, OnStartClicked);
        y -= spacing;
        GameplayUIBuilder.CreateButton(root, "How To Play", new Vector2(0f, y), buttonSize, ShowHowToPlay);
        y -= spacing;
        GameplayUIBuilder.CreateButton(root, "Credits", new Vector2(0f, y), buttonSize, ShowCredits);
        y -= spacing;
        GameplayUIBuilder.CreateButton(root, "Quit", new Vector2(0f, y), buttonSize, QuitGame);

        howToPlayPanel = BuildInfoPanel(
            root,
            "How To Play Panel",
            "HOW TO PLAY",
            "WASD — Move\nMouse — Look\nLeft Click — Shoot\n1 / 2 / 3 — Switch weapons\nESC — Pause\n\nDefeat every enemy to win.");

        creditsPanel = BuildInfoPanel(
            root,
            "Credits Panel",
            "CREDITS",
            "Shooter Protocol\nUnity 6 + URP\nStarter Assets — First Person Controller\nCourse project — AI & combat systems");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    GameObject BuildInfoPanel(Transform parent, string panelName, string title, string body)
    {
        GameObject panel = new GameObject(panelName, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        GameplayUIBuilder.CreatePanel(panel.transform, "Dim", new Color(0f, 0f, 0f, 0.82f));

        RectTransform titleRect = GameplayUIBuilder.CreateText(
            panel.transform, "Panel Title", title, 52, TextAlignmentOptions.Center).rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.72f);
        titleRect.anchorMax = new Vector2(0.5f, 0.72f);
        titleRect.sizeDelta = new Vector2(900f, 80f);

        RectTransform bodyRect = GameplayUIBuilder.CreateText(
            panel.transform, "Panel Body", body, 30, TextAlignmentOptions.Center).rectTransform;
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.sizeDelta = new Vector2(1100f, 420f);

        GameplayUIBuilder.CreateButton(
            panel.transform, "Back", new Vector2(0f, -320f), new Vector2(280f, 70f), HideSubPanels);

        panel.SetActive(false);
        return panel;
    }

    void OnStartClicked()
    {
        GameSession.EnteredLevelFromMenu = true;
        SceneNavigation.LoadMainLevel();
    }

    void ShowHowToPlay()
    {
        HideSubPanels();
        howToPlayPanel.SetActive(true);
    }

    void ShowCredits()
    {
        HideSubPanels();
        creditsPanel.SetActive(true);
    }

    void HideSubPanels()
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
