using UnityEngine;

public class UIManager : MonoBehaviour
{
    private GameObject currentScreen;
    public GameObject mainScreen;
    public GameObject soloScreen;
    public GameObject settingsScreen;

    void Start()
    {
        ClearScreen();
        ShowMain();
    }
    private void ClearScreen()
    {
        mainScreen.SetActive(false);
        soloScreen.SetActive(false);
        settingsScreen.SetActive(false);
    }

    public void ShowMain()
    {
        currentScreen?.SetActive(false);
        mainScreen.SetActive(true);
        currentScreen = mainScreen;
    }

    public void ShowSolo()
    {
        currentScreen?.SetActive(false);
        soloScreen.SetActive(true);
        currentScreen = soloScreen;
    }

    public void ShowSettings()
    {
        currentScreen?.SetActive(false);
        settingsScreen.SetActive(true);
        currentScreen = settingsScreen;
    }
}
