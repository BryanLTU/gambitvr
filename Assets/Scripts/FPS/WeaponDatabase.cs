using System.Collections.Generic;
using UnityEngine;

public class WeaponDatabase : MonoBehaviour
{
    public static WeaponDatabase Instance { get; private set; }

    [SerializeField] private WeaponConfig[] weaponConfigs;

    private Dictionary<WeaponId, WeaponConfig> _byId;

    void Awake()
    {
        Instance = this;
        _byId = new Dictionary<WeaponId, WeaponConfig>();

        foreach (var cfg in weaponConfigs)
        {
            if (cfg != null && !_byId.ContainsKey(cfg.weaponId))
            {
                _byId.Add(cfg.weaponId, cfg);
            }
        }
    }

    public WeaponConfig Get(WeaponId id)
    {
        _byId.TryGetValue(id, out var cfg);
        return cfg;
    }
}
