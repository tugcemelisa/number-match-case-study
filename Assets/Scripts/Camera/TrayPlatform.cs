using UnityEngine;

// Tray backing: a beveled purple frame (reusing the same border-strip
// construction as BoardPlatformFrame) around a flat dark recessed "well"
// plane, so the colored cubes read as sitting inside an open tray/box
// rather than floating over a bare plate or needing individual empty-slot
// sprites for unused positions. Sized every time the tray layout changes,
// driven entirely by the actual cube content bounds PixelPaintGrid passes
// in - never sized independently of what's actually in the tray.
public class TrayPlatform : MonoBehaviour
{
    [SerializeField] Transform top;
    [SerializeField] Transform bottom;
    [SerializeField] Transform left;
    [SerializeField] Transform right;
    [SerializeField] Transform innerWell;

    [Tooltip("Breathing room between the cubes' footprint and the recessed well's edge, as a multiple of one cube (pieceSize) - not a fixed world-space distance, so it scales correctly if pieceSize ever changes.")]
    [SerializeField] float paddingMultiplier = 0.5f;
    [SerializeField] float frameThickness = 0.4f;
    [SerializeField] float wellYPosition = -0.22f;
    [SerializeField] float frameYPosition = -0.08f;

    // minX/maxX/minZ/maxZ already describe the cubes' full visual footprint
    // (their own half-size overhang included) in boardOrigin-local space -
    // this only adds the tray's own breathing room on top of that, it
    // never has to compensate for cube geometry itself.
    public void Fit(Vector3 boardOrigin, float minX, float maxX, float minZ, float maxZ, float pieceSize)
    {
        bool active = maxX > minX && maxZ > minZ;
        gameObject.SetActive(active);
        if (!active)
            return;

        float padding = paddingMultiplier * pieceSize;
        float wellMinX = minX - padding;
        float wellMaxX = maxX + padding;
        float wellMinZ = minZ - padding;
        float wellMaxZ = maxZ + padding;

        float centerX = boardOrigin.x + (wellMinX + wellMaxX) * 0.5f;
        float centerZ = boardOrigin.z + (wellMinZ + wellMaxZ) * 0.5f;
        float wellWidth = wellMaxX - wellMinX;
        float wellDepth = wellMaxZ - wellMinZ;

        innerWell.position = new Vector3(centerX, wellYPosition, centerZ);
        innerWell.localScale = new Vector3(wellWidth / 10f, 1f, wellDepth / 10f);

        float outerLength = wellWidth + frameThickness * 2f;
        PlaceStrip(top, centerX, boardOrigin.z + wellMaxZ + frameThickness * 0.5f, outerLength, false);
        PlaceStrip(bottom, centerX, boardOrigin.z + wellMinZ - frameThickness * 0.5f, outerLength, false);
        PlaceStrip(left, boardOrigin.x + wellMinX - frameThickness * 0.5f, centerZ, wellDepth, true);
        PlaceStrip(right, boardOrigin.x + wellMaxX + frameThickness * 0.5f, centerZ, wellDepth, true);
    }

    // Distance from a tray row's cube-center Z to this tray's own outer top
    // edge once Fit() lays it out - lets PixelPaintGrid work backwards from
    // "where should the tray's outer edge sit" to "where should the first
    // row of cube centers go", instead of guessing the tray's own size.
    public float GetTopEdgeOffsetFromCubeCenter(float pieceSize)
    {
        return pieceSize * 0.5f + paddingMultiplier * pieceSize + frameThickness;
    }

    void PlaceStrip(Transform strip, float x, float z, float length, bool vertical)
    {
        strip.position = new Vector3(x, frameYPosition, z);
        strip.rotation = vertical ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        strip.localScale = new Vector3(length / 10f, 1f, frameThickness / 10f);
    }
}
