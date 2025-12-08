using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RifleWeapon : WeaponBase
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;
    [SerializeField] XRInputValueReader<float> m_TriggerInput = new XRInputValueReader<float>("Trigger");

    private XRGrabInteractable grab;
    private bool isHeld;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        if (grab != null)
        {
            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);
        }

        m_TriggerInput.inputAction.Enable();
    }

    void OnDisable()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
        }

        m_TriggerInput.inputAction.Disable();
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isHeld = false;
    }

    void Update()
    {
        if (!isHeld || config == null || muzzle == null) return;

        if (m_TriggerInput.inputAction.WasPerformedThisFrame())
        {
            TryFire();
        }
    }

    protected override void Fire()
    {
        Vector3 origin = muzzle.position;
        Vector3 dir = muzzle.forward;

        FPSArenaManager.Instance.RequestHitscanShot(origin, dir, config.maxRange, config.damage);
    }
}
