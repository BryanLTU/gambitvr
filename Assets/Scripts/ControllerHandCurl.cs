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

    [Header("Pose Blending")]
    [Tooltip("Seconds to blend between poses")]
    public float poseLerpTime = 0.06f;
    [Range(0,1)] public float pinch_Index  = 1f;
    [Range(0,1)] public float pinch_Thumb  = 1f;
    [Range(0,1)] public float pinch_Middle = 0.1f;
    [Range(0,1)] public float pinch_Ring   = 0.1f;
    [Range(0, 1)] public float pinch_Little = 0.1f;
    

    bool _pinchActive;
    float _tIndex, _tThumb, _tMiddle, _tRing, _tLittle;
    float _lerpVel;

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
        float g = Mathf.Clamp01(m_GripInput?.ReadValue()    ?? 0f);
        float t = Mathf.Clamp01(m_TriggerInput?.ReadValue() ?? 0f);

        float tgtIndex, tgtThumb, tgtMiddle, tgtRing, tgtLittle;

        if (_pinchActive)
        {
            tgtIndex  = pinch_Index;
            tgtThumb  = pinch_Thumb;
            tgtMiddle = pinch_Middle;
            tgtRing   = pinch_Ring;
            tgtLittle = pinch_Little;
        }
        else
        {
            tgtIndex  = t;
            tgtThumb  = Mathf.Clamp01(g * 0.6f + t * 0.4f);
            tgtMiddle = g;
            tgtRing   = g;
            tgtLittle = g;
        }

        float k = poseLerpTime > 0f ? Time.deltaTime / poseLerpTime : 1f;
        _tIndex  = Mathf.Lerp(_tIndex,  tgtIndex,  k);
        _tThumb  = Mathf.Lerp(_tThumb,  tgtThumb,  k);
        _tMiddle = Mathf.Lerp(_tMiddle, tgtMiddle, k);
        _tRing   = Mathf.Lerp(_tRing,   tgtRing,   k);
        _tLittle = Mathf.Lerp(_tLittle, tgtLittle, k);

        ApplyCurl(index,  _tIndex);
        ApplyCurl(thumb,  _tThumb);
        ApplyCurl(middle, _tMiddle);
        ApplyCurl(ring,   _tRing);
        ApplyCurl(little, _tLittle);
    }

    void CacheBindPose(Finger f)
    {
        if (f.proximal)    f.p0 = f.proximal.localRotation;
        if (f.intermediate) f.i0 = f.intermediate.localRotation;
        if (f.distal)       f.d0 = f.distal.localRotation;
    }

    void ApplyCurl(Finger f, float amount01)
    {
        float sign = f.invert ? -1f : 1f;
        float aP = amount01 * f.maxCurl * sign;
        float aI = amount01 * f.maxCurl * 0.8f;
        float aD = amount01 * f.maxCurl * 0.6f;

        if (f.proximal)     f.proximal.localRotation    = f.p0 * Quaternion.AngleAxis(aP, f.localAxis);
        if (f.intermediate) f.intermediate.localRotation = f.i0 * Quaternion.AngleAxis(aI, f.localAxis);
        if (f.distal)       f.distal.localRotation       = f.d0 * Quaternion.AngleAxis(aD, f.localAxis);
    }

    public void BeginPinch() => _pinchActive = true;
    public void EndPinch()   => _pinchActive = false;
}
