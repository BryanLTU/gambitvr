using System.Collections.Generic;
using UnityEngine;

public class ChessGame : MonoBehaviour
{
    [SerializeField]
    private BoardSquare[] allSquares;
    private Dictionary<(int, int), BoardSquare> squareLookup;

    public static ChessGame Instance { get; private set; }

    public PieceColor currentTurn = PieceColor.White;

    private ChessPiece[,] board = new ChessPiece[8, 8];

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        squareLookup = new Dictionary<(int, int), BoardSquare>();
        foreach (var sq in allSquares)
        {
            squareLookup[(sq.file, sq.rank)] = sq;
        }

        AutoRegsiterAllPieces();
    }

    void AutoRegsiterAllPieces()
    {
        ChessPiece[] pieces = GetComponentsInChildren<ChessPiece>();

        foreach (var piece in pieces)
        {
            BoardSquare closest = FindClosestSqaure(piece.transform.position);

            if (closest) RegisterPiece(piece, closest);
            else Debug.LogWarning($"[ChessGame] Could not find square for piece {piece.name}");
        }
    }

    BoardSquare FindClosestSqaure(Vector3 worldPos)
    {
        BoardSquare best = null;
        float bestDist = float.MaxValue;

        foreach (var sq in allSquares)
        {
            if (sq == null) continue;

            float d = Vector3.SqrMagnitude(sq.transform.position - worldPos);
            if (d < bestDist)
            {
                bestDist = d;
                best = sq;
            }
        }

        return best;
    }

    public BoardSquare GetSquare(int file, int rank)
    {
        squareLookup.TryGetValue((file, rank), out var sq);
        return sq;
    }

    public BoardSquare[] GetAllBoardSquares()
    {
        return allSquares;
    }

    public void RegisterPiece(ChessPiece piece, BoardSquare square)
    {
        piece.SetSquare(square);
        board[square.file, square.rank] = piece;
    }

    public ChessPiece GetPieceAt(int file, int rank)
    {
        return board[file, rank];
    }

    bool InBounds(int file, int rank)
    {
        return file >= 0 && file < 8 && rank >= 0 && rank < 8;
    }

    public List<BoardSquare> GetLegalMoves(ChessPiece piece)
    {
        //TODO will need to implement login, depending on type, color and current state of board
        var moves = new List<BoardSquare>();

        if (piece.currentSquare == null)
        {
            Debug.LogWarning("Could not get currentSquare");
            return moves;
        }

        switch (piece.pieceType)
        {
            case PieceType.Pawn:
                GeneratePawnMoves(piece, piece.currentSquare.file, piece.currentSquare.rank, moves);
                break;
        }

        return moves;
    }

    void GeneratePawnMoves(ChessPiece piece, int file, int rank, List<BoardSquare> moves)
    {
        int direction = (piece.pieceColor == PieceColor.White) ? 1 : -1;
        int startRank = (piece.pieceColor == PieceColor.White) ? 1 : 6;

        int forwardRank = rank + direction;

        if (InBounds(file, forwardRank) && GetPieceAt(file, forwardRank) == null)
        {
            AddSqaureIfExists(file, forwardRank, moves);

            int doubleRank = rank + 2 * direction;
            if (rank == startRank && InBounds(file, doubleRank) && GetPieceAt(file, doubleRank) == null)
            {
                AddSqaureIfExists(file, doubleRank, moves);
            }
        }

        int[] diagFiles = { file - 1, file + 1 };
        foreach ( int df in diagFiles)
        {
            int rf = forwardRank;
            if (!InBounds(df, rf)) return;

            var targetPiece = GetPieceAt(df, rf);
            if (targetPiece != null && targetPiece.pieceColor != piece.pieceColor)
            {
                AddSqaureIfExists(df, rf, moves);
            }
        }
    }

    void AddSqaureIfExists(int file, int rank, List<BoardSquare> moves)
    {
        var sq = GetSquare(file, rank);
        if (sq != null)
            moves.Add(sq);
    }

    public bool TryMove(ChessPiece piece, BoardSquare targetSquare)
    {
        if (piece.pieceColor != currentTurn)
            return false;

        var legal = GetLegalMoves(piece);
        if (!legal.Contains(targetSquare))
            return false;
        
        var existing = GetPieceAt(targetSquare.file, targetSquare.rank);
        if (existing != null && existing.pieceColor != piece.pieceColor)
        {
            // Capture logic. TODO: implement event system to transition to FPS
            Destroy(existing.gameObject);
        }

        var fromSquare = piece.currentSquare;
        board[fromSquare.file, fromSquare.rank] = null;
        board[targetSquare.file, targetSquare.rank] = piece;

        piece.SetSquare(targetSquare);

        currentTurn = (currentTurn == PieceColor.White) ? PieceColor.Black : PieceColor.White;

        return true;
    }
}
