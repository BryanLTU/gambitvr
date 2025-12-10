using Unity.Netcode;
using UnityEngine;

public class AutoTurret : NetworkBehaviour
{
    [Header("Shooting")]
    [SerializeField] WeaponConfig weaponConfig;

    [Header("Aim")]
    [SerializeField] private Transform muzzle;

    private float _timer;

    void Update()
    {
        if (!IsOwner && !IsSessionOwner) return;

        _timer += Time.deltaTime;
        if (_timer >= weaponConfig.fireRate)
        {
            _timer = 0f;
            Fire();
        }
    }

    void Fire()
    {
        Vector3 origin = muzzle.position;
        Vector3 dir = muzzle.forward;

        FPSArenaManager.Instance.RequestHitscanShot(origin, dir, weaponConfig.maxRange, weaponConfig.damage, weaponConfig.weaponId);
    }
}
