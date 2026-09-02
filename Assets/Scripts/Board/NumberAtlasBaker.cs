using TMPro;
using UnityEngine;

// One-time bake of every cell's number into a single shared texture, so the
// GPU-instanced board can show numbers by sampling one texture instead of
// needing a per-instance digit-atlas lookup or per-cell TextMeshPro objects.
// Spawns TMP text for the whole grid, renders it once through a temporary
// orthographic camera, then tears everything down immediately.
public static class NumberAtlasBaker
{
    const int CellPixelSize = 64;
    const int MaxTextureSize = 4096;

    public static Texture2D Bake(BoardData board, TMP_FontAsset font)
    {
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
            tmp.ForceMeshUpdate();
        }

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
