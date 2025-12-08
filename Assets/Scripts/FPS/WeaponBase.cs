using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponConfig config;

    protected float lastShotTime;

    public virtual void Initialise(WeaponConfig cfg)
    {
        config = cfg;
    }

    public virtual bool CanFire()
    {
        if (config == null || config.fireRate <= 0f) return true;

        float timeBetweenShots = 1f / config.fireRate;
        return Time.time - lastShotTime >= timeBetweenShots;
    }

    public void TryFire()
    {
        if (!CanFire()) return;

        lastShotTime = Time.time;
        Fire();
    }

    protected abstract void Fire();
}
