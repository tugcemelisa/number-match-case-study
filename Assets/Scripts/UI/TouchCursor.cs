using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cosmetic touch-point presentation for the case-study recording: hides
// the OS pointer and follows it with a soft circular indicator instead,
// so the interaction reads as a finger tap rather than a mouse click.
// Idle is a hollow ring; press/drag fills solid and pops slightly larger,
// then eases back on release - the same convention mobile UX recordings
// use to call out touch input. Pure overlay UI on its own top-most
// Canvas: raycastTarget is off and it never reads input itself, so it
// can't block drag-and-drop.
public class TouchCursor : MonoBehaviour
{
    [Tooltip("Overall size of the cursor graphic, in UI reference pixels at the base 260x260 icon size. Lower this if the finger covers too much of the board.")]
    [SerializeField] float cursorScale = 0.35f;

    [Tooltip("Idle (not pressed) finger sprite. Falls back to a plain dot if left empty.")]
    [SerializeField] Sprite idleSprite;

    [Tooltip("Pressed/dragging finger sprite. Falls back to the idle sprite if left empty.")]
    [SerializeField] Sprite pressedSprite;

    const float BaseIconSize = 260f;
    const float PressedScaleMultiplier = 1.12f;
    const float ScaleSpeed = 14f;

    Image _image;
    RectTransform _rect;
    float _targetScaleMultiplier = 1f;
    bool _wasPressed;

    void Awake()
    {
        Canvas canvas = BuildCanvas();
        _image = BuildDot(canvas.transform);
        _rect = _image.rectTransform;
        Cursor.visible = false;
    }

    void OnDestroy()
    {
        Cursor.visible = true;
    }

    Canvas BuildCanvas()
    {
        var go = new GameObject("Touch Cursor Canvas", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>().enabled = false;
        return canvas;
    }

    // Both source images point up-and-left, with the fingertip - the point
    // that should sit exactly on the actual pointer position - well off
    // from the sprite's visual center. Pivoting there instead of at (0.5,
    // 0.5) keeps the tip anchored under the cursor instead of the whole
    // hand's bounding-box center.
    static readonly Vector2 FingertipPivot = new Vector2(0.36f, 0.78f);

    Image BuildDot(Transform parent)
    {
        var go = new GameObject("Finger", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.sprite = idleSprite != null ? idleSprite : UISprites.RoundedRect();
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = true;

        RectTransform rect = image.rectTransform;
        rect.sizeDelta = new Vector2(BaseIconSize, BaseIconSize) * cursorScale;
        rect.pivot = idleSprite != null ? FingertipPivot : new Vector2(0.5f, 0.5f);
        return image;
    }

    void Update()
    {
        if (!TryReadPointer(out Vector2 screenPos, out bool pressed))
            return;

        _rect.position = screenPos;

        if (pressed != _wasPressed)
        {
            _wasPressed = pressed;
            Sprite sprite = pressed && pressedSprite != null ? pressedSprite
                : idleSprite != null ? idleSprite
                : UISprites.RoundedRect();
            _image.sprite = sprite;
        }

        _targetScaleMultiplier = pressed ? PressedScaleMultiplier : 1f;
        float current = _rect.localScale.x;
        float next = Mathf.Lerp(current, _targetScaleMultiplier, Time.unscaledDeltaTime * ScaleSpeed);
        _rect.localScale = Vector3.one * next;
    }

    static bool TryReadPointer(out Vector2 screenPos, out bool pressed)
    {
        if (Mouse.current != null)
        {
            screenPos = Mouse.current.position.ReadValue();
            pressed = Mouse.current.leftButton.isPressed;
            return true;
        }

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            screenPos = touch.position.ReadValue();
            pressed = touch.press.isPressed;
            return true;
        }

        screenPos = default;
        pressed = false;
        return false;
    }
}
