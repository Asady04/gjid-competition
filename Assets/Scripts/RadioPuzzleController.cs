using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RadioPuzzleController : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform needle;
    public RectTransform knob;
    public Image targetMarker;
    public TMP_Text infoText;

    [Header("Rotation Settings")]
    public float maxRotation = 260f;
    public float scrollSpeed = 100f;

    [Header("Target Frequency")]
    public float targetAngle = 35f;
    public float tolerance = 5f;

    [Header("Gizmos Settings")]
    public float gizmoRadius = 150f;

    [Header("Visual Colors")]
    public Color defaultColor = Color.red;
    public Color targetColor = Color.green;

    private float currentAngle = 0f;
    private bool isScrolling = false;
    private bool solved = false;

    void OnEnable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        solved = false;
        currentAngle = 0f;
        UpdateNeedle();

        if (targetMarker != null)
            targetMarker.color = defaultColor;

        if (infoText != null)
            infoText.text = "Scroll untuk mencari sinyal...";
    }

    void Update()
    {
        if (solved) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            isScrolling = true;
            currentAngle = Mathf.Clamp(
                currentAngle + scroll * scrollSpeed,
                -maxRotation / 2f,
                maxRotation / 2f
            );

            UpdateNeedle();
        }
        else
        {
            if (isScrolling)
            {
                isScrolling = false;
                CheckIfSolved();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePuzzle();
        }
    }

    void UpdateNeedle()
    {
        if (needle != null)
            needle.localRotation = Quaternion.Euler(0f, 0f, -currentAngle);

        // Knob ikut berputar mengikuti needle
        if (knob != null)
            knob.localRotation = Quaternion.Euler(0f, 0f, -currentAngle);
    }

    void CheckIfSolved()
    {
        float diff = Mathf.Abs(currentAngle - targetAngle);

        if (diff <= tolerance)
        {
            solved = true;

            if (infoText != null)
                infoText.text = "Sinyal ditemukan";

            if (targetMarker != null)
                targetMarker.color = targetColor;

            var door = FindObjectOfType<DoorController>();
            if (door != null)
                door.SetOpen(true);

            Invoke(nameof(ClosePuzzle), 1.5f);
        }
        else
        {
            if (infoText != null)
                infoText.text = "Tidak tepat, coba lagi";
        }
    }

    void ClosePuzzle()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        Cursor.visible = false;
    }

    // Gizmos panduan target tidak bergerak mengikuti needle
    void OnDrawGizmos()
    {
        if (needle == null) return;

        Vector3 pivot = needle.transform.position;
        float rad = targetAngle * Mathf.Deg2Rad;

        Vector3 targetPos = pivot +
            new Vector3(
                Mathf.Sin(rad),
                Mathf.Cos(rad),
                0f
            ) * gizmoRadius;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(pivot, gizmoRadius);
        Gizmos.DrawLine(pivot, targetPos);
        Gizmos.DrawSphere(targetPos, 6f);
    }
}
