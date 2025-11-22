using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Door Reference")]
    public DoorController connectedDoor;

    [Header("Detection Tags")]
    public string playerTag = "Player";
    public string npcTag = "NPC"; // pastikan NPCFollower diberi tag ini

    private int objectsOnPlate = 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) || other.CompareTag(npcTag))
        {
            objectsOnPlate++;
            if (connectedDoor != null)
                connectedDoor.SetOpen(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) || other.CompareTag(npcTag))
        {
            objectsOnPlate = Mathf.Max(0, objectsOnPlate - 1);
            if (objectsOnPlate == 0 && connectedDoor != null)
                connectedDoor.SetOpen(false);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
