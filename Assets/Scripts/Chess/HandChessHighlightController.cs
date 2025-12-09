using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRDirectInteractor))]
public class HandChessHighlightController : MonoBehaviour
{
    private XRDirectInteractor _interactor;
    private ChessPieceHighlight _currentHighlight;

    void Awake()
    {
        _interactor = GetComponent<XRDirectInteractor>();
    }

    void LateUpdate()
    {
        if (_interactor == null || _interactor.hasSelection)
        {
            SetCurrentHighlight(null);
            return;
        }

        ChessPieceHighlight best = null;
        float bestDistSqr = float.MaxValue;

        var hovered = _interactor.interactablesHovered;
        Vector3 handPos = _interactor.transform.position;

        foreach (var hoveredInteractable in hovered)
        {
            var mono = hoveredInteractable as MonoBehaviour;
            if (mono == null) continue;

            var highlight = mono.GetComponent<ChessPieceHighlight>();
            var piece = mono.GetComponent<ChessPiece>();

            if (highlight == null || piece == null) continue;

            if (ChessGameNet.Instance != null && !ChessGameNet.Instance.CanLocalPlayerControlPiece(piece)) continue;

            float distSqr = (mono.transform.position - handPos).sqrMagnitude;

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                best = highlight;
            }
        }

        SetCurrentHighlight(best);
    }

    void SetCurrentHighlight(ChessPieceHighlight newHighlight)
    {
        if (_currentHighlight == newHighlight) return;

        if (_currentHighlight != null)
        {
            _currentHighlight.SetHighlight(false);
        }

        _currentHighlight = newHighlight;

        if (_currentHighlight != null)
        {
            _currentHighlight.SetHighlight(true);
        }
    }
}
