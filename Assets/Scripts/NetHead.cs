using Unity.Netcode;
using UnityEngine;

public class NetHead : NetworkBehaviour
{
    Transform head;

    void Start()
    {
        if (IsOwner)
            head = Camera.main ? Camera.main.transform : null;
    }

    void LateUpdate()
    {
        if (!IsOwner || head == null) return;
        transform.SetPositionAndRotation(head.position, head.rotation);
    }
}
