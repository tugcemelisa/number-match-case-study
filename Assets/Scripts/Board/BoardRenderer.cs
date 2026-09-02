using UnityEngine;

// GPU-instanced replacement for spawning one GameObject per board cell.
// Batches cells into groups of <=1023 (Graphics.DrawMeshInstanced's limit)
// and redraws every frame from cached transform/color arrays, so board size
// no longer determines GameObject count, draw calls scale with cell count
// only in fixed 1023-sized steps, and per-cell color changes are cheap
// MaterialPropertyBlock array writes rather than Renderer/Material churn.
public class BoardRenderer
{
    public static readonly Color MaskColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    const int BatchSize = 1023;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    readonly BoardData _board;
    readonly Mesh _mesh;
    readonly Material _material;
    readonly Vector3 _origin;

    readonly Matrix4x4[][] _batchMatrices;
    readonly Vector4[][] _batchColors;
    readonly MaterialPropertyBlock[] _batchBlocks;
    readonly bool[] _batchDirty;

    public BoardRenderer(BoardData board, Mesh mesh, Material material, Vector3 origin)
    {
        _board = board;
        _mesh = mesh;
        _material = material;
        _origin = origin;

        int cellCount = board.Cells.Length;
        int batchCount = Mathf.CeilToInt(cellCount / (float)BatchSize);

        _batchMatrices = new Matrix4x4[batchCount][];
        _batchColors = new Vector4[batchCount][];
        _batchBlocks = new MaterialPropertyBlock[batchCount];
        _batchDirty = new bool[batchCount];

        for (int b = 0; b < batchCount; b++)
        {
            int start = b * BatchSize;
            int count = Mathf.Min(BatchSize, cellCount - start);

            _batchMatrices[b] = new Matrix4x4[count];
            _batchColors[b] = new Vector4[count];
            _batchBlocks[b] = new MaterialPropertyBlock();

            for (int i = 0; i < count; i++)
                BuildInstance(b, i, start + i);

            _batchBlocks[b].SetVectorArray(BaseColorId, _batchColors[b]);
        }
    }

    void BuildInstance(int batch, int localIndex, int cellIndex)
    {
        Cell cell = _board.Cells[cellIndex];
        Vector3 position = _origin + _board.GetCellLocalPosition(cellIndex);
        _batchMatrices[batch][localIndex] = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * _board.PieceSize);
        _batchColors[batch][localIndex] = cell.revealed ? (Vector4)cell.color : (Vector4)MaskColor;
    }

    public void RefreshCell(int cellIndex)
    {
        int batch = cellIndex / BatchSize;
        int local = cellIndex % BatchSize;
        Cell cell = _board.Cells[cellIndex];
        _batchColors[batch][local] = cell.revealed ? (Vector4)cell.color : (Vector4)MaskColor;
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
