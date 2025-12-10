using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using NUnit.Framework;
using XRMultiplayer;

public class PlayerHealth : NetworkBehaviour
{
    [Min(1f)]
    [SerializeField] private float maxHealth = 100f;

    private float _health;
    public float Health01 => maxHealth > 0f ? _health / maxHealth : 0f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsSessionOwner)
        {
            _health = maxHealth;
            SendHealthToOwner(_health / maxHealth);
        }
    }

    public void ApplyDamage(float amount)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[PlayerHealth] ApplyDamage called on client, ignoring");
            return;
        }

        _health = Mathf.Max(0, _health - amount);

        SendHealthToOwner(_health / maxHealth);

        if (_health <= 0f)
        {
            OnDeath();
        }
    }

    [Rpc(SendTo.Server)]
    public void ResetHealthRpc()
    {
        if (!IsServer) return;
        _health = maxHealth;
        SendHealthToOwner(1f);
    }

    private void OnDeath()
    {
        FPSArenaManager.Instance.PlayerDied(this);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SendHealthClientRpc(float newHealth, RpcParams rpcParams = default)
    {
        var ui = FindFirstObjectByType<HandHealthUI>();
        if (ui != null)
            ui.SetHealth01(newHealth);
    }

    private void SendHealthToOwner(float health01)
    {
        if (!IsServer) return;

        var target = RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp);

        SendHealthClientRpc(health01, target);
    }
}