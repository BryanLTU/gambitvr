using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class FPSArenaManager : NetworkBehaviour
{
    public static FPSArenaManager Instance { get; private set; }

    [Header("Arena Spawn")]
    [SerializeField] private Transform spawnA;
    [SerializeField] private Transform spawnB;

    [Header("Local Player VR / FPS Setup")]
    [SerializeField] private Transform xrRigRoot;
    [SerializeField] private Image fadeImage;

    [Header("Arena Walls")]
    [SerializeField] private Transform wallsRoot;
    [SerializeField] private float wallsRiseDuration = 3f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Rpc(SendTo.Everyone)]
    public void StartDuelRpc()
    {
        StartCoroutine(DuelTransitionRoutine(true));
    }

    private IEnumerator DuelTransitionRoutine(bool iAmWhite)
    {
        yield return Fade(1f, 0.5f);

        Transform target = iAmWhite ? spawnA : spawnB;
        if (xrRigRoot != null && target != null)
        {
            xrRigRoot.SetPositionAndRotation(target.position, target.rotation);
        }

        yield return new WaitForSeconds(2f);

        yield return Fade(0f, 0.5f);

        yield return RaiseWallsRoutine();
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
}
