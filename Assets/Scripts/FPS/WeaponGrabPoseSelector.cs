using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WeaponGrabPoseSelector : XRGrabInteractable
{
    [SerializeField] private Transform handleLeft;
    [SerializeField] private Transform handleRight;

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        var interactor = args.interactorObject;
        if (interactor != null)
        {
            if (interactor.handedness == UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Right)
            {
                attachTransform = handleRight != null ? handleRight : attachTransform;
            }
            else
            {
                attachTransform = handleLeft != null ? handleLeft : attachTransform;
            }
        }

        base.OnSelectEntering(args);
    }
}
