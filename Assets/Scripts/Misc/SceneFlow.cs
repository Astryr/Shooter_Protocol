using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// Arranque del menú, cámara URP en MainMenu y redirección si se abre MainLevel sin Start.
/// </summary>
public static class SceneFlow
{
    static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSessionOnPlay()
    {
        GameSession.EnteredLevelFromMenu = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (!subscribed)
        {
            subscribed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMainMenuScene(scene))
        {
            SetupMainMenuScene();
            return;
        }

        if (scene.name == SceneNavigation.MainLevelSceneName && !GameSession.EnteredLevelFromMenu)
            SceneNavigation.LoadMainMenu();
    }

    static bool IsMainMenuScene(Scene scene)
    {
        if (scene.name == SceneNavigation.MainMenuSceneName)
            return true;

        return scene.buildIndex == SceneNavigation.MainMenuBuildIndex && scene.buildIndex >= 0;
    }

    static void SetupMainMenuScene()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureEventSystem();
        EnsureMainMenuCamera();

        if (Object.FindFirstObjectByType<MainMenuUI>() == null)
            new GameObject("Main Menu UI", typeof(MainMenuUI));
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

    static void EnsureMainMenuCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length > 0)
                cam = cameras[0];
        }

        if (cam == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cam = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        cam.enabled = true;
        cam.gameObject.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.1f, 1f);
        cam.depth = -1;

#if UNITY_RENDER_PIPELINE_UNIVERSAL
        if (cam.GetComponent<UniversalAdditionalCameraData>() == null)
            cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
#endif
    }
}
