using UnityEngine;

public class ClickTrigger : MonoBehaviour
{
    public SkipButton skipButton;

    void OnMouseDown()
    {
        skipButton.Skip();
    }
}
