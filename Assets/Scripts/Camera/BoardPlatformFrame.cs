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

    [SerializeField] float thickness = 0.6f;
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

    public void Fit(int gridWidth, int gridHeight, float pieceSize)
    {
        float boardMinX = -0.5f * pieceSize;
        float boardMaxX = (gridWidth - 0.5f) * pieceSize;
        float boardMinZ = -0.5f * pieceSize;
        float boardMaxZ = (gridHeight - 0.5f) * pieceSize;

        float centerX = (boardMinX + boardMaxX) * 0.5f;
        float centerZ = (boardMinZ + boardMaxZ) * 0.5f;
        float outerLength = (boardMaxX - boardMinX) + thickness * 2f;
        float boardHeight = boardMaxZ - boardMinZ;

        PlaceStrip(top, centerX, boardMaxZ + thickness * 0.5f, outerLength, false);
        PlaceStrip(bottom, centerX, boardMinZ - thickness * 0.5f, outerLength, false);
        PlaceStrip(left, boardMinX - thickness * 0.5f, centerZ, boardHeight, true);
        PlaceStrip(right, boardMaxX + thickness * 0.5f, centerZ, boardHeight, true);
    }

    void PlaceStrip(Transform strip, float x, float z, float length, bool vertical)
    {
        strip.position = new Vector3(x, yPosition, z);
        strip.rotation = vertical ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        strip.localScale = new Vector3(length / 10f, 1f, thickness / 10f);
    }
}
