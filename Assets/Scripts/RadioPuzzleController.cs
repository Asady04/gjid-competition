using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RadioPuzzleController : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform needle;           // Jarum radio
    public Image targetMarker;             // Penanda target (optional)
    public TMP_Text infoText;

    [Header("Rotation Settings")]
    [Tooltip("Batas rotasi total (derajat)")]
    public float maxRotation = 160f;
    [Tooltip("Kecepatan rotasi jarum terhadap scroll")]
    public float scrollSpeed = 200f;

    [Header("Target Frequency")]
    [Tooltip("Sudut target sinyal (0 = tengah)")]
    public float targetAngle = 27f;
    [Tooltip("Toleransi kecil agar tidak mustahil diselesaikan")]
    public float tolerance = 1.5f;

    [Header("Visual")]
    public Color defaultColor = Color.white;
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
            infoText.text = "Scroll untuk mencari sinyal yang tepat...";
    }

    void Update()
    {
        if (solved) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // Apakah pemain sedang menggulir?
        if (Mathf.Abs(scroll) > 0.001f)
        {
            isScrolling = true;
            currentAngle = Mathf.Clamp(currentAngle + scroll * scrollSpeed, -maxRotation / 2f, maxRotation / 2f);
            UpdateNeedle();

            if (infoText != null)
                infoText.text = "Sedang mencari sinyal...";
        }
        else
        {
            // Pemain berhenti menggulir
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
    }

    void CheckIfSolved()
    {
        float diff = Mathf.Abs(currentAngle - targetAngle);

        if (diff <= tolerance)
        {
            solved = true;
            if (infoText != null)
                infoText.text = "📻 Sinyal ditemukan!";
            if (targetMarker != null)
                targetMarker.color = targetColor;

            Invoke(nameof(ClosePuzzle), 1.5f);

            var door = FindObjectOfType<DoorController>();
            if (door != null)
                door.SetOpen(true);
        }
        else
        {
            if (infoText != null)
                infoText.text = "Tidak tepat, coba lagi...";
        }
    }

    void ClosePuzzle()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        Cursor.visible = false;
    }
}
