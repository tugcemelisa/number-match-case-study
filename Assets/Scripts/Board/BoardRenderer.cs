using UnityEngine;

// GPU-instanced replacement for spawning one GameObject per board cell.
// Batches cells into groups of <=1023 (Graphics.DrawMeshInstanced's limit)
// and redraws every frame from cached transform/color arrays, so board size
// no longer determines GameObject count, draw calls scale with cell count
// only in fixed 1023-sized steps, and per-cell state changes are cheap
// MaterialPropertyBlock array writes rather than Renderer/Material churn.
//
// Numbers on masked cells are drawn by the shader sampling one shared,
// pre-baked atlas texture (see NumberAtlasBaker) rather than per-cell
// TextMeshPro objects - _CellUV tells each instance which sub-rect of that
// atlas is its own number. _RevealProgress (0..1) drives the shader's
// noise-dissolve transition from the flat mask color to the cell's true
// color; RevealManager animates it per cell, this class just holds and
// uploads the per-instance array.
public class BoardRenderer
{
    public static readonly Color MaskColor = new Color(0.32f, 0.32f, 0.34f, 1f);
    const int BatchSize = 1023;
    static readonly int TrueColorId = Shader.PropertyToID("_TrueColor");
    static readonly int CellUVId = Shader.PropertyToID("_CellUV");
    static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");
    static readonly int NumberAtlasId = Shader.PropertyToID("_NumberAtlas");
    static readonly int NoiseTexId = Shader.PropertyToID("_NoiseTex");
    static readonly int MaskColorId = Shader.PropertyToID("_MaskColor");

    readonly BoardData _board;
    readonly Mesh _mesh;
    readonly Material _material;
    readonly Vector3 _origin;

    readonly Matrix4x4[][] _batchMatrices;
    readonly Vector4[][] _batchTrueColor;
    readonly Vector4[][] _batchCellUV;
    readonly float[][] _batchRevealProgress;
    readonly MaterialPropertyBlock[] _batchBlocks;
    readonly bool[] _batchDirty;

    public BoardRenderer(BoardData board, Mesh mesh, Material material, Vector3 origin, Texture2D numberAtlas)
    {
        _board = board;
        _mesh = mesh;
        _material = material;
        _origin = origin;

        _material.SetTexture(NumberAtlasId, numberAtlas);
        _material.SetTexture(NoiseTexId, GenerateNoiseTexture(256));
        _material.SetColor(MaskColorId, MaskColor);

        int cellCount = board.Cells.Length;
        int batchCount = Mathf.CeilToInt(cellCount / (float)BatchSize);

        _batchMatrices = new Matrix4x4[batchCount][];
        _batchTrueColor = new Vector4[batchCount][];
        _batchCellUV = new Vector4[batchCount][];
        _batchRevealProgress = new float[batchCount][];
        _batchBlocks = new MaterialPropertyBlock[batchCount];
        _batchDirty = new bool[batchCount];

        float cellDu = 1f / board.Width;
        float cellDv = 1f / board.Height;

        for (int b = 0; b < batchCount; b++)
        {
            int start = b * BatchSize;
            int count = Mathf.Min(BatchSize, cellCount - start);

            _batchMatrices[b] = new Matrix4x4[count];
            _batchTrueColor[b] = new Vector4[count];
            _batchCellUV[b] = new Vector4[count];
            _batchRevealProgress[b] = new float[count];
            _batchBlocks[b] = new MaterialPropertyBlock();

            for (int i = 0; i < count; i++)
                BuildInstance(b, i, start + i, cellDu, cellDv);

            _batchBlocks[b].SetVectorArray(TrueColorId, _batchTrueColor[b]);
            _batchBlocks[b].SetVectorArray(CellUVId, _batchCellUV[b]);
            _batchBlocks[b].SetFloatArray(RevealProgressId, _batchRevealProgress[b]);
        }
    }

    void BuildInstance(int batch, int localIndex, int cellIndex, float cellDu, float cellDv)
    {
        Cell cell = _board.Cells[cellIndex];
        Vector3 position = _origin + _board.GetCellLocalPosition(cellIndex);
        _batchMatrices[batch][localIndex] = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * _board.PieceSize);
        _batchTrueColor[batch][localIndex] = cell.color;
        _batchCellUV[batch][localIndex] = new Vector4(cell.x * cellDu, cell.z * cellDv, cellDu, cellDv);
        _batchRevealProgress[batch][localIndex] = cell.revealed ? 1f : 0f;
    }

    // Instant on/off (used for pre-filled groups at board build time and as
    // a fallback) - for the animated version see SetRevealProgress.
    public void RefreshCell(int cellIndex) => SetRevealProgress(cellIndex, _board.Cells[cellIndex].revealed ? 1f : 0f);

    public void SetRevealProgress(int cellIndex, float progress)
    {
        int batch = cellIndex / BatchSize;
        int local = cellIndex % BatchSize;
        _batchRevealProgress[batch][local] = progress;
        _batchDirty[batch] = true;
    }

    public void DrawAll()
    {
        for (int b = 0; b < _batchMatrices.Length; b++)
        {
            if (_batchDirty[b])
            {
                _batchBlocks[b].SetFloatArray(RevealProgressId, _batchRevealProgress[b]);
                _batchDirty[b] = false;
            }

            Graphics.DrawMeshInstanced(_mesh, 0, _material, _batchMatrices[b], _batchMatrices[b].Length, _batchBlocks[b]);
        }
    }

    static Texture2D GenerateNoiseTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.R8, false)
        {
            name = "DissolveNoise",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
        };

        var pixels = new Color32[size * size];
        float scale = 6f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x / (float)size * scale, y / (float)size * scale);
                byte v = (byte)Mathf.Clamp(n * 255f, 0f, 255f);
                pixels[y * size + x] = new Color32(v, v, v, v);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
