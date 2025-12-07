using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HandHealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthFill;
    // [SerializeField] private Transform cameraTransform;

    private PlayerHealth _localHealth;

    void Start()
    {
        /*if (!cameraTransform)
            cameraTransform = Camera.main.transform;*/

        TryGetLocalHealth();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        TryGetLocalHealth();
    }

    private void TryGetLocalHealth()
    {
        if (NetworkManager.Singleton == null) return;

        var localClient = NetworkManager.Singleton.LocalClient;
        if (localClient == null) return;

        var playerObj = localClient.PlayerObject;
        if (playerObj == null) return;

        _localHealth = playerObj.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (_localHealth == null) return;

        healthFill.value = _localHealth.Health01;

        // face the camera | Not needed for now
        /*Vector3 dir = (transform.position - cameraTransform.position).normalized;
        transform.rotation = Quaternion.LookRotation(dir);*/
    }
}
