using UnityEngine;
using TMPro;

public class KnobMenu : MonoBehaviour
{
    [Header("UI")]
    public RectTransform knob;
    public TMP_Text menuText;

    [Header("Knob Settings")]
    public float minAngle = 0f;
    public float maxAngle = 180f;

    private float currentAngle = 0f;
    private int menuIndex = 0;

    private string[] menuNames =
    {
        "Play",
        "Settings",
        "Credit",
        "Exit"
    };

    private void Start()
    {
        menuIndex = 0;
        UpdateMenu();
    }

    private void Update()
{
    float scroll = Input.GetAxis("Mouse ScrollWheel");

    if (scroll != 0)
    {
        if (scroll > 0)
            menuIndex--;
        else
            menuIndex++;

        // === Looping Index ===
        if (menuIndex < 0)
            menuIndex = menuNames.Length - 1;
        else if (menuIndex > menuNames.Length - 1)
            menuIndex = 0;

        // Update rotation
        currentAngle = Mathf.Lerp(minAngle, maxAngle,
            (float)menuIndex / (menuNames.Length - 1));

        knob.rotation = Quaternion.Euler(0, 0, currentAngle);

        UpdateMenu();
    }
}

    void UpdateMenu()
    {
        menuText.text = menuNames[menuIndex];
    }

    public int GetMenuIndex() => menuIndex;
}
