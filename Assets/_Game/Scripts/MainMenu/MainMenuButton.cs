// A main menu button from the Figma "Menu & Settings - Component Library"
// (Primary / Neutral / Destructive Button):
//
//   Default  resting fill, no outline
//   Hover    ray-pointer hover: lighter fill, 35% white outline, +2% scale
//   Pressed  trigger held: darker fill, 10% white outline, 98% scale
//
// Transitions take ~120ms with ease-out, as described on the components.
// The Button's own transition is set to None; this component owns the look.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MainMenuButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image fill;
    [SerializeField] private Image border;

    [Header("Fill colours")]
    [SerializeField] private Color defaultColor = new Color32(0x2C, 0x2C, 0x2C, 0xFF);
    [SerializeField] private Color hoverColor = new Color32(0x33, 0x3B, 0x47, 0xFF);
    [SerializeField] private Color pressedColor = new Color32(0x17, 0x1A, 0x1F, 0xFF);

    [Header("Outline colours")]
    [SerializeField] private Color borderDefault = Color.clear;
    [SerializeField] private Color borderHover = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color borderPressed = new Color(1f, 1f, 1f, 0.10f);

    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.02f;
    [SerializeField] private float pressedScale = 0.98f;
    [SerializeField] private float transitionTime = 0.12f;

    private bool hovered;
    private bool pressed;

    private Color fromFill, toFill, fromBorder, toBorder;
    private float fromScale, toScale = 1f;
    private float elapsed;

    // Pointer exit/up never arrive while the panel is hidden, so start clean
    // every time it is shown again.
    private void OnEnable()
    {
        hovered = false;
        pressed = false;
        SetTarget(snap: true);
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; SetTarget(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; SetTarget(); }
    public void OnPointerDown(PointerEventData eventData) { pressed = true; SetTarget(); }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; SetTarget(); }

    private void SetTarget(bool snap = false)
    {
        Color targetFill = pressed ? pressedColor : hovered ? hoverColor : defaultColor;
        Color targetBorder = pressed ? borderPressed : hovered ? borderHover : borderDefault;
        float targetScale = pressed ? pressedScale : hovered ? hoverScale : 1f;

        fromFill = fill != null ? fill.color : targetFill;
        fromBorder = border != null ? border.color : targetBorder;
        fromScale = transform.localScale.x;
        toFill = targetFill;
        toBorder = targetBorder;
        toScale = targetScale;
        elapsed = snap ? transitionTime : 0f;
        Apply(snap ? 1f : 0f);
    }

    private void Update()
    {
        if (elapsed >= transitionTime)
            return;

        elapsed += Time.unscaledDeltaTime;
        float t = transitionTime > 0f ? Mathf.Clamp01(elapsed / transitionTime) : 1f;
        Apply(1f - (1f - t) * (1f - t)); // ease-out
    }

    private void Apply(float t)
    {
        if (fill != null)
            fill.color = Color.Lerp(fromFill, toFill, t);
        if (border != null)
        {
            border.color = Color.Lerp(fromBorder, toBorder, t);
            border.enabled = border.color.a > 0.001f;
        }
        transform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, t);
    }
}
