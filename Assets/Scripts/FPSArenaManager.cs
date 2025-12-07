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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Rpc(SendTo.Everyone)]
    public void StartDuelRpc()
    {
        if (IsServer)
        {
            foreach (var ph in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                ph.ResetHealthRpc();
            }
        }

        StartCoroutine(DuelTransitionRoutine(true, ClassType.Pawn));
    }

    [Rpc(SendTo.Everyone)]
    public void EndDuelRpc()
    {
        healthUI.SetActive(false);
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
        if (!IsServer) return;

        EndDuelRpc();
    }
}
