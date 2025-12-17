using UnityEngine;
using UnityEngine.Rendering;

public static class PreviewMaterialFactory
{
    static Material _moveMaterial;
    static Material _captureMaterial;

    public static Material Move
    {
        get
        {
            Ensure();
            return _moveMaterial;
        }
    }

    public static Material Capture
    {
        get
        {
            Ensure();
            return _captureMaterial;
        }
    }

    static void Ensure()
    {
        if (_moveMaterial != null && _captureMaterial != null)
            return;

        string shaderName = GraphicsSettings.currentRenderPipeline
            ? "Universal Render Pipeline/Lit"
            : "Standard";

        var shader = Shader.Find(shaderName);
        if (!shader)
        {
            Debug.LogWarning($"PreviewMaterialFactory: Shader '{shaderName}' not found.");
            return;
        }

        if (_moveMaterial == null)
        {
            _moveMaterial = new Material(shader);
            SetFade(_moveMaterial, new Color(1f, 1f, 1f, 0.3f));
        }

        if (_captureMaterial == null)
        {
            _captureMaterial = new Material(shader);
            SetFade(_captureMaterial, new Color(1f, 0f, 0f, 0.6f));
        }
    }

    static void SetFade(Material mat, Color color)
    {
        mat.color = color;

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1);

        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0);

        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = (int)RenderQueue.Transparent;

        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }
}
