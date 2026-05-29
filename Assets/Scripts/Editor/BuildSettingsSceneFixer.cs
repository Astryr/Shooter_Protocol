#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Evita que MainMenu aparezca como "Deleted" en Build Settings:
/// usa el GUID real del .meta de cada escena.
/// </summary>
[InitializeOnLoad]
static class BuildSettingsSceneFixer
{
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    const string MainLevelPath = "Assets/Scenes/MainLevel.unity";

    static BuildSettingsSceneFixer()
    {
        EditorApplication.delayCall += SyncBuildScenes;
    }

    [MenuItem("Tools/Shooter Protocol/Fix Build Settings Scenes")]
    static void SyncBuildScenesMenu()
    {
        SyncBuildScenes();
    }

    static void SyncBuildScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var desired = new List<EditorBuildSettingsScene>();
        TryAdd(MainMenuPath, desired);
        TryAdd(MainLevelPath, desired);

        if (desired.Count == 0)
            return;

        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        if (ScenesMatch(current, desired))
            return;

        EditorBuildSettings.scenes = desired.ToArray();
        Debug.Log("[BuildSettingsSceneFixer] Build Settings actualizado: MainMenu (0), MainLevel (1).");
    }

    static void TryAdd(string path, List<EditorBuildSettingsScene> list)
    {
        if (!File.Exists(path))
            return;

        string guid = AssetDatabase.AssetPathToGUID(path);
        if (string.IsNullOrEmpty(guid))
            return;

        list.Add(new EditorBuildSettingsScene(path, true));
    }

    static bool ScenesMatch(EditorBuildSettingsScene[] current, List<EditorBuildSettingsScene> desired)
    {
        if (current == null || current.Length != desired.Count)
            return false;

        for (int i = 0; i < desired.Count; i++)
        {
            if (current[i].path != desired[i].path)
                return false;

            string expectedGuid = AssetDatabase.AssetPathToGUID(desired[i].path);
            if (!string.IsNullOrEmpty(expectedGuid) && current[i].guid.ToString() != expectedGuid)
                return false;
        }

        return true;
    }
}
#endif
