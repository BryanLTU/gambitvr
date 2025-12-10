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
}
