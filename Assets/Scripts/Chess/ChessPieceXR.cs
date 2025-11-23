using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Gaze;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(ChessPiece))]
[RequireComponent(typeof(XRGrabInteractable))]
public class ChessPieceXR : MonoBehaviour
{
    private ChessPiece _piece;
    private XRGrabInteractable _grabInteractable;
    private List<BoardSquare> _currentLegalMoves;

    void Awake()
    {
        _piece = GetComponent<ChessPiece>();
        _grabInteractable = GetComponent<XRGrabInteractable>();

        _grabInteractable.selectEntered.AddListener(OnGrab);
        _grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        if (_grabInteractable == null) return;
        _grabInteractable.selectEntered.RemoveListener(OnGrab);
        _grabInteractable.selectExited.RemoveListener(OnRelease);        
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;
        
        Debug.Log($"OnGarb by {args.interactorObject.transform.name}");
        _currentLegalMoves = ChessGame.Instance.GetLegalMoves(_piece);
        foreach (var sq in _currentLegalMoves)
            sq.SetHighlight(true);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;

        if (_currentLegalMoves != null)
        {
            foreach (var sq in _currentLegalMoves)
                sq.SetHighlight(false);
        }

        BoardSquare target = FindClosestSquare();

        if (target != null)
        {
            bool moved = ChessGame.Instance.TryMove(_piece, target);
            if (!moved)
            {
                _piece.SetSquare(_piece.currentSquare);
            }
        } else
        {
            _piece.SetSquare(_piece.currentSquare);
        }
    }

    BoardSquare FindClosestSquare()
    {
        float bestDist = float.MaxValue;
        BoardSquare best = null;

        foreach (var sq in ChessGame.Instance.GetAllBoardSquares())
        {
            float d = Vector3.Distance(transform.position, sq.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = sq;
            }
        }

        return best;
    }
}
