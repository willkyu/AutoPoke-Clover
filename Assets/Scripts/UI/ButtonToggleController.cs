using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ButtonToggleController : MonoBehaviour
{
    private Button myButton;
    private Text buttonText;

    public string startText = "Start";
    public string stopText = "Stop";

    public UnityEvent onStart;
    public UnityEvent onStop;

    private bool isRunning = false;

    void Start()
    {
        myButton = GetComponent<Button>();
        buttonText = myButton.GetComponentInChildren<Text>();

        myButton.onClick.AddListener(OnButtonClick);
        buttonText.text = startText; // 初始文本
    }

    void OnButtonClick()
    {
        if (isRunning)
        {
            onStop.Invoke();
            buttonText.text = startText;
        }
        else
        {
            onStart.Invoke();
            buttonText.text = stopText;
        }

        isRunning = !isRunning;
    }
}
