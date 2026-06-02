using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra el Render Texture de LabSecurityCamera en un monitor del nivel.
/// </summary>
public class LabMonitorDisplay : MonoBehaviour
{
    [SerializeField] RenderTexture securityRenderTexture;
    [SerializeField] RawImage screenUI;
    [SerializeField] Renderer screenRenderer;
    [SerializeField] string textureProperty = "_BaseMap";

    void Start()
    {
        if (securityRenderTexture == null)
        {
            LabSecurityCamera camera = FindFirstObjectByType<LabSecurityCamera>();
            if (camera != null)
                securityRenderTexture = camera.GetRenderTexture();
        }

        if (screenUI != null)
            screenUI.texture = securityRenderTexture;

        if (screenRenderer != null && securityRenderTexture != null)
            screenRenderer.material.SetTexture(textureProperty, securityRenderTexture);
    }
}
