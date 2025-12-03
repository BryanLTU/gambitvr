using UnityEngine;

public class testSpawn : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponent<PlayerClass>();
        pc.AssignClass(ClassType.Pawn);
        
    }
}
