#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PGExamMenu
{
    const string ExamShaderGraphFolder = "Assets/ShaderGraph/Exam";
    const string ExamMaterialsFolder = "Assets/Materials/Exam";
    const string ExamTexturesFolder = "Assets/Textures/Exam";
    const string RenderTexturePath = "Assets/RenderTextures/SecurityCamera_RT.renderTexture";
    const string VolumeProfilePath = "Assets/Settings/LabExamVolumeProfile.asset";
    const string GuidePath = "Assets/ShaderGraph/EXAMEN_PARCIAL_PG.md";

    [MenuItem("PG/Examen/Abrir guía de consignas")]
    public static void OpenGuide()
    {
        Object guide = AssetDatabase.LoadMainAssetAtPath(GuidePath);
        if (guide != null)
            AssetDatabase.OpenAsset(guide);
        else
            EditorUtility.DisplayDialog("PG Examen", "No se encontró EXAMEN_PARCIAL_PG.md", "OK");
    }

    [MenuItem("PG/Examen/Crear carpetas del examen")]
    public static void CreateFolders()
    {
        Directory.CreateDirectory(ExamShaderGraphFolder);
        Directory.CreateDirectory(ExamMaterialsFolder);
        Directory.CreateDirectory(ExamTexturesFolder);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("PG Examen",
            "Carpetas creadas:\n• ShaderGraph/Exam\n• Materials/Exam\n• Textures/Exam\n\nCreá cada .shadergraph desde Create → Shader Graph → URP.",
            "OK");
    }

    [MenuItem("PG/Examen/Verificar Global Volume en escena activa")]
    public static void CheckGlobalVolume()
    {
        Volume[] volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
        bool hasGlobal = false;

        foreach (Volume volume in volumes)
        {
            if (volume.isGlobal && volume.sharedProfile != null)
            {
                hasGlobal = true;
                Debug.Log($"[PG Examen] Global Volume OK: {volume.name} → {volume.sharedProfile.name}");
            }
        }

        if (!hasGlobal)
        {
            Debug.LogWarning("[PG Examen] Falta un Global Volume con LabExamVolumeProfile en la escena. Ver EXAMEN_PARCIAL_PG.md");
            EditorUtility.DisplayDialog("PG Examen",
                "No hay Global Volume con perfil asignado.\n\nAgregá: GameObject → Volume → Is Global → LabExamVolumeProfile.",
                "OK");
        }
    }

    [MenuItem("PG/Examen/Crear GameObject Exam Systems")]
    public static void CreateExamSystems()
    {
        if (Object.FindFirstObjectByType<ExamCreditsUI>() != null)
        {
            EditorUtility.DisplayDialog("PG Examen", "Ya existe ExamCreditsUI en la escena.", "OK");
            return;
        }

        GameObject root = new GameObject("Exam Systems");
        root.AddComponent<ExamSystemsBootstrap>();
        root.AddComponent<ExamCreditsUI>();

        Selection.activeGameObject = root;
        Undo.RegisterCreatedObjectUndo(root, "Create Exam Systems");

        EditorUtility.DisplayDialog("PG Examen",
            "Creado 'Exam Systems'.\n\n1) Completá apellidos en ExamCreditsUI.\n2) Asigná referencia en GameManager.\n3) Configurá Security Camera + monitor (ver guía).",
            "OK");
    }

    [MenuItem("PG/Examen/Seleccionar Render Texture de seguridad")]
    public static void PingRenderTexture()
    {
        Object rt = AssetDatabase.LoadMainAssetAtPath(RenderTexturePath);
        if (rt != null)
        {
            EditorGUIUtility.PingObject(rt);
            Selection.activeObject = rt;
        }
    }

    [MenuItem("PG/Examen/Seleccionar perfil de postproceso")]
    public static void PingVolumeProfile()
    {
        Object profile = AssetDatabase.LoadMainAssetAtPath(VolumeProfilePath);
        if (profile != null)
        {
            EditorGUIUtility.PingObject(profile);
            Selection.activeObject = profile;
        }
    }
}
#endif
