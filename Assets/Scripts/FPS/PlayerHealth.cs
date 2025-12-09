using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using NUnit.Framework;

public class PlayerHealth : NetworkBehaviour
{
    [Min(1f)]
    [SerializeField] private float maxHealth = 100f;

    private NetworkVariable<float> _health = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float Health01 => _health.Value / maxHealth;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer) _health.Value = maxHealth;
    }

    [Rpc(SendTo.Everyone)]
    public void TakeDamageRpc(float amount)
    {
        if (!IsSessionOwner) return;

        _health.Value = Mathf.Max(0, _health.Value - amount);

        if (_health.Value <= 0f)
        {
            OnDeath();
        }
    }

    [Rpc(SendTo.Server)]
    public void ResetHealthRpc()
    {
        if (!IsServer) return;
        _health.Value = maxHealth;
    }

    private void OnDeath()
    {
        FPSArenaManager.Instance.PlayerDied(this);
    }
}