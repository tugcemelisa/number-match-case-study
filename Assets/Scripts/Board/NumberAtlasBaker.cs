using System.Collections.Generic;
using TMPro;
using UnityEngine;

// One-time bake of each DISTINCT NUMBER (not each cell) into a single
// small shared texture. A board only ever has as many distinct numbers as
// the source image has color groups - bounded by the palette, not by
// cell count - so a 64x64 board (4096 cells) with, say, 16 distinct
// numbers still only spawns 16 TMP objects and bakes a small texture,
// not one atlas region per cell. The previous per-cell version scaled the
// atlas with grid area (4096x4096 at 64x64, exactly 64MB as RGBA32),
// which overflowed this environment's D3D12 upload buffer and made the
// bake effectively hang; this version's atlas size is independent of
// grid size entirely.
public static class NumberAtlasBaker
{
    const int CellPixelSize = 96;

    public static (Texture2D atlas, Dictionary<int, Vector4> uvByNumber) Bake(BoardData board, TMP_FontAsset font)
    {
        // This is a Dynamic-atlas-population font, so digits it hasn't been
        // asked to render yet don't have glyph data until generated. Force
        // every digit to exist up front rather than relying on whatever
        // happens to already be cached - a fresh checkout (or a cleared
        // cache) would otherwise bake a blank atlas with no visible numbers.
        font.TryAddCharacters("0123456789");

        // A dark outline keeps numbers legible once cells sit on top of
        // bright/varied revealed colors instead of just the flat gray mask.
        if (font.material != null)
        {
            font.material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            font.material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
            font.material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        }

        var numbers = new List<int>(board.NumberToCellIndices.Keys);
        numbers.Sort();

        int cols = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(numbers.Count)));
        int rows = Mathf.Max(1, Mathf.CeilToInt(numbers.Count / (float)cols));

        int texWidth = cols * CellPixelSize;
        int texHeight = rows * CellPixelSize;

        var renderTexture = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGB32)
        {
            name = "NumberAtlas_Bake",
            depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm,
        };

        var cameraGO = new GameObject("~NumberAtlasBakeCamera") { hideFlags = HideFlags.HideAndDontSave };
        Camera bakeCamera = cameraGO.AddComponent<Camera>();
        bakeCamera.orthographic = true;
        bakeCamera.orthographicSize = rows * 0.5f;
        bakeCamera.aspect = (float)cols / rows;
        bakeCamera.clearFlags = CameraClearFlags.SolidColor;
        bakeCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        bakeCamera.targetTexture = renderTexture;
        bakeCamera.nearClipPlane = 0.1f;
        bakeCamera.farClipPlane = 20f;
        cameraGO.transform.SetPositionAndRotation(
            new Vector3(cols * 0.5f, 10f, rows * 0.5f),
            Quaternion.Euler(90f, 0f, 0f));

        var textParent = new GameObject("~NumberAtlasBakeText") { hideFlags = HideFlags.HideAndDontSave };
        var allTexts = new List<TextMeshPro>(numbers.Count);
        var uvByNumber = new Dictionary<int, Vector4>(numbers.Count);

        float cellDu = 1f / cols;
        float cellDv = 1f / rows;

        for (int i = 0; i < numbers.Count; i++)
        {
            int number = numbers[i];
            int col = i % cols;
            int row = i / cols;

            var go = new GameObject($"num_{number}");
            go.transform.SetParent(textParent.transform);
            go.transform.SetPositionAndRotation(
                new Vector3(col + 0.5f, 0f, row + 0.5f),
                Quaternion.Euler(90f, 0f, 0f));

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = font;
            tmp.text = number.ToString();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 1f;
            tmp.fontSizeMax = 20f;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(0.9f, 0.9f);
            allTexts.Add(tmp);

            uvByNumber[number] = new Vector4(col * cellDu, row * cellDv, cellDu, cellDv);
        }

        // Two passes: the first pass is what triggers the SDF font asset to
        // generate glyph data for any digit seen for the first time: a mesh
        // built in that same pass can end up referencing that glyph's atlas
        // UVs before they've actually settled, which is what produced a
        // garbled character here. Re-running ForceMeshUpdate once every
        // glyph is already resident fixes it.
        foreach (TextMeshPro tmp in allTexts)
            tmp.ForceMeshUpdate();
        foreach (TextMeshPro tmp in allTexts)
            tmp.ForceMeshUpdate();

        bakeCamera.Render();

        var atlas = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false)
        {
            name = "NumberAtlas",
        };
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = renderTexture;
        atlas.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
        atlas.Apply();
        RenderTexture.active = previousActive;

        Object.Destroy(cameraGO);
        Object.Destroy(textParent);
        renderTexture.Release();
        Object.Destroy(renderTexture);

        return (atlas, uvByNumber);
    }
}
