using UnityEngine;

// GPU-instanced replacement for spawning one GameObject per board cell.
// Batches cells into groups of <=1023 (Graphics.DrawMeshInstanced's limit)
// and redraws every frame from cached transform/color arrays, so board size
// no longer determines GameObject count, draw calls scale with cell count
// only in fixed 1023-sized steps, and per-cell color changes are cheap
// MaterialPropertyBlock array writes rather than Renderer/Material churn.
//
// Numbers on masked cells are drawn by the shader sampling one shared,
// pre-baked atlas texture (see NumberAtlasBaker) rather than per-cell
// TextMeshPro objects - _CellUV tells each instance which sub-rect of that
// atlas is its own number. _BaseColor.a doubles as a "show number" flag
// (1 while masked, 0 once revealed) so the shader knows when to stop
// sampling the atlas for a cell.
public class BoardRenderer
{
    public static readonly Color MaskColor = new Color(0.32f, 0.32f, 0.34f, 1f);
    const int BatchSize = 1023;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int CellUVId = Shader.PropertyToID("_CellUV");
    static readonly int NumberAtlasId = Shader.PropertyToID("_NumberAtlas");

    readonly BoardData _board;
    readonly Mesh _mesh;
    readonly Material _material;
    readonly Vector3 _origin;

    readonly Matrix4x4[][] _batchMatrices;
    readonly Vector4[][] _batchColors;
    readonly Vector4[][] _batchCellUV;
    readonly MaterialPropertyBlock[] _batchBlocks;
    readonly bool[] _batchDirty;

    public BoardRenderer(BoardData board, Mesh mesh, Material material, Vector3 origin, Texture2D numberAtlas)
    {
        _board = board;
        _mesh = mesh;
        _material = material;
        _origin = origin;

        _material.SetTexture(NumberAtlasId, numberAtlas);

        int cellCount = board.Cells.Length;
        int batchCount = Mathf.CeilToInt(cellCount / (float)BatchSize);

        _batchMatrices = new Matrix4x4[batchCount][];
        _batchColors = new Vector4[batchCount][];
        _batchCellUV = new Vector4[batchCount][];
        _batchBlocks = new MaterialPropertyBlock[batchCount];
        _batchDirty = new bool[batchCount];

        float cellDu = 1f / board.Width;
        float cellDv = 1f / board.Height;

        for (int b = 0; b < batchCount; b++)
        {
            int start = b * BatchSize;
            int count = Mathf.Min(BatchSize, cellCount - start);

            _batchMatrices[b] = new Matrix4x4[count];
            _batchColors[b] = new Vector4[count];
            _batchCellUV[b] = new Vector4[count];
            _batchBlocks[b] = new MaterialPropertyBlock();

            for (int i = 0; i < count; i++)
                BuildInstance(b, i, start + i, cellDu, cellDv);

            _batchBlocks[b].SetVectorArray(BaseColorId, _batchColors[b]);
            _batchBlocks[b].SetVectorArray(CellUVId, _batchCellUV[b]);
        }
    }

    void BuildInstance(int batch, int localIndex, int cellIndex, float cellDu, float cellDv)
    {
        Cell cell = _board.Cells[cellIndex];
        Vector3 position = _origin + _board.GetCellLocalPosition(cellIndex);
        _batchMatrices[batch][localIndex] = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * _board.PieceSize);
        _batchColors[batch][localIndex] = MakeColorAndFlag(cell);
        _batchCellUV[batch][localIndex] = new Vector4(cell.x * cellDu, cell.z * cellDv, cellDu, cellDv);
    }

    static Vector4 MakeColorAndFlag(Cell cell)
    {
        Color color = cell.revealed ? cell.color : MaskColor;
        float showNumber = cell.revealed ? 0f : 1f;
        return new Vector4(color.r, color.g, color.b, showNumber);
    }

    public void RefreshCell(int cellIndex)
    {
        int batch = cellIndex / BatchSize;
        int local = cellIndex % BatchSize;
        _batchColors[batch][local] = MakeColorAndFlag(_board.Cells[cellIndex]);
        _batchDirty[batch] = true;
    }

    public void DrawAll()
    {
        for (int b = 0; b < _batchMatrices.Length; b++)
        {
            if (_batchDirty[b])
            {
                _batchBlocks[b].SetVectorArray(BaseColorId, _batchColors[b]);
                _batchDirty[b] = false;
            }

            Graphics.DrawMeshInstanced(_mesh, 0, _material, _batchMatrices[b], _batchMatrices[b].Length, _batchBlocks[b]);
        }
    }
}
