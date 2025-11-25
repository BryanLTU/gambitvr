using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class BoardSquare : MonoBehaviour
{
    public int file;
    public int rank;

    [Tooltip("Optional: name like A1, B3 for debugging.")]
    public string squareName;

    public GameObject highlightPrefab;
    GameObject _instance;

    void Awake()
    {
        if (highlightPrefab)
        {
            _instance = Instantiate(highlightPrefab, transform);
            _instance.SetActive(false);
        }
    }

    public void SetHighlight(bool enable)
    {
        _instance?.SetActive(enable);
    }
}
