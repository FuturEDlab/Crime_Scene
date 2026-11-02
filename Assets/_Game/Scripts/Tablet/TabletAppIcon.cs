using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A tablet button with the four states used across the Figma board: Default,
// Hover, Selected and Hover + Selected (home app icons, camera controls...).
//
//   Hover    = the pointer (mouse or VR ray) is over the button.
//   Selected = the button is being pressed.
//
// So a normal tap reads Hover -> Hover + Selected -> (action). Dragging off
// while still pressed shows plain Selected.
//
// Any mix of these is applied per state, all set up by the tablet builders:
//   - Tile: recolour an Image (and optionally swap its sprite, for icons that
//     are one flattened image per state, like Audio Recordings).
//   - Border: an outline Image shown in the states whose border colour is not
//     fully transparent.
//   - State objects: one child per state, only the current one active (for
//     artwork whose size changes between states, like the camera buttons).
//
// The Button's own transition is set to None; this component owns the look.
[RequireComponent(typeof(Button))]
public class TabletAppIcon : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private enum State { Default, Hover, Selected, HoverSelected }

    [SerializeField] private Image tile;

    [Tooltip("Optional outline, shown in states whose border colour has alpha > 0.")]
    [SerializeField] private Image border;

    [Header("Tile colours")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color hoverSelectedColor = Color.white;

    [Header("Border colours (transparent = hidden)")]
    [SerializeField] private Color borderDefault = Color.clear;
    [SerializeField] private Color borderHover = Color.white;
    [SerializeField] private Color borderSelected = Color.clear;
    [SerializeField] private Color borderHoverSelected = new Color(1f, 1f, 1f, 0.5f);

    [Header("Whole-icon sprites (flattened icons only)")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite hoverSelectedSprite;

    [Header("Per-state objects (optional)")]
    [SerializeField] private GameObject defaultObject;
    [SerializeField] private GameObject hoverObject;
    [SerializeField] private GameObject selectedObject;
    [SerializeField] private GameObject hoverSelectedObject;

    private bool hovered;
    private bool pressed;

    // Pointer exit/up never arrive while the screen is hidden, so start clean
    // every time the screen is shown again.
    void OnEnable()
    {
        hovered = false;
        pressed = false;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; Refresh(); }
    public void OnPointerDown(PointerEventData eventData) { pressed = true; Refresh(); }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; Refresh(); }

    private void Refresh()
    {
        State state = hovered && pressed ? State.HoverSelected
                    : hovered ? State.Hover
                    : pressed ? State.Selected
                    : State.Default;

        if (tile != null)
        {
            tile.color = Pick(state, defaultColor, hoverColor, selectedColor, hoverSelectedColor);
            Sprite sprite = Pick(state, defaultSprite, hoverSprite, selectedSprite, hoverSelectedSprite);
            if (sprite != null)
                tile.sprite = sprite;
        }

        if (border != null)
        {
            Color borderColor = Pick(state, borderDefault, borderHover, borderSelected, borderHoverSelected);
            border.enabled = borderColor.a > 0f;
            border.color = borderColor;
        }

        SetStateObject(defaultObject, state == State.Default);
        SetStateObject(hoverObject, state == State.Hover);
        SetStateObject(selectedObject, state == State.Selected);
        SetStateObject(hoverSelectedObject, state == State.HoverSelected);
    }

    private static T Pick<T>(State state, T normal, T hover, T selected, T hoverSelected)
    {
        switch (state)
        {
            case State.Hover: return hover;
            case State.Selected: return selected;
            case State.HoverSelected: return hoverSelected;
            default: return normal;
        }
    }

    private static void SetStateObject(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}
