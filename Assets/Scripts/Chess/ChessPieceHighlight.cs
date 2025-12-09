using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(ChessPiece))]
public class ChessPieceHighlight : MonoBehaviour
{
    [Header("Shader Graph property")]
    [SerializeField] private string highlightPropertyName = "_Highlight";
    [SerializeField] private float highlightOnValue = 0.6f;
    [SerializeField] private float highlightOffValue = 0f;

    private ChessPiece _piece;
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    private int _highlightId;
    private bool _isHighlighted;

    void Awake()
    {
        _piece = GetComponent<ChessPiece>();
        _renderers = GetComponentsInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
        _highlightId = Shader.PropertyToID(highlightPropertyName);
    }

    public ChessPiece Piece => _piece;

    public void SetHighlight(bool enabled)
    {
        if (_isHighlighted == enabled) return;
        _isHighlighted = enabled;

        float value = enabled ? highlightOnValue : highlightOffValue;

        foreach (var rend in _renderers)
        {
            if (rend == null) continue;

            rend.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_highlightId, value);
            rend.SetPropertyBlock(_mpb);
        }
    }
}
