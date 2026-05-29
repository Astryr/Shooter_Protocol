#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Al dar Play, Unity siempre abre MainMenu primero (aunque tengas MainLevel abierto en el editor).
/// </summary>
[InitializeOnLoad]
static class PlayFromMainMenu
{
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    static PlayFromMainMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        SceneAsset menuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        if (menuScene == null)
        {
            Debug.LogError(
                $"[PlayFromMainMenu] No se encontró {MainMenuPath}. " +
                "Crea la escena MainMenu o corrige la ruta.");
            return;
        }

        EditorSceneManager.playModeStartScene = menuScene;
    }
}
#endif
