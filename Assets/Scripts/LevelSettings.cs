using UnityEngine;

public class LevelSettings : MonoBehaviour
{
    public static LevelSettings Instance;

    public void Awake()
    {
        Instance = this;
    }

    [Header("Painting")] public float VisibilityRate = 0.8f;
    public Texture2D SourceImage;

    [Header("Grid")] public PaintPiece PiecePrefab;
    public int GridWidth = 32;
    public int GridHeight = 32;
    public float PieceSize = 1f;
    public float ColorTolerance = 0.1f;

    private PixelPaintGrid _grid;

    public Texture2D GetPaintingSprite() => SourceImage;

    [ContextMenu("Reset Painting")]
    public void ResetPainting()
    {
        if (_grid == null)
            _grid = FindAnyObjectByType<PixelPaintGrid>();

        for (int i = 0; i < GridWidth * GridHeight; i++)
            PlayerPrefs.DeleteKey($"PieceVisibility_{i}");

        PlayerPrefs.Save();

        if (Application.isPlaying && _grid != null)
            _grid.Regenerate();
    }
}