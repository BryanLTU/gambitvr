using UnityEngine;

public enum WeaponId
{
    None = 0,
    Rifle = 1,
    SMG= 2,
    Sniper = 3,
    LMG = 4,
    AR = 5
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

    [Header("Tracer")]
    public GameObject tracerPrefab;
    public float tracerDuration = 0.1f;

    [Header("Impact")]
    public GameObject impactPrefab;
    public float impactDuration = 1.5f;

    [Header("Knockback")]
    public float knockbackForce = 0f;
    public bool knockbackPlayers = false;
    public bool knockbackChessPieces = true;

    [Header("Audio")]
    public AudioClip shotClip;
    [Range(0f, 1f)] public float shotVolume = 1f;
}
