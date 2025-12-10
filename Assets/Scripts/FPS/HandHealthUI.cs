using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HandHealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthFill;

    private void Awake()
    {
        if (healthFill != null)
            healthFill.value = 1f;
    }

    public void SetHealth01(float value01)
    {
        if (healthFill == null) return;
        healthFill.value = Mathf.Clamp01(value01);
    }
}
