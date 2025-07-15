using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class ConsoleToUI : MonoBehaviour
{
    public TextMeshProUGUI consoleText; // Assign your TextMeshProUGUI component here in inspector
    public ScrollRect scrollRect;
    private StringBuilder stringBuilder = new StringBuilder();

    private void Awake()
    {
        //Redirect log output to this UI console
        Application.logMessageReceived += HandleLog;
    }

    private void OnDestroy()
    {
        //Stop redirecting log output when the object is destroyed
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Add the log message to your UI text
        stringBuilder.Append(logString).Append("\n");
        consoleText.text = stringBuilder.ToString();

        // Auto-scroll to bottom only if scrollbar is already at the bottom
        if (scrollRect.normalizedPosition.y <= 0.01f)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }
}
