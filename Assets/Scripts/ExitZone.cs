using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    public string playerTag = "Player";
    public float companionRadius = 1.6f;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!other.CompareTag(playerTag)) return;

        float sqrR = companionRadius * companionRadius;
        bool hasFollower = false;

        foreach (var fol in NPCFollower.All)
        {
            if (fol == null || !fol.isFollowing) continue;

            float d2 = (fol.transform.position - other.transform.position).sqrMagnitude;
            if (d2 <= sqrR)
            {
                hasFollower = true;
                break;
            }
        }

        if (hasFollower)
        {
            GameGlobals.SetFlag("day1_delivered_folks", true);
            Debug.Log("Exit success! Flag set.");
        }
        else
        {
            Debug.Log("Player reached exit but NO follower close enough.");
        }
    }
}
