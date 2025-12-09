using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using XRMultiplayer;

public class ChessGameNet : NetworkBehaviour
{
    public static ChessGameNet Instance { get; private set; }

    private ChessGame _game;

    NetworkVariable<ulong> WhiteClientId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<ulong> BlackClientId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly List<ulong> _connectedClients = new();

    void Awake()
    {
        Instance = this;
        _game = GetComponent<ChessGame>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsSessionOwner) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!_connectedClients.Contains(id))
            {
                _connectedClients.Add(id);
            }
        }

        TryAssignSides();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!IsSessionOwner) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!_connectedClients.Contains(clientId))
            _connectedClients.Add(clientId);

        TryAssignSides();
    }

    private void OnClientDisconnect(ulong clientId)
    {
        _connectedClients.Remove(clientId);

        WhiteClientId.Value = 0;
        BlackClientId.Value = 0;
    }

    private void TryAssignSides()
    {
        // Need both players
        if (!IsSessionOwner || _connectedClients.Count < 2) return;
        // Already assigned
        if (WhiteClientId.Value != 0 || BlackClientId.Value != 0) return;

        ulong hostId = NetworkManager.Singleton.LocalClientId;
        ulong otherId = _connectedClients.First(id => id != hostId);

        WhiteClientId.Value = hostId;
        BlackClientId.Value = otherId;

        Debug.Log($"[ChessGameNet] Assigned sides: White={WhiteClientId.Value}, Black={BlackClientId.Value}");
    }

    public bool CanClientControlPiece(ulong clientId, ChessPiece piece)
    {
        // If sides not assigned, allow to move pieces
        if (WhiteClientId.Value == 0 && BlackClientId.Value == 0) return true;

        return piece.pieceColor == PieceColor.White ? WhiteClientId.Value == clientId : BlackClientId.Value == clientId;
    }

    public bool CanLocalPlayerControlPiece(ChessPiece piece)
    {
        if (!NetworkManager.Singleton.IsClient)
            return false;

        ulong localId = NetworkManager.Singleton.LocalClientId;
        return CanClientControlPiece(localId, piece);
    }

    [Rpc(SendTo.Server)]
    public void SubmitMoveRpc(ulong pieceNetworkId, int file, int rank, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        var pieceObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[pieceNetworkId];
        var piece = pieceObj.GetComponent<ChessPiece>();

        if (!CanClientControlPiece(senderId, piece))
            return;

        var targetSquare = _game.GetSquare(file, rank);
        if (targetSquare == null) return;

        bool moved = _game.TryMove(piece, targetSquare);
        if (!moved && piece.currentSquare != null)
        {
            piece.SetSquare(piece.currentSquare);
        }
        else
        {
            UpdatePiecePositionClientRpc(pieceNetworkId, file, rank);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void UpdatePiecePositionClientRpc(ulong pieceNetworkId, int file, int rank)
    {
        var pieceObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[pieceNetworkId];
        var piece = pieceObj.GetComponent<ChessPiece>();

        var targetSquare = _game.GetSquare(file, rank);
        if (targetSquare != null)
        {
            piece.SetSquare(targetSquare);
        }
    }
}
