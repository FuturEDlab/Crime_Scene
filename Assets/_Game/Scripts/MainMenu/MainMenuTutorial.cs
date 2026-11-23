// Tutorial panel of the main menu: the Figma "Tutorial" pages (Start, 1-13),
// one visible at a time. Back/Next step through them; Finish and
// Return to Main Menu go back to the main menu.

using UnityEngine;

public class MainMenuTutorial : MonoBehaviour
{
    [SerializeField] private MainMenuController menu;
    [SerializeField] private GameObject[] pages;

    private int current;

    public void ShowFirstPage() => ShowPage(0);

    public void NextPage()
    {
        if (current < pages.Length - 1)
            ShowPage(current + 1);
        else
            Finish();
    }

    public void PreviousPage()
    {
        if (current > 0)
            ShowPage(current - 1);
    }

    public void Finish()
    {
        if (menu != null)
            menu.ShowMainMenu();
    }

    private void ShowPage(int index)
    {
        current = Mathf.Clamp(index, 0, pages.Length - 1);
        for (int i = 0; i < pages.Length; i++)
            if (pages[i] != null)
                pages[i].SetActive(i == current);
    }
}
