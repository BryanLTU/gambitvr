using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DynamicAttachOnGrab : MonoBehaviour
{
    [Header("Assign your hand bits")]
    [SerializeField] private ControllerHandCurl handCurl;
    public XRDirectInteractor interactor;
    public Transform indexTip;
    public Transform thumbTip;

    [Header("Tuning")]
    public float surfaceOffset = 0.006f;    // how far to sit outside the surface
    public LayerMask grabbableMask;
    public bool ignorePrefabAttachPoses = true;
    public bool updateWhileHeld = true;
    public float followLerp = 1.0f;

    [Header("Colliders to disable")]
    [SerializeField] private List<Collider> _collidersToDisable;

    Transform attach;
    XRGrabInteractable current;
    Collider currentCollider;

    void Awake()
    {
        if (!interactor) interactor = GetComponent<XRDirectInteractor>();
        attach = new GameObject("DynamicAttach").transform;
        attach.SetParent(interactor.transform, false);
        interactor.attachTransform = attach;

        interactor.selectEntered.AddListener(OnSelectEntered);
        interactor.selectExited.AddListener(OnSelectExited);
    }

    void OnDestroy()
    {
        interactor.selectEntered.RemoveListener(OnSelectEntered);
        interactor.selectExited.RemoveListener(OnSelectExited);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        current = args.interactableObject as XRGrabInteractable;
        if (!current) return;

        if (!ignorePrefabAttachPoses && current.attachTransform) return;

        handCurl?.BeginPinch();
        foreach (var c in _collidersToDisable) if (c) c.enabled = false;

        currentCollider = ClosestColliderToMidpoint(current);
        UpdateAttachOnce(current, currentCollider);

        current.attachEaseInTime = 0f;
        current.smoothPosition   = false;
        current.smoothRotation   = false;
        current.movementType = XRBaseInteractable.MovementType.Instantaneous;

        StartCoroutine(RecomputeAttachAfterPose());
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        handCurl?.EndPinch();
        foreach (var c in _collidersToDisable) if (c) c.enabled = true;

        current = null;
        currentCollider = null;

        attach.localPosition = Vector3.zero;
        attach.localRotation = Quaternion.identity;
    }
    
    IEnumerator RecomputeAttachAfterPose()
    {
        float start = Time.time;
        const float maxWait = 0.08f;          // safety
        const float pinchDist = 0.025f;

        while (current &&
            Vector3.Distance(indexTip.position, thumbTip.position) > pinchDist &&
            Time.time - start < maxWait)
            yield return null;

        if (current) {
            currentCollider = ClosestColliderToMidpoint(current);
            UpdateAttachOnce(current, currentCollider);
        }
    }

    void UpdateAttachOnce(XRGrabInteractable grab, Collider col)
    {
        Vector3 mid = (indexTip.position + thumbTip.position) * 0.5f;

        if (!col) col = currentCollider = ClosestColliderToMidpoint(grab);

        Vector3 contact = col ? col.ClosestPoint(mid) : mid;
        Vector3 normal  = EstimateNormal(mid, contact, col);
        attach.position = contact + normal * surfaceOffset;

        Vector3 pinchDir = (indexTip.position - thumbTip.position).normalized;
        Vector3 forward  = Vector3.Cross(interactor.transform.up, pinchDir);
        if (forward.sqrMagnitude < 1e-4f) forward = interactor.transform.forward;
        attach.rotation = Quaternion.LookRotation(forward, interactor.transform.up);
    }

    Collider ClosestColliderToMidpoint(XRGrabInteractable grab)
    {
        if (grab.colliders == null || grab.colliders.Count == 0) return null;
        Vector3 mid = (indexTip.position + thumbTip.position) * 0.5f;
        float best = float.PositiveInfinity; Collider bestCol = null;
        foreach (var c in grab.colliders)
        {
            if (!c || !c.enabled) continue;
            float d = (c.ClosestPoint(mid) - mid).sqrMagnitude;
            if (d < best) { best = d; bestCol = c; }
        }
        return bestCol;
    }

    Vector3 EstimateNormal(Vector3 from, Vector3 to, Collider col)
    {
        Vector3 dir = (to - from).normalized;
        if (Physics.Raycast(from, dir, out var hit, 0.2f, grabbableMask, QueryTriggerInteraction.Ignore)
            && hit.collider == col)
            return hit.normal;

        // Fallback: point away from collider center
        return (to - col.bounds.center).normalized;
    }
}
