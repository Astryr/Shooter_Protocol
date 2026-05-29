using UnityEngine;

/// <summary>
/// Aplica emisión HDR en el hijo "Sphere Glow" (mismo patrón que FleeingRobot / SniperRobot).
/// Robot y Turret no lo hacían en runtime y quedaban más apagados que el resto.
/// </summary>
public static class EnemyGlowVisual
{
    public static readonly Color TealGlow = new Color(0.3690898f, 2.6082375f, 1.6168528f, 1f);

    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    public static Renderer FindGlowRenderer(Transform root)
    {
        Transform glow = root.Find("Model/HoveringRobot02/Sphere Glow");
        if (glow == null)
            glow = root.Find("Sphere Glow");

        if (glow != null)
            return glow.GetComponent<Renderer>();

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Sphere Glow")
                return child.GetComponent<Renderer>();
        }

        return null;
    }

    public static void Apply(Transform root, Color glowColor)
    {
        Apply(FindGlowRenderer(root), glowColor);
    }

    public static void Apply(Renderer renderer, Color glowColor)
    {
        if (renderer == null) return;

        Material mat = renderer.material;
        mat.EnableKeyword("_EMISSION");

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor(EmissionColorID, glowColor);
        block.SetColor(BaseColorID, glowColor);
        renderer.SetPropertyBlock(block);
    }
}
