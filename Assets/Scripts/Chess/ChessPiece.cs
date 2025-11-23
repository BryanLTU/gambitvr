using UnityEngine;

public enum PieceColor { White, Black }
public enum PieceType { Pawn, Rook, Knight, Bishop, Queen, King }

[RequireComponent(typeof(Collider))]
public class ChessPiece : MonoBehaviour
{
    public PieceType pieceType;
    public PieceColor pieceColor;

    [HideInInspector]
    public BoardSquare currentSquare;

    public void SetSquare(BoardSquare square)
    {
        currentSquare = square;
        if (square != null)
        {
            transform.position = square.transform.position;
        }
    }
}
