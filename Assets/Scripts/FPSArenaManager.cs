using System.Collections;
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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Rpc(SendTo.Everyone)]
    public void StartDuelRpc()
    {
        if (IsSessionOwner)
        {
            foreach (var ph in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                ph.ResetHealthRpc();
            }
        }

        bool iAmWhite = IsSessionOwner;
        ClassType myClass = ClassType.Pawn;

        StartCoroutine(DuelTransitionRoutine(iAmWhite, myClass));
    }

    [Rpc(SendTo.Everyone)]
    public void EndDuelRpc()
    {
        healthUI.SetActive(false);

        // TODO: teleport players back to Chess board game
    }

    private IEnumerator DuelTransitionRoutine(bool iAmWhite, ClassType myClass)
    {
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
        SpawnWeaponAt(weaponSpawn, myClass);

        yield return new WaitForSeconds(2f);

        yield return Fade(0f, 0.5f);

        yield return RaiseWallsRoutine();

        yield return StartCountdown();
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

        EndDuelRpc();
    }

    private void SpawnWeaponAt(Transform weaponSpawn, ClassType myClass)
    {
        if (weaponSpawn == null) return;

        WeaponId weaponId = GetWeaponForClass(myClass);
        if (weaponId == WeaponId.None) return;

        var cfg = WeaponDatabase.Instance.Get(weaponId);
        if (cfg == null || cfg.weaponPrefab == null)
        {
            Debug.LogWarning($"[FPSArenaManager] No WeaponConfig / prefab for {weaponId}");
            return;
        }

        GameObject weaponObj = Instantiate(cfg.weaponPrefab, weaponSpawn.position, weaponSpawn.rotation);

        var weaponBase = weaponObj.GetComponent<WeaponBase>();
        if (weaponBase != null)
        {
            weaponBase.Initialise(cfg);
        }
    }

    private WeaponId GetWeaponForClass(ClassType classType)
    {
        switch (classType)
        {
            case ClassType.Pawn: return WeaponId.Rifle;
            case ClassType.Knight: return WeaponId.Bow;
            // TODO: Add other classes
            default: return WeaponId.Rifle;
        }
    }

    public void RequestHitscanShot(Vector3 origin, Vector3 direction, float maxRange, float damage, WeaponId weaponId)
    {
        RequestHitscanShotRpc(origin, direction, maxRange, damage, weaponId);
    }

    [Rpc(SendTo.Server)]
    private void RequestHitscanShotRpc(Vector3 origin, Vector3 direction, float maxRange, float damage, WeaponId weaponId, RpcParams rpcParams = default)
    {
        WeaponConfig cfg = WeaponDatabase.Instance != null ? WeaponDatabase.Instance.Get(weaponId) : null;
        Ray ray = new Ray(origin, direction);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, damageLayerMask, QueryTriggerInteraction.Ignore))
        {
            var health = hit.collider.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamageRpc(damage);
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
}
