using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public DoorController connectedDoor;

    public string playerTag = "Player";
    public string npcTag = "NPC";

    private int objectsOnPlate = 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (connectedDoor != null && connectedDoor.isPermanentOpen)
            return;

        if (other.CompareTag(playerTag) || other.CompareTag(npcTag))
        {
            objectsOnPlate++;
            connectedDoor?.SetOpen(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (connectedDoor != null && connectedDoor.isPermanentOpen)
            return;

        if (other.CompareTag(playerTag) || other.CompareTag(npcTag))
        {
            objectsOnPlate = Mathf.Max(0, objectsOnPlate - 1);

            if (objectsOnPlate == 0)
                connectedDoor?.SetOpen(false);
        }
    }
}
