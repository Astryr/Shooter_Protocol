using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Carga escenas por índice de Build Settings (más fiable que solo por nombre).
/// 0 = MainMenu, 1 = MainLevel
/// </summary>
public static class SceneNavigation
{
    public const int MainMenuBuildIndex = 0;
    public const int MainLevelBuildIndex = 1;

    public const string MainMenuSceneName = "MainMenu";
    public const string MainLevelSceneName = "MainLevel";

    public static void LoadMainMenu()
    {
        Time.timeScale = 1f;
        LoadByIndexOrName(MainMenuBuildIndex, MainMenuSceneName);
    }

    public static void LoadMainLevel()
    {
        Time.timeScale = 1f;
        LoadByIndexOrName(MainLevelBuildIndex, MainLevelSceneName);
    }

    static void LoadByIndexOrName(int buildIndex, string sceneName)
    {
        if (SceneManager.sceneCountInBuildSettings > buildIndex)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log($"[SceneNavigation] Cargando escena buildIndex={buildIndex} path={path}");
                SceneManager.LoadScene(buildIndex);
                return;
            }
        }

        Debug.LogWarning(
            $"[SceneNavigation] Build index {buildIndex} no disponible. Intentando cargar por nombre: {sceneName}");

        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogError($"[SceneNavigation] No se pudo cargar la escena '{sceneName}'. Revisa File → Build Settings.");
    }
}
