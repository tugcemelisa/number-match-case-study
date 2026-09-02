using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PixelPaintGrid : MonoBehaviour
{
    BoardData _board;
    BoardRenderer _boardRenderer;
    Texture2D _numberAtlas;
    readonly List<PaintPiece> _leftoverPieces = new();

    public BoardData Board => _board;

    void Start()
    {
        StartCoroutine(Init());
    }

    void Update()
    {
        _boardRenderer?.DrawAll();
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => LevelSettings.Instance != null);

        LevelSettings settings = LevelSettings.Instance;

        if (settings.SourceImage == null)
        {
            Debug.LogError("PixelPaintGrid: SourceImage is not set on LevelSettings.");
            yield break;
        }

        if (settings.PiecePrefab == null)
        {
            Debug.LogError("PixelPaintGrid: PiecePrefab is not set on LevelSettings.");
            yield break;
        }

        _board = new BoardData(
            settings.SourceImage,
            settings.GridWidth,
            settings.GridHeight,
            settings.PieceSize,
            settings.ColorTolerance,
            LoadStartFilledState);

        Mesh cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        Material boardMaterial = Resources.Load<Material>("BoardInstanced");

        if (cubeMesh == null)
        {
            Debug.LogError("PixelPaintGrid: built-in Cube.fbx mesh not found.");
            yield break;
        }

        if (boardMaterial == null)
        {
            Debug.LogError("PixelPaintGrid: missing Resources/BoardInstanced material.");
            yield break;
        }

        TMP_FontAsset numberFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (numberFont == null)
        {
            Debug.LogError("PixelPaintGrid: missing default TMP font asset for number baking.");
            yield break;
        }

        _numberAtlas = NumberAtlasBaker.Bake(_board, numberFont);
        _boardRenderer = new BoardRenderer(_board, cubeMesh, boardMaterial, transform.position, _numberAtlas);

        SpawnLeftoverPieces(settings.PiecePrefab, settings.PieceSize);
    }

    void SpawnLeftoverPieces(PaintPiece piecePrefab, float pieceSize)
    {
        // Tray sits below the board (negative Z), not above it: dragging a
        // piece upward onto the board keeps the player's hand/finger clear
        // of the board instead of covering it while dragging downward.
        float startZ = -(pieceSize * 2);
        int slot = 0;

        for (int i = 0; i < _board.Cells.Length; i++)
        {
            Cell cell = _board.Cells[i];
            if (cell.filled)
                continue;

            int row = slot / _board.Width;
            int col = slot % _board.Width;
            slot++;

            Vector3 position = transform.position + new Vector3(col * pieceSize, 0, startZ - row * pieceSize);
            PaintPiece piece = Instantiate(piecePrefab, position, Quaternion.identity, transform);
            piece.transform.localScale *= pieceSize;

            piece.Init(cell.color, cell.number);
            piece.SetTrayVisible();

            _leftoverPieces.Add(piece);
        }
    }

    public void Regenerate()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        _leftoverPieces.Clear();
        _board = null;
        _boardRenderer = null;

        if (_numberAtlas != null)
        {
            Destroy(_numberAtlas);
            _numberAtlas = null;
        }

        StartCoroutine(Init());
    }

    bool LoadStartFilledState(int index)
    {
        string key = $"PieceVisibility_{index}";
        if (PlayerPrefs.HasKey(key))
            return PlayerPrefs.GetInt(key) == 1;

        bool startFilled = Random.value < LevelSettings.Instance.VisibilityRate;
        PlayerPrefs.SetInt(key, startFilled ? 1 : 0);
        return startFilled;
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}
