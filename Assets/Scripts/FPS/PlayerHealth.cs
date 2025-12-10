using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using NUnit.Framework;
using XRMultiplayer;

public class PlayerHealth : NetworkBehaviour
{
    [Min(1f)]
    [SerializeField] private float maxHealth = 100f;

    private NetworkVariable<float> _health = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float Health01 => _health.Value / maxHealth;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (NetworkManager.Singleton.IsServer) _health.Value = maxHealth;

        _health.OnValueChanged += OnHealthChanged;
    }

    private void OnDestroy()
    {
        base.OnDestroy();
        _health.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        Utils.Log($"[PlayerHealth] {OwnerClientId} health changed: {oldValue} -> {newValue}");
    }

    public void ApplyDamage(float amount)
    {
        Debug.Log($"IsSessionOwner={IsSessionOwner} IsServer={IsServer} {NetworkManager.Singleton.IsServer} IsHost={IsHost} {NetworkManager.Singleton.IsHost}");
        if (!NetworkManager.Singleton.IsServer) return;

        _health.Value = Mathf.Max(0, _health.Value - amount);

        if (_health.Value <= 0f)
        {
            OnDeath();
        }
    }

    [Rpc(SendTo.Server)]
    public void ResetHealthRpc()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        _health.Value = maxHealth;
    }

    private void OnDeath()
    {
        FPSArenaManager.Instance.PlayerDied(this);
    }
}