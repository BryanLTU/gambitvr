using UnityEngine;

public class SetClass : MonoBehaviour
{
    public ClassType newClass;

    private void OnTriggerEnter(Collider other)
    {
        PlayerClass pc = other.GetComponentInParent<PlayerClass>();
        pc.AssignClass(newClass);
    }
}
