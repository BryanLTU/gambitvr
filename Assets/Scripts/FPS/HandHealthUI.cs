using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HandHealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthFill;

    private PlayerHealth _localHealth;

    void Start()
    {
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

        if (_localHealth == null)
        {
            Debug.LogWarning("[HandHealthUI] Could not find PlayerHealth on local PlayerObject");
        }
    }

    void Update()
    {
        if (_localHealth == null || healthFill == null) return;

        healthFill.value = _localHealth.Health01;
    }
}
