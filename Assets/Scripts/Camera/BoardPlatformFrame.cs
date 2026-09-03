using System.Collections;
using UnityEngine;

// A chunky "platform" border around the board, built from 4 plane strips
// using a fake-3D bevel shader (BoardFrameBevel) rather than real raised
// geometry - the gameplay camera looks perfectly straight down, so actual
// height wouldn't read as depth anyway. The shading alone sells a raised
// wooden/plastic-tray look like the reference puzzle games.
public class BoardPlatformFrame : MonoBehaviour
{
    [SerializeField] Transform top;
    [SerializeField] Transform bottom;
    [SerializeField] Transform left;
    [SerializeField] Transform right;

    [Tooltip("Breathing room between the outermost cell and the frame's inner edge, as a multiple of one cell (pieceSize) - not a fixed world-space distance, so it scales correctly if pieceSize ever changes.")]
    [SerializeField] float paddingMultiplier = 0.5f;
    [SerializeField] float thickness = 0.45f;
    [SerializeField] float yPosition = -0.15f;

    void Start()
    {
        StartCoroutine(FitWhenReady());
    }

    IEnumerator FitWhenReady()
    {
        yield return new WaitUntil(() => LevelSettings.Instance != null);

        LevelSettings settings = LevelSettings.Instance;
        Fit(settings.GridWidth, settings.GridHeight, settings.PieceSize);
    }

    // gridWidth/gridHeight/pieceSize describe the grid's own content bounds
    // only - innerPadding is the sole source of breathing room between the
    // outermost cell and the frame's inner edge, so the frame can never
    // drift out of sync with the grid it's wrapping.
    public void Fit(int gridWidth, int gridHeight, float pieceSize)
    {
        float padding = paddingMultiplier * pieceSize;

        float contentMinX = -0.5f * pieceSize;
        float contentMaxX = (gridWidth - 0.5f) * pieceSize;
        float contentMinZ = -0.5f * pieceSize;
        float contentMaxZ = (gridHeight - 0.5f) * pieceSize;

        float boardMinX = contentMinX - padding;
        float boardMaxX = contentMaxX + padding;
        float boardMinZ = contentMinZ - padding;
        float boardMaxZ = contentMaxZ + padding;

        float centerX = (boardMinX + boardMaxX) * 0.5f;
        float centerZ = (boardMinZ + boardMaxZ) * 0.5f;
        float outerLength = (boardMaxX - boardMinX) + thickness * 2f;
        float boardHeight = boardMaxZ - boardMinZ;

        PlaceStrip(top, centerX, boardMaxZ + thickness * 0.5f, outerLength, false);
        PlaceStrip(bottom, centerX, boardMinZ - thickness * 0.5f, outerLength, false);
        PlaceStrip(left, boardMinX - thickness * 0.5f, centerZ, boardHeight, true);
        PlaceStrip(right, boardMaxX + thickness * 0.5f, centerZ, boardHeight, true);
    }

    // The board frame's own outer bottom edge in the same local/world Z
    // space Fit() positions strips in - lets other systems (the tray
    // platform, specifically) place themselves relative to where this
    // frame actually ends up, instead of guessing its size independently.
    public float GetOuterBottomZ(float pieceSize)
    {
        float contentMinZ = -0.5f * pieceSize;
        float boardMinZ = contentMinZ - paddingMultiplier * pieceSize;
        return boardMinZ - thickness;
    }

    void PlaceStrip(Transform strip, float x, float z, float length, bool vertical)
    {
        strip.position = new Vector3(x, yPosition, z);
        strip.rotation = vertical ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        strip.localScale = new Vector3(length / 10f, 1f, thickness / 10f);
    }
}
