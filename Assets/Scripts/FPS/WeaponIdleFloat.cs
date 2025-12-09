using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponIdleFloat : MonoBehaviour
{
    [SerializeField] private float floatAmplitude = 0.05f;
    [SerializeField] private float floatFrequency = 1.5f;
    [SerializeField] private float rotateSpeed = 35f;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    
    private Vector3 basePosition;
    private float phaseOffset;
    private bool floatingActive = true;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnEnable()
    {
        basePosition = transform.position;
        
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        floatingActive = false;
        basePosition = transform.position;
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        enabled = false;
    }

    void Update()
    {
        if (!floatingActive) return;
        if (grab != null && grab.isSelected) return;

        float yOffset = Mathf.Sin(Time.time * floatFrequency + phaseOffset) * floatAmplitude;
        transform.position = basePosition + new Vector3(0f, yOffset, 0f);

        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }
}
