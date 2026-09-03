using TMPro;
using UnityEngine;

// Self-contained "you did it" popup: pops in with a small overshoot, holds
// briefly, then fades out and destroys itself. Spawned fresh per group
// completion (not pooled/reused) so overlapping reveals at 64x64 don't
// fight over shared state - each is a cheap, short-lived GameObject.
public class SuccessPopup : MonoBehaviour
{
    const float PopDuration = 0.25f;
    const float HoldDuration = 0.35f;
    const float FadeDuration = 0.45f;
    const float OvershootScale = 1.25f;
    const float RestScale = 1f;

    static readonly string[] Messages = { "Nice!", "Great!", "Awesome!", "Perfect!" };

    TextMeshPro _text;
    float _t;

    public static void Spawn(TMP_FontAsset font, Vector3 worldPosition, Color color)
    {
        if (font == null)
            return;

        var go = new GameObject("Success Popup");
        go.transform.SetPositionAndRotation(worldPosition + Vector3.up * 0.6f, Quaternion.Euler(90f, 0f, 0f));
        go.transform.localScale = Vector3.zero;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = font;
        tmp.text = Messages[Random.Range(0, Messages.Length)];
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 5f;
        tmp.color = color;
        tmp.outlineColor = Color.black;
        tmp.outlineWidth = 0.25f;
        tmp.rectTransform.sizeDelta = new Vector2(4f, 1.5f);

        var popup = go.AddComponent<SuccessPopup>();
        popup._text = tmp;
    }

    void Update()
    {
        _t += Time.deltaTime;

        float scale;
        if (_t < PopDuration)
        {
            float t01 = _t / PopDuration;
            scale = Mathf.Lerp(0f, OvershootScale, EaseOutBack(t01));
        }
        else if (_t < PopDuration + HoldDuration)
        {
            float t01 = (_t - PopDuration) / HoldDuration;
            scale = Mathf.Lerp(OvershootScale, RestScale, t01);
        }
        else
        {
            scale = RestScale;
            float fadeT01 = Mathf.Clamp01((_t - PopDuration - HoldDuration) / FadeDuration);
            if (_text != null)
            {
                Color c = _text.color;
                c.a = 1f - fadeT01;
                _text.color = c;
            }
        }

        transform.localScale = Vector3.one * scale;

        if (_t >= PopDuration + HoldDuration + FadeDuration)
            Destroy(gameObject);
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float t1 = t - 1f;
        return 1f + c3 * t1 * t1 * t1 + c1 * t1 * t1;
    }
}
