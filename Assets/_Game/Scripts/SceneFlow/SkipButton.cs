using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.InputSystem;

public class SkipButton : MonoBehaviour
{
    [Header("Where to go when skipping")]
    [SerializeField] private string menuSceneName = "MenuScene";

    [Header("Optional UI element")]
    [SerializeField] private GameObject skipUI; 
    [SerializeField] private float showAfterSeconds = 0f;

    [Header("Optional Cutscene / Timeline")]
    [SerializeField] private PlayableDirector cutscene;
    [SerializeField] private bool evaluateToEnd = true;

    [Header("Hotkey (desktop testing)")]
    [SerializeField] private Key skipKey = Key.Escape;

    private bool skipEnabled = false;

    void Start()
    {
        // hide UI initially
        if (skipUI != null)
        {
            skipUI.SetActive(false);

            if (showAfterSeconds <= 0f)
            {
                skipUI.SetActive(true);
                skipEnabled = true;
            }
            else
            {
                Invoke(nameof(EnableSkipUI), showAfterSeconds);
            }
        }
        else
        {
            skipEnabled = true;
        }
    }

    void EnableSkipUI()
    {
        if (skipUI != null)
            skipUI.SetActive(true);

        skipEnabled = true;
    }

    void Update()
    {
        if (!skipEnabled) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && skipKey != Key.None && keyboard[skipKey].wasPressedThisFrame)
            Skip();
    }

    // Call this from your button OnClick
    public void Skip()
    {
        if (!skipEnabled) return;

        // Optional: finish timeline before loading
        if (cutscene != null)
        {
            if (evaluateToEnd)
            {
                cutscene.time = cutscene.duration;
                cutscene.Evaluate();
            }
            cutscene.Stop();
        }

        // make sure scene exists
        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError($"SkipButton: Scene \"{menuSceneName}\" is not in Build Settings or name is wrong.");
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}
