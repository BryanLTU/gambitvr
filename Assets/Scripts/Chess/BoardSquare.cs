using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class BoardSquare : MonoBehaviour
{
    public int file;
    public int rank;

    [Tooltip("Optional: name like A1, B3 for debugging.")]
    public string squareName;

    public GameObject highlightPrefab;
    public GameObject recommendPrefab;
    GameObject _instance;
    GameObject _recommend;

    [SerializeField] private float previewScale = 1f;
    [SerializeField] private float yOffset = 0.002f;

    void Awake()
    {
        if (highlightPrefab && recommendPrefab)
        {
            _instance = Instantiate(highlightPrefab, transform);
            _recommend = Instantiate(recommendPrefab, transform);
            _instance.SetActive(false);
            _recommend.SetActive(false);
        }
    }

    public void SetHighlight(bool enable)
    {
        _instance?.SetActive(enable);
    }
    public void SetHighlightRecommend(bool enable)
    {
        _recommend?.SetActive(enable);
    }

    public void DrawPiecePreview(GameObject source, Material previewMaterial)
    {
        if (!source || !previewMaterial)
            return;

        var meshTuples = MeshPreviewCache.Get(source);
        if (meshTuples == null || meshTuples.Length == 0)
            return;

        foreach (var (meshFilter, renderer) in meshTuples)
        {
            var mesh = meshFilter.sharedMesh;
            if (!mesh)
                continue;

            var matrix = Matrix4x4.TRS(transform.position + Vector3.up * yOffset, transform.rotation, meshFilter.transform.lossyScale * previewScale);

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Graphics.DrawMesh(mesh, matrix, previewMaterial, gameObject.layer, null, i);
            }
        }
    }
}
