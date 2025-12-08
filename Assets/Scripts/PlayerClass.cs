using UnityEngine;

public enum ClassType { None, Pawn, King, Queen, Rook, Bishop, Knight };

public class PlayerClass : MonoBehaviour
{
    public ClassType playerClass;
    public Transform rightHand;
    public GameObject pawnWeaponPrefab;

    private GameObject currentWeapon;

    public void AssignClass(ClassType assignedClass)
    {
        playerClass = assignedClass;
        ApplyLoadout();
    }

    private void ApplyLoadout()
    {
        if (currentWeapon != null) Destroy(currentWeapon);

        switch (playerClass)
        {
            case ClassType.None: default: break;

            case ClassType.Pawn: break;

            case ClassType.King: break;

            case ClassType.Queen: break;

            case ClassType.Rook: break;

            case ClassType.Bishop: break;

            case ClassType.Knight: break;


        }
    }
}


