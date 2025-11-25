using Unity.Netcode;
using UnityEngine;

public class ChessGameNet : NetworkBehaviour
{
    public static ChessGameNet Instance { get; private set; }

    private ChessGame _game;

    void Awake()
    {
        Instance = this;
        _game = GetComponent<ChessGame>();
    }

    [Rpc(SendTo.Server)]
    public void SubmitMoveRpc(ulong pieceNetworkId, int file, int rank)
    {
        var pieceObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[pieceNetworkId];
        var piece = pieceObj.GetComponent<ChessPiece>();

        var targetSquare = _game.GetSquare(file, rank);
        if (targetSquare == null) return;

        bool moved = _game.TryMove(piece, targetSquare);
        if (!moved && piece.currentSquare != null)
        {
            piece.SetSquare(piece.currentSquare);
        }
    }
}
