using UnityEngine;

// Records a circular buffer of positions + cumulative distances so followers can request a point X units behind the player.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerPathRecorder : MonoBehaviour
{
    public int bufferSize = 2048;               // must be big enough for long trails
    public float minRecordDistance = 0.02f;     // only record when player moved at least this far (reduce redundant points)
    public bool startRecording = true;

    Vector2[] posBuffer;
    float[] distBuffer; // cumulative distance at each buffer slot (distance from that slot forward to newest pos)
    int writeIndex = 0;
    Vector2 lastRecordedPos;
    int filled = 0;

    void Awake()
    {
        if (bufferSize < 16) bufferSize = 16;
        posBuffer = new Vector2[bufferSize];
        distBuffer = new float[bufferSize];

        lastRecordedPos = transform.position;
        // initialize buffers with current pos
        for (int i = 0; i < bufferSize; i++)
        {
            posBuffer[i] = lastRecordedPos;
            distBuffer[i] = 0f;
        }
        writeIndex = 0;
        filled = 1;
    }

    void FixedUpdate()
    {
        if (!startRecording) return;

        Vector2 p = transform.position;
        float moved = Vector2.Distance(p, lastRecordedPos);
        if (moved >= minRecordDistance)
        {
            // write new sample
            posBuffer[writeIndex] = p;

            // compute cumulative distance from this entry to the newest (we'll store distance-to-newest)
            int prevIndex = (writeIndex - 1) < 0 ? bufferSize - 1 : writeIndex - 1;
            // distance at prevIndex is distance-to-newest from that older slot; but easier: compute distance between prev sample and new sample
            // Build distBuffer so distBuffer[writeIndex] == 0 (newest), and older slots hold distance along path to newest.
            // We'll shift older distances by adding moved.
            for (int i = 0; i < filled; i++)
            {
                int idx = (writeIndex - i - 1);
                if (idx < 0) idx += bufferSize;
                distBuffer[idx] += moved;
            }
            distBuffer[writeIndex] = 0f;

            lastRecordedPos = p;
            writeIndex = (writeIndex + 1) % bufferSize;
            if (filled < bufferSize) filled++;
        }
    }

    // Returns true and out position if there exists a recorded point at 'metersBack' (distance behind newest).
    // If metersBack is greater than total recorded length, returns the oldest recorded position.
    public bool TryGetPositionAtDistanceBack(float metersBack, out Vector2 result)
    {
        result = posBuffer[(writeIndex - 1 + bufferSize) % bufferSize]; // default newest

        if (filled == 0) return false;

        // If metersBack <= 0 => newest position
        if (metersBack <= 0f)
        {
            result = posBuffer[(writeIndex - 1 + bufferSize) % bufferSize];
            return true;
        }

        // find two samples where distBuffer[a] <= metersBack <= distBuffer[b]
        // note: distBuffer of newest (writeIndex-1) == 0, older entries have increasing distances.
        // We'll walk back from newest until we exceed metersBack.
        int newestIdx = (writeIndex - 1 + bufferSize) % bufferSize;

        // If buffer only has one sample, return it
        if (filled == 1)
        {
            result = posBuffer[newestIdx];
            return true;
        }

        int idxA = newestIdx;
        int idxB = newestIdx;

        // Walk backwards (toward older) until distBuffer[idxB] >= metersBack or we've exhausted buffer
        int steps = 0;
        while (steps < filled)
        {
            idxB = (newestIdx - steps + bufferSize) % bufferSize;
            float dB = distBuffer[idxB];
            if (dB >= metersBack)
            {
                // idxB is older sample with distance >= metersBack
                break;
            }
            steps++;
        }

        if (steps >= filled)
        {
            // metersBack exceeds recorded length -> return oldest
            int oldestIdx = (newestIdx - (filled - 1) + bufferSize) % bufferSize;
            result = posBuffer[oldestIdx];
            return true;
        }

        // idxB is older (distance >= metersBack), idxA is next newer sample (steps-1)
        idxA = (idxB + 1) % bufferSize; // newer sample, smaller dist
        float dA = distBuffer[idxA];
        float dBval = distBuffer[idxB];

        // if dBval == dA (rare double-sample), just return one
        if (Mathf.Approximately(dBval, dA))
        {
            result = posBuffer[idxB];
            return true;
        }

        // Interpolate between posA (closer to newest) and posB (older) to reach exact metersBack
        float t = (metersBack - dA) / (dBval - dA);
        Vector2 posA = posBuffer[idxA];
        Vector2 posB = posBuffer[idxB];
        result = Vector2.Lerp(posA, posB, Mathf.Clamp01(t));
        return true;
    }

    // helpful diagnostics
    public float GetTotalRecordedDistance()
    {
        if (filled <= 1) return 0f;
        int newest = (writeIndex - 1 + bufferSize) % bufferSize;
        int oldest = (newest - (filled - 1) + bufferSize) % bufferSize;
        return distBuffer[oldest];
    }
}
