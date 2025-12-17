using UnityEngine;

public enum ClassType { None, Pawn, King, Queen, Rook, Bishop, Knight };

public class PlayerClass : MonoBehaviour
{
    public ClassType playerClass;
    public GameObject[] Weapon;
    public int health;
    [SerializeField] private Camera playerCam;
    public void AssignClass(ClassType assignedClass)
    {
        playerClass = assignedClass;
        ApplyLoadout();
    }

    private void ApplyLoadout()
    {

        switch (playerClass)
        {
            case ClassType.None: default: break;

            case ClassType.Pawn:
                health = 150;
                break;

            case ClassType.King:
                health = 500;
                break;

            case ClassType.Queen:
                health = 350;
                break;

            case ClassType.Rook:
                health = 300;
                break;

            case ClassType.Bishop:
                health = 250;
                break;

            case ClassType.Knight:
                health = 250;
                break;
        }
    }

}