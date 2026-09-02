using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceColor
{
    public int colorNumber;
    public Color color;
}

public class PixelPaintGrid : MonoBehaviour
{
    private float _pieceSize;
    private int _width;
    private int _height;
    private float _colorTolerance;

    private Texture2D _sourceImage;
    private PaintPiece _paintPiece;
    private List<PieceColor> _colorsList = new();
    private List<PieceColor> _invisibleColorsList = new();
    private readonly List<PaintPiece> _leftoverPieces = new();

    void Start()
    {
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return new WaitUntil(() => LevelSettings.Instance != null);

        LevelSettings settings = LevelSettings.Instance;

        _sourceImage = settings.GetPaintingSprite();
        _paintPiece = settings.PiecePrefab;
        _width = settings.GridWidth;
        _height = settings.GridHeight;
        _pieceSize = settings.PieceSize;
        _colorTolerance = settings.ColorTolerance;

        if (_sourceImage == null)
        {
            Debug.LogError("PixelPaintGrid: SourceImage is not set on LevelSettings.");
            yield break;
        }

        if (_paintPiece == null)
        {
            Debug.LogError("PixelPaintGrid: PiecePrefab is not set on LevelSettings.");
            yield break;
        }

        GeneratePixelGrid();
        SpawnLeftoverPieces();
    }

    void GeneratePixelGrid()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                Vector3 position = transform.position + new Vector3(x * _pieceSize, 0, z * _pieceSize);
                PaintPiece piece = Instantiate(_paintPiece, position, Quaternion.identity, transform);
                piece.transform.localScale *= _pieceSize;

                Color originalColor = _sourceImage.GetPixelBilinear((float)x / _width, (float)z / _height);
                PieceColor closestPieceColor = GetOrAddClosestColor(originalColor);

                int pieceIndex = x * _height + z;
                piece.Init(closestPieceColor.color, closestPieceColor.colorNumber);

                bool isPieceVisible = LoadVisibilityState(pieceIndex);
                piece.SetVisible(isPieceVisible);

                if (!isPieceVisible)
                    _invisibleColorsList.Add(closestPieceColor);
            }
        }
    }


    void SpawnLeftoverPieces()
    {
        float startZ = _height * _pieceSize + _pieceSize;

        for (int i = 0; i < _invisibleColorsList.Count; i++)
        {
            PieceColor pieceColor = _invisibleColorsList[i];

            int row = i / _width;
            int col = i % _width;

            Vector3 position = transform.position + new Vector3(col * _pieceSize, 0, startZ + row * _pieceSize);
            PaintPiece piece = Instantiate(_paintPiece, position, Quaternion.identity, transform);
            piece.transform.localScale *= _pieceSize;

            piece.Init(pieceColor.color, pieceColor.colorNumber);
            piece.SetTrayVisible();

            _leftoverPieces.Add(piece);
        }
    }

    PieceColor GetOrAddClosestColor(Color color)
    {
        foreach (PieceColor existingPieceColor in _colorsList)
        {
            if (IsColorSimilar(color, existingPieceColor.color))
                return existingPieceColor;
        }

        PieceColor newPieceColor = new PieceColor { color = color, colorNumber = _colorsList.Count + 1 };
        _colorsList.Add(newPieceColor);
        return newPieceColor;
    }

    bool IsColorSimilar(Color color1, Color color2)
    {
        float diffR = Mathf.Abs(color1.r - color2.r);
        float diffG = Mathf.Abs(color1.g - color2.g);
        float diffB = Mathf.Abs(color1.b - color2.b);

        return (diffR < _colorTolerance) && (diffG < _colorTolerance) && (diffB < _colorTolerance);
    }

    public void Regenerate()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        _colorsList.Clear();
        _invisibleColorsList.Clear();
        _leftoverPieces.Clear();

        StartCoroutine(Init());
    }

    bool LoadVisibilityState(int index)
    {
        string key = $"PieceVisibility_{index}";
        if (PlayerPrefs.HasKey(key))
        {
            return PlayerPrefs.GetInt(key) == 1;
        }
        else
        {
            bool isVisible = Random.value < LevelSettings.Instance.VisibilityRate;
            SaveVisibilityState(index, isVisible);
            return isVisible;
        }
    }

    void SaveVisibilityState(int index, bool isVisible)
    {
        string key = $"PieceVisibility_{index}";
        PlayerPrefs.SetInt(key, isVisible ? 1 : 0);
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}