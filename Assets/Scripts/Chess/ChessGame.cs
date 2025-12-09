using System.Collections.Generic;
using UnityEngine;
using XRMultiplayer;

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
        var moves = new List<BoardSquare>();

        if (piece.currentSquare == null)
        {
            Debug.LogWarning("Could not get currentSquare");
            return moves;
        }

        int f = piece.currentSquare.file;
        int r = piece.currentSquare.rank;

        switch (piece.pieceType)
        {
            case PieceType.Pawn:
                GeneratePawnMoves(piece, f, r, moves);
                break;
            case PieceType.Rook:
                GenerateSlidingMoves(piece, f, r, moves,
                    new (int df, int dr)[]
                    {
                        (1, 0), (-1, 0), (0, 1), (0, -1)
                    });
                break;
            case PieceType.Bishop:
                GenerateSlidingMoves(piece, f, r, moves,
                    new (int df, int dr)[]
                    {
                        (1, 1), (1, -1), (-1, 1), (-1, -1)
                    });
                break;
            case PieceType.Queen:
                GenerateSlidingMoves(piece, f, r, moves,
                    new (int df, int dr)[]
                    {
                        (1, 0), (-1, 0), (0, 1), (0, -1),
                        (1, 1), (1, -1), (-1, 1), (-1, -1)
                    });
                break;
            case PieceType.Knight:
                GenerateKnightMoves(piece, f, r, moves);
                break;
            case PieceType.King:
                GenerateKingMoves(piece, f, r, moves);
                break;
        }

        return moves;
    }

    void GeneratePawnMoves(ChessPiece piece, int f, int r, List<BoardSquare> moves)
    {
        int direction = (piece.pieceColor == PieceColor.White) ? 1 : -1;
        int startRank = (piece.pieceColor == PieceColor.White) ? 1 : 6;

        int forwardRank = r + direction;

        if (InBounds(f, forwardRank) && GetPieceAt(f, forwardRank) == null)
        {
            AddSquareIfExists(f, forwardRank, moves);

            int doubleRank = r + 2 * direction;
            if (r == startRank && InBounds(f, doubleRank) && GetPieceAt(f, doubleRank) == null)
            {
                AddSquareIfExists(f, doubleRank, moves);
            }
        }

        int[] diagFiles = { f - 1, f + 1 };
        foreach ( int df in diagFiles)
        {
            int rf = forwardRank;
            if (!InBounds(df, rf)) return;

            var targetPiece = GetPieceAt(df, rf);
            if (targetPiece != null && targetPiece.pieceColor != piece.pieceColor)
            {
                AddSquareIfExists(df, rf, moves);
            }
        }
    }

    // (rook/bishop/queen)
    void GenerateSlidingMoves(ChessPiece piece, int f, int r, List<BoardSquare> moves, (int df, int dr)[] directions)
    {
        foreach (var dir in directions)
        {
            int file = f + dir.df;
            int rank = r + dir.dr;

            while (InBounds(file, rank))
            {
                var targetPiece = GetPieceAt(file, rank);
                if (targetPiece == null)
                {
                    AddSquareIfExists(file, rank, moves);
                }
                else
                {
                    if (targetPiece.pieceColor != piece.pieceColor)
                    {
                        AddSquareIfExists(file, rank, moves);
                    }

                    break;
                }

                file += dir.df;
                rank += dir.dr;
            }
        }
    }

    void GenerateKnightMoves(ChessPiece piece, int f, int r, List<BoardSquare> moves)
    {
        int[,] offsets = new int[,]
        {
            { 1, 2 }, { 2, 1 },
            { 2, -1 }, { 1, -2 },
            { -1, -2 }, { -2, -1 },
            { -2, 1 }, { -1, 2 }
        };

        for (int i = 0; i < offsets.GetLength(0); i++)
        {
            int file = f + offsets[i, 0];
            int rank = r + offsets[i, 1];

            if (!InBounds(file, rank)) continue;

            var targetPiece = GetPieceAt(file, rank);
            if (targetPiece == null || targetPiece.pieceColor != piece.pieceColor)
            {
                AddSquareIfExists(file, rank, moves);
            }
        }
    }

    void GenerateKingMoves(ChessPiece peice, int f, int r, List<BoardSquare> moves)
    {
        for (int df = -1; df <= 1; df++)
        {
            for (int dr = -1; dr <= 1; dr++)
            {
                if (df == 0 && dr == 0) continue;

                int file = f + df;
                int rank = r + dr;

                if (!InBounds(file, rank)) continue;

                var targetPiece = GetPieceAt(file, rank);
                if (targetPiece == null || targetPiece.pieceColor != peice.pieceColor)
                {
                    AddSquareIfExists(file, rank, moves);
                }
            }
        }
    }

    void AddSquareIfExists(int file, int rank, List<BoardSquare> moves)
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
            FPSArenaManager.Instance.StartDuelRpc();
        }

        var fromSquare = piece.currentSquare;
        board[fromSquare.file, fromSquare.rank] = null;
        board[targetSquare.file, targetSquare.rank] = piece;

        piece.SetSquare(targetSquare);

        currentTurn = (currentTurn == PieceColor.White) ? PieceColor.Black : PieceColor.White;

        return true;
    }
}
