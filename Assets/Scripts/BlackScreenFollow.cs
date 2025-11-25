using UnityEngine;

public class BlackScreenFollow : MonoBehaviour
{
    [SerializeField]
    private Camera _camera;

    private void LateUpdate()
    {
        if (_camera == null) return;

        Vector3 targetPos = _camera.transform.position + _camera.transform.forward * 0.2f;
        Quaternion targetRot = _camera.transform.rotation;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5);
    }
}
