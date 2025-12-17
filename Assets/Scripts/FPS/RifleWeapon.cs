using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using XRMultiplayer;

public class RifleWeapon : WeaponBase
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private AudioSource shotSource;
    [SerializeField] XRInputValueReader<float> m_TriggerInput = new("Trigger");

    private XRGrabInteractable grab;
    private bool isHeld;

    private string _leftTriggerBindingPath;
    private string _rightTriggerBindingPath;

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

        CacheTriggerBindingPaths();

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

    private void CacheTriggerBindingPaths()
    {
        _leftTriggerBindingPath = null;
        _rightTriggerBindingPath = null;

        var action = m_TriggerInput.inputAction;
        foreach (var b in action.bindings)
        {
            var path = string.IsNullOrEmpty(b.effectivePath) ? b.path : b.effectivePath;
            if (string.IsNullOrEmpty(path)) continue;

            if (_leftTriggerBindingPath == null && path.Contains("{LeftHand}"))
                _leftTriggerBindingPath = path;
            if (_rightTriggerBindingPath == null && path.Contains("{RightHand}"))
                _rightTriggerBindingPath = path;
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        isHeld = true;

        var inputInteractor = args.interactorObject;
        if (inputInteractor == null) return;

        bool left = inputInteractor.handedness == InteractorHandedness.Left;

        var chosenPath = left ? _leftTriggerBindingPath : _rightTriggerBindingPath;
        if (string.IsNullOrEmpty(chosenPath))
        {
            Debug.LogWarning("[RifleWeapon] Could not find trigger binding path for that hand");
            return;
        }

        m_TriggerInput.inputAction.bindingMask = new InputBinding { path = chosenPath };
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isHeld = false;
        m_TriggerInput.inputAction.bindingMask = null;
    }

    void Update()
    {
        if (!isHeld || muzzle == null) return;

        if (m_TriggerInput.ReadValue() >= 0.5f)
        {
            TryFire();
        }
    }

    protected override void Fire()
    {
        if (config != null && config.shotClip != null && shotSource != null)
        {
            shotSource.PlayOneShot(config.shotClip, config.shotVolume);
        }
        Vector3 origin = muzzle.position;
        Vector3 dir = muzzle.forward;

        Utils.Log("[RifleWeapon] Fire");
        FPSArenaManager.Instance.RequestHitscanShot(origin, dir, config.maxRange, config.damage, config.weaponId);
    }
}
