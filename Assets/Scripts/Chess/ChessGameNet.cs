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

    private bool _duelActive;
    private ulong _attackerNetId;
    private ulong _defenderNetId;
    private ulong _attackerClientId;
    private ulong _defenderClientId;
    private int _targetFile;
    private int _targetRank;

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

    [Rpc(SendTo.Server)]
    public void ForceDuelRpc(ulong attackerPieceNetId, ulong defenderPieceNetId, int targetFile, int targetRank, RpcParams rpcParams = default)
    {
        #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        return;
        #endif

        if (!IsSessionOwner) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerPieceNetId, out var aObj)) return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(defenderPieceNetId, out var dObj)) return;

        var attacker = aObj.GetComponent<ChessPiece>();
        var defender = dObj.GetComponent<ChessPiece>();
        if (!attacker || !defender) return;

        ulong sender = rpcParams.Receive.SenderClientId;
        if (!CanClientControlPiece(sender, attacker)) return;

        var targetSquare = _game.GetSquare(targetFile, targetRank);
        if (!targetSquare) return;

        StartFPSDuel(attacker, defender, targetSquare);
    }

    public void StartFPSDuel(ChessPiece attacker, ChessPiece defender, BoardSquare targetSquare)
    {
        if (!IsSessionOwner) return;

        var attackerNet = attacker.GetComponent<NetworkObject>();
        var defenderNet = defender.GetComponent<NetworkObject>();

        if (attackerNet == null || defenderNet == null)
        {
            Debug.LogError("[ChessGameNet] Duel pieces have no NetworkObject!");
            return;
        }

        ulong whiteClient = WhiteClientId.Value;
        ulong blackClient = BlackClientId.Value;

        ulong attackerClientId = (attacker.pieceColor == PieceColor.White) ? whiteClient : blackClient;
        ulong defenderClientId = (defender.pieceColor == PieceColor.White) ? whiteClient : blackClient;

        _attackerNetId = attackerNet.NetworkObjectId;
        _defenderNetId = defenderNet.NetworkObjectId;
        _attackerClientId = attackerClientId;
        _defenderClientId = defenderClientId;
        _targetFile = targetSquare.file;
        _targetRank = targetSquare.rank;
        _duelActive = true;

        _game.BuildSnapshotByPieceId(out var ids, out var files, out var ranks);
        FPSArenaManager.Instance.SnapArenaToBoardClientRpc(ids, files, ranks);

        FPSArenaManager.Instance.StartDuelRpc();
    }

    public void OnFPSDuelFinished(ulong loserClientId)
    {
        if (!IsSessionOwner) return;
        if (!_duelActive) return;

        bool attackerLost;

        if (loserClientId == _attackerClientId)
            attackerLost = true;
        else if (loserClientId == _defenderClientId)
            attackerLost = false;
        else
        {
            Debug.LogWarning("[ChessGameNet] Duel finished but loser client is neither attacker nor defender");
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_attackerNetId, out var attackerNetObj))
        {
            Debug.LogWarning("[ChessGameNet] Attacker object missing on duel resolution");
            _duelActive = false;
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_defenderNetId, out var defenderNetObj))
        {
            Debug.LogWarning("[ChessGameNet] Defender object missing on duel resolution");
            _duelActive = false;
            return;
        }

        var attackerPiece = attackerNetObj.GetComponent<ChessPiece>();
        var defenderPiece = defenderNetObj.GetComponent<ChessPiece>();

        var targetSquare = _game.GetSquare(_targetFile, _targetRank);

        if (targetSquare == null)
        {
            Debug.LogError("[ChessGameNet] Target square missing on duel resolution");
            _duelActive = false;
            return;
        }

        bool attackerWon = !attackerLost;

        _game.ResolveFpsDuel(attackerPiece, defenderPiece, targetSquare, attackerWon);

        ulong loserNetId   = attackerWon ? _defenderNetId   : _attackerNetId;
        ulong loserOwnerId = attackerWon ? _defenderClientId : _attackerClientId;

        HidePieceEverywhereClientRpc(loserNetId);
        DespawnPieceByActualOwner(loserNetId);

        if (attackerWon)
        {
            UpdatePiecePositionClientRpc(_attackerNetId, _targetFile, _targetRank);
        }

        _duelActive = false;
    }

    private void DespawnPieceByActualOwner(ulong pieceNetId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetId, out var netObj))
        {
            Debug.LogWarning($"[ChessGameNet] DespawnPieceByActualOwner: piece {pieceNetId} not found on session owner");
            return;
        }

        ulong actualOwner = netObj.OwnerClientId;

        var target = RpcTarget.Single(actualOwner, RpcTargetUse.Temp);
        DespawnPieceClientRpc(pieceNetId, target);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void DespawnPieceClientRpc(ulong pieceNetId, RpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetId, out var netObj))
        {
            if (!netObj.IsOwner)
            {
                Debug.LogWarning($"[ChessGameNet] DespawnPieceClientRpc: local is not owner. local={NetworkManager.Singleton.LocalClientId} owner={netObj.OwnerClientId}");
                return;
            }

            netObj.Despawn(true);
        }
    }

    public bool TryGetLocalPlayerColor(out PieceColor color)
    {
        color = PieceColor.White;

        if (!IsClient)
            return false;
        
        ulong clientId = NetworkManager.Singleton.LocalClientId;

        if (WhiteClientId.Value == clientId)
        {
            color = PieceColor.White;
            return true;
        }
        else if (BlackClientId.Value == clientId)
        {
            color = PieceColor.Black;
            return true;
        }

        return false;
    }

    [Rpc(SendTo.Everyone)]
    private void HidePieceEverywhereClientRpc(ulong pieceNetId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetId, out var netObj))
        {
            var go = netObj.gameObject;

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
