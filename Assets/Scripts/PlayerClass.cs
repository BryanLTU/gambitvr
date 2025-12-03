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
                SpawnWeapon(Weapon[0]);
                break;

            case ClassType.King:
                health = 500;
                SpawnWeapon(Weapon[1]);
                break;

            case ClassType.Queen:
                health = 350;
                SpawnWeapon(Weapon[2]);
                break;

            case ClassType.Rook:
                health = 300;
                SpawnWeapon(Weapon[3]);
                break;

            case ClassType.Bishop:
                health = 250;
                SpawnWeapon(Weapon[4]);
                break;

            case ClassType.Knight:
                health = 250;
                SpawnWeapon(Weapon[5]);
                break;


        }
    }

    private void SpawnWeapon(GameObject weapon)
    {
        Vector3 pos = playerCam.transform.position + playerCam.transform.forward * 0.5f;
        Instantiate(weapon, pos, playerCam.transform.rotation);
    }
}


