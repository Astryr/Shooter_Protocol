#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Añade Main Menu UI a la escena MainMenu si falta (visible en Hierarchy).
/// </summary>
static class SetupMainMenuScene
{
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Tools/Shooter Protocol/Setup Main Menu Scene")]
    static void Setup()
    {
        if (!System.IO.File.Exists(MainMenuPath))
        {
            Debug.LogError($"No existe {MainMenuPath}. Guarda tu escena de menú en esa ruta.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

        if (Object.FindFirstObjectByType<MainMenuUI>() == null)
        {
            GameObject uiRoot = new GameObject("Main Menu UI");
            uiRoot.AddComponent<MainMenuUI>();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Añadido GameObject 'Main Menu UI' con script MainMenuUI.");
        }
        else
        {
            Debug.Log("La escena ya tiene MainMenuUI.");
        }

        Debug.Log(
            "La escena puede verse vacía en el Editor. Los botones aparecen al dar PLAY. " +
            "En Build Settings: MainMenu = índice 0.");
    }
}
#endif
