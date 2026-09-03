using UnityEngine;

// Runtime-generated rounded-rectangle sprite for UI panels, so the
// cosmetic HUD/popup elements don't need external art assets. Built once
// and cached; the sprite's border makes it safe to use with Image.Type.
// Sliced so it stretches cleanly to any panel size.
public static class UISprites
{
    static Sprite _cached;

    public static Sprite RoundedRect()
    {
        if (_cached != null)
            return _cached;

        const int size = 64;
        const float radius = 22f;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "RoundedRect",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float dx = Mathf.Max(Mathf.Abs(px - size * 0.5f) - (size * 0.5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(py - size * 0.5f) - (size * 0.5f - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                float alpha = Mathf.Clamp01(0.5f - dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        _cached = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        _cached.name = "RoundedRectSprite";
        return _cached;
    }
}
