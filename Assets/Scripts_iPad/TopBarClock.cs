using UnityEngine;
using TMPro;   // needed for TextMeshProUGUI

public class TopBarClock : MonoBehaviour
{
    
    [SerializeField] private TextMeshProUGUI timeText;

    // Start time: 2:00 PM (14:00 in 24h time)
    [SerializeField] private int startHour = 14;   // 14 = 2 PM
    [SerializeField] private int startMinute = 0;

    private float elapsedSeconds = 0f;

    void Start()
    {
        elapsedSeconds = 0f;
        UpdateTimeText();   // show 2:00 PM at the start
    }

    void Update()
    {
        // Add the time that passed since last frame
        elapsedSeconds += Time.deltaTime;
        UpdateTimeText();
    }

    void UpdateTimeText()
    {
        // Convert start time to total seconds
        int totalStartSeconds = (startHour * 60 + startMinute) * 60;

        // Add elapsed seconds since the experience started
        int totalSeconds = totalStartSeconds + Mathf.FloorToInt(elapsedSeconds);

        // Convert back to hours and minutes (24h)
        int hour24 = (totalSeconds / 3600) % 24;
        int minute = (totalSeconds / 60) % 60;

        // Convert to 12h format with AM/PM
        int displayHour = hour24 % 12;
        if (displayHour == 0) displayHour = 12;
        string ampm = hour24 >= 12 ? "PM" : "AM";

        // Example: "2:05 PM"
        timeText.text = $"{displayHour}:{minute:00} {ampm}";
    }
}