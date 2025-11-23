using UnityEngine;
using TMPro;

public class DoorRemote : MonoBehaviour
{
    public DoorController targetDoor;
    public TMP_Text interactPrompt;
    public float interactRadius = 1.5f;
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";

    private Transform player;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;

        if (interactPrompt != null)
            interactPrompt.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= interactRadius && targetDoor != null)
        {
            if (!targetDoor.isPermanentOpen)
            {
                if (interactPrompt != null)
                {
                    interactPrompt.text = "Tekan E";
                    interactPrompt.gameObject.SetActive(true);
                }

                if (Input.GetKeyDown(interactKey))
                {
                    targetDoor.SetPermanentOpen();
                    DisableAllPlatesLinkedToDoor();

                    if (interactPrompt != null)
                        interactPrompt.gameObject.SetActive(false);

                    enabled = false;
                }
            }
        }
        else
        {
            if (interactPrompt != null)
                interactPrompt.gameObject.SetActive(false);
        }
    }

    void DisableAllPlatesLinkedToDoor()
    {
        PressurePlate[] plates = FindObjectsOfType<PressurePlate>();
        foreach (var plate in plates)
        {
            if (plate.connectedDoor == targetDoor)
                plate.enabled = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
