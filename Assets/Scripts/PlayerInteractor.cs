using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRadius = 1.4f;
    public KeyCode interactKey = KeyCode.E;
    public bool toggleRecorderOnFollow = true;

    PlayerPathRecorder recorder;

    void Start()
    {
        recorder = GetComponent<PlayerPathRecorder>();
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            HandleInteract();
        }
    }

    void HandleInteract()
    {
        var info = GetNearestNPCInfo();
        bool npcNearby = info.canInteract;
        bool isFollowing = NPCFollower.IsGlobalFollowing();

        // Case 1: FOLLOW — only allowed when NPC is in range
        if (!isFollowing && npcNearby)
        {
            NPCFollower.ToggleGlobalFollow(true);
            if (toggleRecorderOnFollow && recorder != null)
                recorder.startRecording = true;
            return;
        }

        // Case 2: STAY — allowed anytime
        if (isFollowing)
        {
            NPCFollower.ToggleGlobalFollow(false);
            if (recorder != null)
                recorder.startRecording = false;
        }
    }

    GameObject GetNearestNPC()
    {
        GameObject nearest = null;
        float best = float.MaxValue;

        foreach (var npc in NPCFollower.All)
        {
            float d = Vector2.Distance(transform.position, npc.transform.position);
            if (d < best)
            {
                best = d;
                nearest = npc.gameObject;
            }
        }

        return best <= interactRadius ? nearest : null;
    }

    public (GameObject npc, bool canInteract) GetNearestNPCInfo()
    {
        var npc = GetNearestNPC();
        return (npc, npc != null);
    }
}
