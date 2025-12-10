using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Gaze;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(ChessPiece))]
[RequireComponent(typeof(XRGrabInteractable))]
public class ChessPieceXR : NetworkBehaviour
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
        base.OnDestroy();
        if (_grabInteractable == null) return;
        _grabInteractable.selectEntered.RemoveListener(OnGrab);
        _grabInteractable.selectExited.RemoveListener(OnRelease);        
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;

        if (!ChessGameNet.Instance.CanLocalPlayerControlPiece(_piece))
        {
            var interactor = args.interactorObject;
            if (interactor != null)
            {
                args.manager.SelectExit(interactor, _grabInteractable);
            }
            return;
        }

        _piece.IsGrabbed = true;
        _piece.FreeFromBoard();

        _currentLegalMoves = ChessGame.Instance.GetLegalMoves(_piece);
        foreach (var sq in _currentLegalMoves)
            sq.SetHighlightRecommend(true);

        if (_piece.TryGetComponent<Collider>(out var col))
            col.enabled = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;

        if (!ChessGameNet.Instance.CanLocalPlayerControlPiece(_piece)) return;

        if (_currentLegalMoves != null)
        {
            foreach (var sq in _currentLegalMoves)
            {
                sq.SetHighlight(false);
                sq.SetHighlightRecommend(false);
            }
            _currentLegalMoves = null;
        }

        BoardSquare target = FindClosestSquare();

        var netObj = _piece.GetComponent<NetworkObject>();
        if (target != null)
        {
            ChessGameNet.Instance.SubmitMoveRpc(netObj.NetworkObjectId, target.file, target.rank);
        }
        else
        {
            if (_piece.currentSquare != null)
            {
                ChessGameNet.Instance.SubmitMoveRpc(netObj.NetworkObjectId,_piece.currentSquare.file, _piece.currentSquare.rank);
            }
        }
        _piece.IsGrabbed = false;
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

        if (bestDist > 2f)
        {
            return null;
        }

        return best;
    }

    void Update()
    {
        if (!_piece.IsGrabbed || _currentLegalMoves == null) return;

        foreach (var sq in _currentLegalMoves)
        {
            float distance = Vector3.Distance(transform.position, sq.transform.position);
            float threshold = 0.05f;

            if (distance < threshold)
            {
                sq.SetHighlight(true);
                sq.SetHighlightRecommend(false);

            }
            else
            {
                sq.SetHighlight(false);
                sq.SetHighlightRecommend(true);
            }
        }
    }
}
