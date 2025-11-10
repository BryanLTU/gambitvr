using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class ControllerHandCurl : MonoBehaviour
{
    [Header("Actions")]
    [SerializeField]
    XRInputValueReader<float> m_GripInput = new XRInputValueReader<float>("Grip");
    [SerializeField]
    XRInputValueReader<float> m_TriggerInput = new XRInputValueReader<float>("Trigger");

    [System.Serializable]
    public class Finger
    {
        public Transform proximal, intermediate, distal;
        [HideInInspector] public Quaternion p0, i0, d0;
        public float maxCurl = 70f;     // degrees at proximal
        public Vector3 localAxis = Vector3.right; // axis to bend around (test X/Y/Z)
        public bool invert;             // flip direction if needed
    }

    public Finger index = new Finger { maxCurl = 70f };
    public Finger middle = new Finger { maxCurl = 70f };
    public Finger ring   = new Finger { maxCurl = 70f };
    public Finger little = new Finger { maxCurl = 70f };

    [Header("Thumb")]
    public Finger thumb = new Finger { maxCurl = 50f };

    void Awake()
    {
        CacheBindPose(index);
        CacheBindPose(middle);
        CacheBindPose(ring);
        CacheBindPose(little);
        CacheBindPose(thumb);
    }

    void LateUpdate()
    {
        float g = Mathf.Clamp01(m_GripInput?.ReadValue() ?? 0f);
        float t = Mathf.Clamp01(m_TriggerInput?.ReadValue() ?? 0f);

        // Index reacts mostly to trigger; others to grip
        ApplyCurl(index, Mathf.Clamp01(t));
        ApplyCurl(middle, g);
        ApplyCurl(ring,   g);
        ApplyCurl(little, g);

        // Thumb: mix of both
        ApplyCurl(thumb, Mathf.Clamp01(g * 0.6f + t * 0.4f));
    }

    void CacheBindPose(Finger f)
    {
        if (f.proximal)    f.p0 = f.proximal.localRotation;
        if (f.intermediate) f.i0 = f.intermediate.localRotation;
        if (f.distal)       f.d0 = f.distal.localRotation;
    }

    void ApplyCurl(Finger f, float amount)
    {
        if (f.proximal)
        {
            float sign = f.invert ? -1f : 1f;
            var p = Quaternion.AngleAxis(sign * amount * f.maxCurl, f.localAxis);
            f.proximal.localRotation = f.p0 * p;
        }
        if (f.intermediate)
        {
            var i = Quaternion.AngleAxis(amount * f.maxCurl * 0.8f, f.localAxis);
            f.intermediate.localRotation = f.i0 * i;
        }
        if (f.distal)
        {
            var d = Quaternion.AngleAxis(amount * f.maxCurl * 0.6f, f.localAxis);
            f.distal.localRotation = f.d0 * d;
        }
    }
}
