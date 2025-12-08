using UnityEngine;

public enum WeaponId
{
    None = 0,
    Rifle = 0,
    Bow = 2
}

[CreateAssetMenu(menuName = "FPS/Weapon Config")]
public class WeaponConfig : ScriptableObject
{
    public WeaponId weaponId;

    public GameObject weaponPrefab;

    [Header("Stats")]
    public float damage = 10f;
    [Tooltip("Shots per second")]
    public float fireRate = 10f;
    public float maxRange = 100f;
}
