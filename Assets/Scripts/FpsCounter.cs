using UnityEngine;

// Minimal on-screen FPS counter for judging performance while testing at
// scale (Task 5) - smoothed over a short window so it's readable instead of
// jittering every frame.
public class FpsCounter : MonoBehaviour
{
    const float SampleWindow = 0.5f;

    float _timer;
    int _frames;
    float _fps;
    GUIStyle _style;

    void Update()
    {
        _timer += Time.unscaledDeltaTime;
        _frames++;

        if (_timer >= SampleWindow)
        {
            _fps = _frames / _timer;
            _timer = 0f;
            _frames = 0;
        }
    }

    void OnGUI()
    {
        _style ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            normal = { textColor = _fps >= 50f ? Color.green : (_fps >= 30f ? Color.yellow : Color.red) },
        };
        _style.normal.textColor = _fps >= 50f ? Color.green : (_fps >= 30f ? Color.yellow : Color.red);

        string gridLabel = LevelSettings.Instance != null
            ? $"{LevelSettings.Instance.GridWidth}x{LevelSettings.Instance.GridHeight}"
            : "?";
        GUI.Label(new Rect(20, 20, 400, 60), $"{_fps:F0} FPS   {gridLabel} grid", _style);
    }
}
