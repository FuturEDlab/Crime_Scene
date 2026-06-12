using UnityEngine;
using TMPro;
using System;

public class iPadProximityUI : MonoBehaviour
{
    public Transform player;
    public float activationDistance = 3f;

    public GameObject appsPanel;
    public GameObject clockPanel;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI dateText;

    private bool isNear;

    void Update()
    {
        {
            timeText.text = DateTime.Now.ToString("hh:mm tt");
            if (dateText != null)
                dateText.text = DateTime.Now.ToString("ddd, MMM dd");
        }
        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= activationDistance && !isNear)
        {
            isNear = true;
            ShowApps();
        }
        else if (distance > activationDistance && isNear)
        {
            isNear = false;
            ShowClock();
        }

        if (!isNear)
        {
            UpdateTime();
        }
    }

    void ShowApps()
    {
        appsPanel.SetActive(true);
        clockPanel.SetActive(false);
    }

    void ShowClock()
    {
        appsPanel.SetActive(false);
        clockPanel.SetActive(true);
    }

    void UpdateTime()
    {
        timeText.text = DateTime.Now.ToString("hh:mm tt");
        dateText.text = DateTime.Now.ToString("ddd, MMM dd");
    }
}