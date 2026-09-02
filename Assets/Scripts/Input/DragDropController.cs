using UnityEngine;
using UnityEngine.InputSystem;

// Drag-and-drop for tray pieces. Uses the new Input System's low-level
// Mouse/Touchscreen devices directly rather than routing through the
// project's UI action map - simpler to reason about for a single
// press/drag/release gesture, and works for both mouse (editor/PC) and
// touch (mobile) without extra wiring.
public class DragDropController : MonoBehaviour
{
    [SerializeField] PixelPaintGrid grid;
    [SerializeField] float dragHeight = 0.6f;

    Camera _camera;
    Plane _groundPlane;
    PaintPiece _draggedPiece;

    void Awake()
    {
        _camera = Camera.main;
        _groundPlane = new Plane(Vector3.up, Vector3.zero);
    }

    void Update()
    {
        if (grid == null || grid.Board == null || _camera == null)
            return;

        if (!TryReadPointer(out Vector2 screenPos, out bool pressedThisFrame, out bool releasedThisFrame))
            return;

        if (_draggedPiece == null)
        {
            if (pressedThisFrame)
                TryBeginDrag(screenPos);
        }
        else if (releasedThisFrame)
        {
            EndDrag(screenPos);
        }
        else
        {
            UpdateDrag(screenPos);
        }
    }

    static bool TryReadPointer(out Vector2 screenPos, out bool pressedThisFrame, out bool releasedThisFrame)
    {
        if (Mouse.current != null)
        {
            screenPos = Mouse.current.position.ReadValue();
            pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
            return true;
        }

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            screenPos = touch.position.ReadValue();
            pressedThisFrame = touch.press.wasPressedThisFrame;
            releasedThisFrame = touch.press.wasReleasedThisFrame;
            return touch.press.isPressed || releasedThisFrame;
        }

        screenPos = default;
        pressedThisFrame = false;
        releasedThisFrame = false;
        return false;
    }

    void TryBeginDrag(Vector2 screenPos)
    {
        Ray ray = _camera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        PaintPiece piece = hit.collider.GetComponentInParent<PaintPiece>();
        if (piece == null)
            return;

        _draggedPiece = piece;
        piece.SetColliderEnabled(false);
        UpdateDrag(screenPos);
    }

    void UpdateDrag(Vector2 screenPos)
    {
        if (TryGetGroundPoint(screenPos, out Vector3 groundPoint))
            _draggedPiece.transform.position = new Vector3(groundPoint.x, dragHeight, groundPoint.z);
    }

    void EndDrag(Vector2 screenPos)
    {
        PaintPiece piece = _draggedPiece;
        _draggedPiece = null;

        bool placed = false;

        if (TryGetGroundPoint(screenPos, out Vector3 groundPoint))
        {
            Vector3 local = groundPoint - grid.transform.position;
            if (grid.Board.TryGetCellIndex(local, out int cellIndex))
                placed = grid.TryPlacePiece(cellIndex, piece.Number);
        }

        if (placed)
            Destroy(piece.gameObject);
        else
            piece.PlayRejectAndReturn();
    }

    bool TryGetGroundPoint(Vector2 screenPos, out Vector3 point)
    {
        Ray ray = _camera.ScreenPointToRay(screenPos);
        if (_groundPlane.Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }

        point = default;
        return false;
    }
}
