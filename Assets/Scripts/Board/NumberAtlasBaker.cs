using System.Collections.Generic;
using TMPro;
using UnityEngine;

// One-time bake of every cell's number into a single shared texture, so the
// GPU-instanced board can show numbers by sampling one texture instead of
// needing a per-instance digit-atlas lookup or per-cell TextMeshPro objects.
// Spawns TMP text for the whole grid, renders it once through a temporary
// orthographic camera, then tears everything down immediately.
public static class NumberAtlasBaker
{
    const int CellPixelSize = 96;
    const int MaxTextureSize = 4096;

    public static Texture2D Bake(BoardData board, TMP_FontAsset font)
    {
        // A dark outline keeps numbers legible once cells sit on top of
        // bright/varied revealed colors instead of just the flat gray mask.
        // Set once on the font asset's shared material so both the baked
        // board numbers and the tray's live TMP text pick it up.
        if (font.material != null)
        {
            font.material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            font.material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
            font.material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        }

        int texWidth = Mathf.Clamp(board.Width * CellPixelSize, CellPixelSize, MaxTextureSize);
        int texHeight = Mathf.Clamp(board.Height * CellPixelSize, CellPixelSize, MaxTextureSize);

        var renderTexture = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGB32)
        {
            name = "NumberAtlas_Bake",
            depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm,
        };

        var cameraGO = new GameObject("~NumberAtlasBakeCamera") { hideFlags = HideFlags.HideAndDontSave };
        Camera bakeCamera = cameraGO.AddComponent<Camera>();
        bakeCamera.orthographic = true;
        bakeCamera.orthographicSize = board.Height * 0.5f;
        bakeCamera.aspect = (float)board.Width / board.Height;
        bakeCamera.clearFlags = CameraClearFlags.SolidColor;
        bakeCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        bakeCamera.targetTexture = renderTexture;
        bakeCamera.nearClipPlane = 0.1f;
        bakeCamera.farClipPlane = 20f;
        cameraGO.transform.SetPositionAndRotation(
            new Vector3(board.Width * 0.5f, 10f, board.Height * 0.5f),
            Quaternion.Euler(90f, 0f, 0f));

        var textParent = new GameObject("~NumberAtlasBakeText") { hideFlags = HideFlags.HideAndDontSave };
        var allTexts = new List<TextMeshPro>(board.Cells.Length);

        foreach (Cell cell in board.Cells)
        {
            var go = new GameObject($"num_{cell.x}_{cell.z}");
            go.transform.SetParent(textParent.transform);
            go.transform.SetPositionAndRotation(
                new Vector3(cell.x + 0.5f, 0f, cell.z + 0.5f),
                Quaternion.Euler(90f, 0f, 0f));

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = font;
            tmp.text = cell.number.ToString();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 1f;
            tmp.fontSizeMax = 20f;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(0.9f, 0.9f);
            allTexts.Add(tmp);
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

        return atlas;
    }
}
