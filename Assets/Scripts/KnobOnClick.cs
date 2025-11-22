using UnityEngine;
using UnityEngine.SceneManagement;

public class KnobOnClick : MonoBehaviour
{
    public KnobMenu knob;
    public MainMenuManager menu;   // ← tambah referensi ke MainMenuManager

    public void ExecuteMenu()
    {
        int index = knob.GetMenuIndex();

        switch (index)
        {
            case 0: // PLAY
                menu.PlayGame();
                break;

            case 1: // SETTINGS PANEL
                menu.OpenOptionsPanel();
                break;

            case 2: // CREDIT
                menu.OpenCreditScene();
                break;

            case 3: // EXIT
                menu.QuitGame();
                break;
        }
    }
}
