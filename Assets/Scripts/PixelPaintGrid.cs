using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PixelPaintGrid : MonoBehaviour
{
    [SerializeField] RevealEffects revealEffects;

    BoardData _board;
    BoardRenderer _boardRenderer;
    RevealManager _revealManager;
    Texture2D _numberAtlas;
    readonly List<PaintPiece> _leftoverPieces = new();

    public BoardData Board => _board;

    // A wrong drop still landed on a real cell - briefly flash that
    // socket red so the rejection is anchored to a specific place on the
    // board, not just the returning cube.
    public void FlashWrongCell(int cellIndex) => _boardRenderer?.SetWrongFlash(cellIndex);

    // Attempts to fill the given cell with a piece of the given number.
    // Returns false (no state change) if the cell doesn't exist, is already
    // filled, or doesn't match the piece's number. Once every cell in the
    // number's group is filled, kicks off its staggered dissolve reveal
    // plus the particle/camera-shake/audio moment.
    public bool TryPlacePiece(int cellIndex, int number)
    {
        if (_board == null || !_board.TryFillCell(cellIndex, number))
            return false;

        _boardRenderer.SetFilled(cellIndex);

        Vector3 cellWorldPosition = transform.position + _board.GetCellLocalPosition(cellIndex);
        Color cellColor = _board.Cells[cellIndex].color;

        if (_board.IsGroupComplete(number))
        {
            _board.RevealGroup(number);
            _revealManager.RevealGroup(number, this);

            if (revealEffects != null)
                revealEffects.PlayGroupComplete(GetGroupCentroidWorldPosition(number), cellColor);

            if (_board.IsFullyRevealed())
                StartCoroutine(PlayBoardCompleteDelayed());
        }
        else if (revealEffects != null)
        {
            revealEffects.PlayPlacement(cellWorldPosition, cellColor);
        }

        return true;
    }

    // A short anticipation pause after the final group's own reveal
    // starts, so the full-board celebration reads as a bigger, separate
    // beat rather than overlapping it.
    IEnumerator PlayBoardCompleteDelayed()
    {
        yield return new WaitForSeconds(0.5f);

        if (revealEffects != null)
        {
            Vector3 center = transform.position + new Vector3(
                (_board.Width - 1) * _board.PieceSize * 0.5f, 0f, (_board.Height - 1) * _board.PieceSize * 0.5f);
            revealEffects.PlayBoardComplete(center);
        }
    }

    Vector3 GetGroupCentroidWorldPosition(int number)
    {
        List<int> cells = _board.NumberToCellIndices[number];
        Vector3 sum = Vector3.zero;
        foreach (int idx in cells)
            sum += _board.GetCellLocalPosition(idx);
        return transform.position + sum / cells.Count;
    }

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

        TMP_FontAsset numberFont = Resources.Load<TMP_FontAsset>("Bangers SDF");
        if (numberFont == null)
        {
            Debug.LogError("PixelPaintGrid: missing default TMP font asset for number baking.");
            yield break;
        }

        _numberAtlas = NumberAtlasBaker.Bake(_board, numberFont);
        _boardRenderer = new BoardRenderer(_board, cubeMesh, boardMaterial, transform.position, _numberAtlas);
        _revealManager = new RevealManager(_board, _boardRenderer);

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
            piece.TrayPosition = position;

            _leftoverPieces.Add(piece);
        }

        if (slot > 0)
        {
            int lastRow = (slot - 1) / _board.Width;
            int maxCol = Mathf.Min(slot, _board.Width) - 1;
            TrayPlatform trayPlatform = FindAnyObjectByType<TrayPlatform>();
            if (trayPlatform != null)
                trayPlatform.Fit(transform.position, 0f, maxCol * pieceSize, startZ - lastRow * pieceSize, startZ);
        }
    }

    public void Regenerate()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        _leftoverPieces.Clear();
        _board = null;
        _boardRenderer = null;
        _revealManager = null;

        if (_numberAtlas != null)
        {
            Destroy(_numberAtlas);
            _numberAtlas = null;
        }

        StartCoroutine(Init());
    }

    bool LoadStartFilledState(int index)
    {
        LevelSettings settings = LevelSettings.Instance;
        string key = $"PieceVisibility_{settings.GridWidth}x{settings.GridHeight}_{index}";
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
