using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Orchestrates the staggered, per-cell dissolve animation for a completed
// number group: cells nearest the group's centroid start dissolving first,
// creating an outward wave instead of the whole group flipping color at
// once. Pure C# (not a MonoBehaviour) - the caller supplies the coroutine
// host, since animating each cell is just writing one float into
// BoardRenderer's per-instance array, independent of grid size.
public class RevealManager
{
    const float CellDissolveDuration = 0.55f;
    const float StaggerPerCell = 0.02f;
    const float MaxStagger = 0.5f;

    readonly BoardData _board;
    readonly BoardRenderer _renderer;

    public RevealManager(BoardData board, BoardRenderer renderer)
    {
        _board = board;
        _renderer = renderer;
    }

    public void RevealGroup(int number, MonoBehaviour coroutineHost)
    {
        List<int> ordered = OrderByDistanceFromCentroid(_board.NumberToCellIndices[number]);

        for (int i = 0; i < ordered.Count; i++)
        {
            float delay = Mathf.Min(i * StaggerPerCell, MaxStagger);
            coroutineHost.StartCoroutine(AnimateCell(ordered[i], delay));
        }
    }

    List<int> OrderByDistanceFromCentroid(List<int> cells)
    {
        Vector2 centroid = Vector2.zero;
        foreach (int idx in cells)
        {
            Cell c = _board.Cells[idx];
            centroid += new Vector2(c.x, c.z);
        }
        centroid /= cells.Count;

        List<int> ordered = new List<int>(cells);
        ordered.Sort((a, b) =>
        {
            float da = (CellPos(a) - centroid).sqrMagnitude;
            float db = (CellPos(b) - centroid).sqrMagnitude;
            return da.CompareTo(db);
        });
        return ordered;
    }

    Vector2 CellPos(int cellIndex)
    {
        Cell c = _board.Cells[cellIndex];
        return new Vector2(c.x, c.z);
    }

    IEnumerator AnimateCell(int cellIndex, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float t = 0f;
        while (t < CellDissolveDuration)
        {
            t += Time.deltaTime;
            _renderer.SetRevealProgress(cellIndex, Mathf.Clamp01(t / CellDissolveDuration));
            yield return null;
        }

        _renderer.SetRevealProgress(cellIndex, 1f);
    }
}
