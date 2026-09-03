using System.Collections.Generic;
using UnityEngine;

public struct Cell
{
    public int number;
    public Color color;
    public bool filled;
    public bool revealed;
    public int x;
    public int z;
}

class PieceColor
{
    public int colorNumber;
    public Color color;
}

// Owns the puzzle's per-cell data (number, true color, fill/reveal state) and
// the number->cell-indices lookup used for group-completion checks. Replaces
// the per-cell GameObjects PixelPaintGrid used to spawn directly - BoardData
// is pure data, rendered separately by BoardRenderer.
public class BoardData
{
    public int Width { get; }
    public int Height { get; }
    public float PieceSize { get; }
    public Cell[] Cells { get; }
    public Dictionary<int, List<int>> NumberToCellIndices { get; } = new();

    readonly List<PieceColor> _colorsList = new();
    readonly float _colorTolerance;

    public BoardData(Texture2D sourceImage, int width, int height, float pieceSize, float colorTolerance,
        System.Func<int, bool> loadStartFilled)
    {
        Width = width;
        Height = height;
        PieceSize = pieceSize;
        _colorTolerance = colorTolerance;
        Cells = new Cell[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int index = x * height + z;
                Color originalColor = sourceImage.GetPixelBilinear((float)x / width, (float)z / height);
                PieceColor closest = GetOrAddClosestColor(originalColor);
                bool startFilled = loadStartFilled(index);

                Cells[index] = new Cell
                {
                    number = closest.colorNumber,
                    color = closest.color,
                    filled = startFilled,
                    revealed = false,
                    x = x,
                    z = z,
                };

                if (!NumberToCellIndices.TryGetValue(closest.colorNumber, out List<int> list))
                {
                    list = new List<int>();
                    NumberToCellIndices[closest.colorNumber] = list;
                }
                list.Add(index);
            }
        }

        // A number whose cells all happen to start filled is trivially
        // "complete" before the player does anything - reveal it up front.
        // A group with only SOME cells pre-filled stays masked (matches
        // Task 1: no color anywhere until a full group is complete).
        foreach (KeyValuePair<int, List<int>> group in NumberToCellIndices)
        {
            if (IsGroupComplete(group.Value))
                RevealGroup(group.Value);
        }
    }

    public bool IsGroupComplete(int number) => IsGroupComplete(NumberToCellIndices[number]);

    bool IsGroupComplete(List<int> cellIndices)
    {
        foreach (int index in cellIndices)
            if (!Cells[index].filled)
                return false;
        return true;
    }

    public bool IsFullyRevealed()
    {
        foreach (Cell cell in Cells)
            if (!cell.revealed)
                return false;
        return true;
    }

    public void RevealGroup(int number) => RevealGroup(NumberToCellIndices[number]);

    void RevealGroup(List<int> cellIndices)
    {
        foreach (int index in cellIndices)
        {
            Cell cell = Cells[index];
            cell.revealed = true;
            Cells[index] = cell;
        }
    }

    // Fills the specific cell under a drop point - the player must drop a
    // piece on a cell that both matches its number AND isn't already filled,
    // not just anywhere a matching number happens to still be open.
    public bool TryFillCell(int cellIndex, int number)
    {
        if (cellIndex < 0 || cellIndex >= Cells.Length)
            return false;

        Cell cell = Cells[cellIndex];
        if (cell.filled || cell.number != number)
            return false;

        cell.filled = true;
        Cells[cellIndex] = cell;
        return true;
    }

    public Vector3 GetCellLocalPosition(int cellIndex)
    {
        Cell cell = Cells[cellIndex];
        return new Vector3(cell.x * PieceSize, 0f, cell.z * PieceSize);
    }

    public bool TryGetCellIndex(Vector3 localPosition, out int cellIndex)
    {
        int x = Mathf.RoundToInt(localPosition.x / PieceSize);
        int z = Mathf.RoundToInt(localPosition.z / PieceSize);

        if (x < 0 || x >= Width || z < 0 || z >= Height)
        {
            cellIndex = -1;
            return false;
        }

        cellIndex = x * Height + z;
        return true;
    }

    PieceColor GetOrAddClosestColor(Color color)
    {
        foreach (PieceColor existing in _colorsList)
            if (IsColorSimilar(color, existing.color))
                return existing;

        PieceColor added = new PieceColor { color = color, colorNumber = _colorsList.Count + 1 };
        _colorsList.Add(added);
        return added;
    }

    bool IsColorSimilar(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < _colorTolerance
            && Mathf.Abs(a.g - b.g) < _colorTolerance
            && Mathf.Abs(a.b - b.b) < _colorTolerance;
    }
}
