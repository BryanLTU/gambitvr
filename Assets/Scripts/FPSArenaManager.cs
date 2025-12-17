using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

public class FPSArenaManager : NetworkBehaviour
{
    public static FPSArenaManager Instance { get; private set; }

    [Header("Arena Spawn")]
    [SerializeField] private Transform spawnA;
    [SerializeField] private Transform spawnB;

    [Header("Weapon Spawns")]
    [SerializeField] private Transform weaponSpawnA;
    [SerializeField] private Transform weaponSpawnB;

    [Header("Local Player VR / FPS Setup")]
    [SerializeField] private Transform xrRigRoot;
    [SerializeField] private Image fadeImage;

    [Header("Local Player Class / Loadout")]
    [SerializeField] private PlayerClass localPlayerClass;

    [Header("Arena Walls")]
    [SerializeField] private Transform wallsRoot;
    [SerializeField] private float wallsRiseDuration = 3f;

    [Header("Health")]
    [SerializeField] private GameObject healthUI;
    [SerializeField] private LayerMask damageLayerMask;

    [Header("Return to Chess Board")]
    [SerializeField] private Transform chessSpawnWhite;
    [SerializeField] private Transform chessSpawnBlack;

    [Header("Arena Board Sync")]
    [SerializeField] private BoardSquareMap arenaSquareMap;
    [SerializeField] private Transform arenaPiecesRoot;

    private Vector3 _wallsStartPos;
    private Vector3 _savedXrPos;
    private Quaternion _savedXrRot;

    private readonly List<ulong> _spawnedWeaponNetIds = new();

    private readonly Dictionary<int, ChessPiece> _arenaById = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (wallsRoot != null)
            _wallsStartPos = wallsRoot.position;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _arenaById.Clear();

        foreach (var piece in arenaPiecesRoot.GetComponentsInChildren<ChessPiece>(true))
        {
            if (!piece.TryGetComponent<PieceIdentity>(out var ident)) continue;
            _arenaById[ident.pieceId] = piece;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void StartDuelRpc(
        ulong attackerClientId,
        ulong defenderClientId,
        PieceType attackerPieceType,
        PieceType defenderPieceType
    )
    {
        if (IsSessionOwner)
        {
            foreach (var ph in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                ph.ResetHealthRpc();
            }
        }

        ulong localId = NetworkManager.Singleton.LocalClientId;

        bool iAmAttacker = localId == attackerClientId;
        bool iAmDefender = localId == defenderClientId;

        if (!iAmAttacker && !iAmDefender)
        {
            Debug.LogWarning("Not starting duel due to not matching either attacker or defender id");
            return;
        }

        bool iAmWhite = IsSessionOwner;
        ClassType myClass = iAmAttacker ? GetClassTypeForPieceType(attackerPieceType) : GetClassTypeForPieceType(defenderPieceType);

        StartCoroutine(DuelTransitionRoutine(iAmWhite, myClass));
    }

    [Rpc(SendTo.Everyone)]
    public void EndDuelRpc()
    {
        healthUI.SetActive(false);

        StartCoroutine(ReturnFromDuelRoutine());
    }

    private IEnumerator DuelTransitionRoutine(bool iAmWhite, ClassType myClass)
    {
         if (xrRigRoot != null)
        {
            _savedXrPos = xrRigRoot.position;
            _savedXrRot = xrRigRoot.rotation;
        }

        yield return Fade(1f, 0.5f);

        Transform target = iAmWhite ? spawnA : spawnB;
        if (xrRigRoot != null && target != null)
        {
            xrRigRoot.SetPositionAndRotation(target.position, target.rotation);
        }

        if (localPlayerClass != null)
        {
            localPlayerClass.AssignClass(myClass);
        }

        healthUI.SetActive(true);

        Transform weaponSpawn = iAmWhite ? weaponSpawnA : weaponSpawnB;
        if (weaponSpawn != null)
        {
            SpawnWeaponServerRpc(NetworkManager.Singleton.LocalClientId, weaponSpawn.position, weaponSpawn.rotation, myClass);
        }

        yield return new WaitForSeconds(2f);

        yield return Fade(0f, 0.5f);

        yield return RaiseWallsRoutine();

        yield return StartCountdown();
    }

    private IEnumerator ReturnFromDuelRoutine()
    {
        yield return Fade(1f, 0.5f);

        bool iAmWhite = IsSessionOwner;

        if (xrRigRoot != null)
        {
            Transform chessSpawn = iAmWhite ? chessSpawnWhite : chessSpawnBlack;

            if (chessSpawn != null)
            {
                xrRigRoot.SetPositionAndRotation(chessSpawn.position, chessSpawn.rotation);
            }
            else
            {
                xrRigRoot.SetPositionAndRotation(_savedXrPos, _savedXrRot);
            }
        }

        if (wallsRoot != null)
            wallsRoot.position = _wallsStartPos;

        yield return Fade(0f, 0.5f);
    }


    private IEnumerator Fade(float targetAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        Color startColor = fadeImage.color;
        float start = fadeImage.color.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            fadeImage.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(start, targetAlpha, t / duration));
            yield return null;
        }

        fadeImage.color = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
    }

    private IEnumerator RaiseWallsRoutine()
    {
        float t = 0f;

        Vector3 startPos = wallsRoot.position;
        Vector3 endPos = startPos + Vector3.up * 4f;

        while (t < wallsRiseDuration)
        {
            t += Time.deltaTime;
            float a = t / wallsRiseDuration;
            wallsRoot.position = Vector3.Lerp(startPos, endPos, a);

            yield return null;
        }

        wallsRoot.position = endPos;
    }

    private IEnumerator StartCountdown()
    {
        //TODO change to actual in scene number visuals (e.g. in the middle of the arena)
        PlayerHudNotification.Instance.ShowText("Game starts in 3", 1);
        yield return new WaitForSeconds(2);

        for (int i = 2; i > 0; i--)
        {
            PlayerHudNotification.Instance.ShowText($"{i}", 1);
            yield return new WaitForSeconds(2);
        }
    }

    public void PlayerDied(PlayerHealth deadPlayer)
    {
        if (!IsSessionOwner) return;

        ulong loserClientId = deadPlayer.OwnerClientId;

        if (ChessGameNet.Instance != null)
        {
            ChessGameNet.Instance.OnFPSDuelFinished(loserClientId);
        }

        CleanupWeapons();

        EndDuelRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SpawnWeaponServerRpc(ulong ownerClientId, Vector3 position, Quaternion rotation, ClassType classType, RpcParams rpcParams = default)
    {
        WeaponId weaponId = GetWeaponForClass(classType);
        if (weaponId == WeaponId.None)
        {
            Debug.LogWarning($"[FPSArenaManager] Could not find weapon for class={classType}");
            return;   
        }

        var cfg = WeaponDatabase.Instance.Get(weaponId);
        if (cfg == null || cfg.weaponPrefab == null)
        {
            Debug.LogWarning($"[FPSArenaManager] No WeaponConfig / prefab for {weaponId}");
            return;
        }

        GameObject weaponObj = Instantiate(cfg.weaponPrefab, position, rotation);

        if (!weaponObj.TryGetComponent<NetworkObject>(out var netObj))
        {
            Debug.LogError("[FPSArenaManager] Weapon prefab does not have a NetworkObject component");
            Destroy(weaponObj);
            return;
        }

        // netObj.SpawnWithOwnership(ownerClientId);
        netObj.Spawn();

        _spawnedWeaponNetIds.Add(netObj.NetworkObjectId);

        InitWeaponClientRpc(netObj.NetworkObjectId, weaponId);

        if (weaponObj.TryGetComponent<WeaponBase>(out var weaponBase))
        {
            weaponBase.Initialise(cfg);
        }

        Debug.Log($"Weapon spawned for client {ownerClientId} at position {position}");
    }

    [Rpc(SendTo.Everyone)]
    private void InitWeaponClientRpc(ulong weaponNetId, WeaponId weaponId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(weaponNetId, out var netObj))
        {
            if (netObj.TryGetComponent<WeaponBase>(out var weaponBase))
            {
                var cfg = WeaponDatabase.Instance.Get(weaponId);
                weaponBase.Initialise(cfg);
            }
        }
    }

    private WeaponId GetWeaponForClass(ClassType classType)
    {
        switch (classType)
        {
            case ClassType.Pawn: return WeaponId.Rifle;
            case ClassType.Knight: return WeaponId.SMG;
            case ClassType.Rook: return WeaponId.Sniper;
            case ClassType.Queen: return WeaponId.LMG;
            case ClassType.Bishop: return WeaponId.AR;
            default: return WeaponId.Rifle;
        }
    }

    private ClassType GetClassTypeForPieceType(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => ClassType.Pawn,
            PieceType.Knight => ClassType.Knight,
            PieceType.Rook => ClassType.Rook,
            PieceType.Bishop => ClassType.Bishop,
            PieceType.Queen => ClassType.Queen,
            PieceType.King => ClassType.King,
            _ => ClassType.None,
        };
    }

    public void RequestHitscanShot(Vector3 origin, Vector3 direction, float maxRange, float damage, WeaponId weaponId)
    {
        Utils.Log("[FPSArenaManager] RequestHitscanShotRpc called");
        RequestHitscanShotRpc(origin, direction, maxRange, damage, weaponId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void RequestHitscanShotRpc(Vector3 origin, Vector3 direction, float maxRange, float damage, WeaponId weaponId, RpcParams rpcParams = default)
    {
        WeaponConfig cfg = WeaponDatabase.Instance != null ? WeaponDatabase.Instance.Get(weaponId) : null;
        Ray ray = new Ray(origin, direction);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, damageLayerMask, QueryTriggerInteraction.Collide))
        {
            Utils.Log("[FPSArenaManager] Shot and hit");
            var health = hit.collider.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.ApplyDamage(damage);
            }
            
            ApplyKnockback(hit, direction, cfg);

            BroadcastHitscanShotRpc(origin, hit.point, weaponId);
        }
        else
        {
            Vector3 endPoint = origin + direction * maxRange;
            BroadcastHitscanShotRpc(origin, endPoint, weaponId);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void BroadcastHitscanShotRpc(Vector3 origin, Vector3 hitPoint, WeaponId weaponId, RpcParams rpcParams = default)
    {
        WeaponConfig cfg = WeaponDatabase.Instance != null ? WeaponDatabase.Instance.Get(weaponId) : null;

        SpawnTracer(origin, hitPoint, cfg);
        SpawnImpact(hitPoint, cfg);
    }

    private void SpawnTracer(Vector3 from, Vector3 to, WeaponConfig weaponConfig)
    {
        if (weaponConfig.tracerPrefab == null) return;

        GameObject tracerObj = Instantiate(weaponConfig.tracerPrefab);
        var lr = tracerObj.GetComponent<LineRenderer>();
        
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
        }

        Destroy(tracerObj, weaponConfig.tracerDuration);
    }

    private void SpawnImpact(Vector3 at, WeaponConfig weaponConfig)
    {
        if (weaponConfig.impactPrefab == null) return;

        GameObject impactObject = Instantiate(weaponConfig.impactPrefab, at, Quaternion.identity);
        Destroy(impactObject, weaponConfig.impactDuration);
    }

    private void ApplyKnockback(RaycastHit hit, Vector3 direction, WeaponConfig weaponConfig)
    {
        if (weaponConfig.knockbackForce <= 0f) return;

        Rigidbody rb = hit.rigidbody ?? hit.collider.attachedRigidbody;
        if (rb == null) return;

        if (!weaponConfig.knockbackPlayers && rb.GetComponent<PlayerHealth>() != null) return;

        if (!weaponConfig.knockbackChessPieces && rb.transform.CompareTag("Chess")) return;

        rb.AddForceAtPosition(direction.normalized * weaponConfig.knockbackForce, hit.point, ForceMode.Impulse);
    }

    private void CleanupWeapons()
    {
        if (!IsSessionOwner) return;

        var spawnManager = NetworkManager.Singleton.SpawnManager;

        for (int i = _spawnedWeaponNetIds.Count - 1; i >= 0; i--)
        {
            ulong netId = _spawnedWeaponNetIds[i];

            HideNetObjectEverywhereClientRpc(netId);
            DespawnByActualOwner(netId);

            _spawnedWeaponNetIds.RemoveAt(i);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SnapArenaToBoardClientRpc(int[] pieceIds, int[] files, int[] ranks)
    {
        if (arenaSquareMap == null) return;

        for (int i = 0; i < pieceIds.Length; i++)
        {
            if (!_arenaById.TryGetValue(pieceIds[i], out var arenaPiece)) continue;

            var sq = arenaSquareMap.GetSquare(files[i], ranks[i]);
            if (sq == null) continue;

            arenaPiece.currentSquare = sq;

            var netObj = arenaPiece.GetComponent<NetworkObject>();
            if (netObj == null || !netObj.IsOwner) continue;

            var nt = arenaPiece.GetComponent<ClientNetworkTransform>();
            Vector3 pos = sq.transform.position + new Vector3(0f, 0.01f, 0f);

            if (nt != null)
            {
                nt.Teleport(pos, Quaternion.identity, arenaPiece.transform.localScale);
            }
            else
            {
                arenaPiece.transform.SetPositionAndRotation(pos, Quaternion.identity);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SetArenaPieceActiveClientRpc(int pieceId, bool active)
    {
        if (_arenaById.TryGetValue(pieceId, out var arenaPiece) && arenaPiece != null)
        {
            arenaPiece.gameObject.SetActive(active);
        }
    }

    private void DespawnByActualOwner(ulong netId)
    {
        var sm = NetworkManager.Singleton.SpawnManager;

        if (!sm.SpawnedObjects.TryGetValue(netId, out var netObj) || netObj == null || !netObj.IsSpawned)
            return;

        ulong owner = netObj.OwnerClientId;

        var target = RpcTarget.Single(owner, RpcTargetUse.Temp);
        DespawnNetObjectClientRpc(netId, target);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void DespawnNetObjectClientRpc(ulong netId, RpcParams rpcParams = default)
    {
        var sm = NetworkManager.Singleton.SpawnManager;

        if (sm.SpawnedObjects.TryGetValue(netId, out var netObj) && netObj != null && netObj.IsSpawned)
        {
            if (!netObj.IsOwner)
            {
                Debug.LogWarning($"[FPSArenaManager] DespawnNetObjectClientRpc: not owner. local={NetworkManager.Singleton.LocalClientId} owner={netObj.OwnerClientId} netId={netId}");
                return;
            }

            netObj.Despawn(true);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void HideNetObjectEverywhereClientRpc(ulong netId)
    {
        var sm = NetworkManager.Singleton.SpawnManager;

        if (sm.SpawnedObjects.TryGetValue(netId, out var netObj) && netObj != null)
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
