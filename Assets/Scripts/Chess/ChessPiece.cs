using Unity.Netcode;
using UnityEngine;

public enum PieceColor { White, Black }
public enum PieceType { Pawn, Rook, Knight, Bishop, Queen, King }

[RequireComponent(typeof(Collider))]
public class ChessPiece : NetworkBehaviour
{
    public PieceType pieceType;
    public PieceColor pieceColor;

    [HideInInspector]
    public BoardSquare currentSquare;

    Rigidbody _rb;

    public bool IsGrabbed = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!IsGrabbed && currentSquare != null)
        {
            if (!_rb.isKinematic || _rb.useGravity)
            {
                SetSquare(currentSquare);
                LockToBoard();
            }
        }
    }

    public void LockToBoard()
    {
        if (_rb == null) return;

        _rb.isKinematic = true;
        _rb.useGravity = false;
    }

    public void FreeFromBoard()
    {
        if (_rb == null) return;

        Debug.Log($"{name} freefromboard");
        _rb.isKinematic = false;
        _rb.useGravity = true;
    }

    public void SetSquare(BoardSquare square)
    {
        currentSquare = square;
        if (square != null)
        {
            transform.SetPositionAndRotation(square.transform.position + new Vector3(0f, 0.01f, 0f), Quaternion.identity);
        }

        LockToBoard();
    }
}
