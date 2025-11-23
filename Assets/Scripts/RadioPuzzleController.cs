using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class RadioPuzzleController : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform needle;
    public RectTransform knob;
    public Image targetMarker;
    public TMP_Text infoText;
    public Button closeButton;

    [Header("Rotation Settings")]
    public float maxRotation = 260f;
    public float scrollSpeed = 100f;

    [Header("Target Frequency")]
    public float targetAngle = 35f;
    public float tolerance = 5f;

    [Header("Mission UI")]
    public TMP_Text missionText;
    public float missionDuration = 10f;

    [Header("Enemy Reaction")]
    public EnemyAI[] assignedEnemies;
    public Transform radioTarget;
    public float enemyStayDuration = 10f;

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

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);

        if (missionText != null)
            missionText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!solved)
        {
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
            else if (isScrolling)
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

            if (closeButton != null)
                closeButton.gameObject.SetActive(true);

            TriggerEnemyToRadio();
            StartMissionTimer();
        }
        else
        {
            if (infoText != null)
                infoText.text = "Tidak tepat, coba lagi";
        }
    }

    void TriggerEnemyToRadio()
    {
        if (assignedEnemies == null || assignedEnemies.Length == 0) return;
        if (radioTarget == null) return;

        foreach (var enemy in assignedEnemies)
        {
            if (enemy == null) continue;

            EnemyCommander commander = enemy.GetComponent<EnemyCommander>();
            if (commander != null)
                commander.InvestigateRadio(radioTarget.position, enemyStayDuration);
        }
    }

    void StartMissionTimer()
    {
        if (missionText == null) return;

        missionText.gameObject.SetActive(true);
        StartCoroutine(MissionTimer());
    }

    private IEnumerator MissionTimer()
    {
        float timer = missionDuration;

        while (timer > 0f)
        {
            missionText.text = "Selamatkan semuanya dalam " + Mathf.Ceil(timer) + " detik!";
            timer -= Time.deltaTime;
            yield return null;
        }

        missionText.gameObject.SetActive(false);
    }

    public void ClosePuzzle()
    {
        solved = false;
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

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
