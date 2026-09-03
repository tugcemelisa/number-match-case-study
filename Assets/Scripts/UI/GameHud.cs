using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Everything screen-space lives on one code-built Canvas: a cosmetic top
// bar (menu/coins/level/timer/pause - static dummy values, no
// functionality, purely to match the reference composition), a center
// "group complete" success popup, and a small combo/streak counter.
// Built entirely at runtime so there's no per-project scene wiring to
// maintain - SceneBootstrap just has to ensure one GameObject with this
// component exists.
public class GameHud : MonoBehaviour
{
    public static GameHud Instance { get; private set; }

    static readonly Color PanelColor = new Color(0.16f, 0.12f, 0.27f, 0.95f);
    static readonly Color PanelHighlight = new Color(0.42f, 0.34f, 0.6f, 1f);
    static readonly Color GoldAccent = new Color(1f, 0.82f, 0.35f, 1f);
    static readonly Color TextColor = Color.white;

    TMP_FontAsset _font;
    RectTransform _popupRoot;
    TextMeshProUGUI _popupText;
    CanvasGroup _popupGroup;
    Coroutine _popupRoutine;

    RectTransform _comboRoot;
    TextMeshProUGUI _comboText;
    CanvasGroup _comboGroup;
    Coroutine _comboRoutine;
    int _comboCount;

    void Awake()
    {
        Instance = this;
        _font = Resources.Load<TMP_FontAsset>("Bangers SDF");

        Canvas canvas = BuildCanvas();
        BuildTopHud(canvas.transform);
        BuildCenterPopup(canvas.transform);
        BuildCombo(canvas.transform);
    }

    Canvas BuildCanvas()
    {
        var go = new GameObject("Hud Canvas", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>().enabled = false;

        return canvas;
    }

    // ---------- Top HUD (cosmetic only) ----------

    void BuildTopHud(Transform parent)
    {
        var bar = new GameObject("Top Hud", typeof(RectTransform));
        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.SetParent(parent, false);
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.sizeDelta = new Vector2(0f, 170f);
        barRect.anchoredPosition = new Vector2(0f, -90f);

        CreateMenuIcon(barRect, new Vector2(-460f, 0f), 100f);
        CreatePill(barRect, "Score Pill", new Vector2(-260f, 0f), 190f, 84f, "8320", GoldAccent);
        CreateLevelBadge(barRect, new Vector2(0f, 0f));
        CreatePill(barRect, "Timer Pill", new Vector2(275f, 0f), 170f, 84f, "02:45", new Color(0.82f, 0.88f, 1f, 1f));
        CreatePauseIcon(barRect, new Vector2(460f, 0f), 100f);
    }

    // Geometric icons instead of text glyphs - emoji/symbol characters
    // aren't guaranteed to exist in the Bangers SDF font (or any
    // fallback), so glyph-based icons render as tofu boxes. Plain
    // rectangles sidestep font coverage entirely.
    void CreateMenuIcon(RectTransform parent, Vector2 anchoredPos, float size)
    {
        RectTransform panel = CreatePanel("Menu Button", parent, PanelColor);
        panel.sizeDelta = new Vector2(size, size);
        panel.anchoredPosition = anchoredPos;
        AddInnerBorder(panel);

        for (int i = 0; i < 3; i++)
        {
            RectTransform bar = CreatePanel("Bar", panel, TextColor);
            bar.sizeDelta = new Vector2(size * 0.5f, size * 0.08f);
            bar.anchoredPosition = new Vector2(0f, (1 - i) * size * 0.2f);
        }
    }

    void CreatePauseIcon(RectTransform parent, Vector2 anchoredPos, float size)
    {
        RectTransform panel = CreatePanel("Pause Button", parent, PanelColor);
        panel.sizeDelta = new Vector2(size, size);
        panel.anchoredPosition = anchoredPos;
        AddInnerBorder(panel);

        for (int i = 0; i < 2; i++)
        {
            RectTransform bar = CreatePanel("Bar", panel, TextColor);
            bar.sizeDelta = new Vector2(size * 0.13f, size * 0.4f);
            bar.anchoredPosition = new Vector2((i == 0 ? -1f : 1f) * size * 0.13f, 0f);
        }
    }

    void CreatePill(RectTransform parent, string name, Vector2 anchoredPos, float width, float height, string value, Color iconColor)
    {
        RectTransform panel = CreatePanel(name, parent, PanelColor);
        panel.sizeDelta = new Vector2(width, height);
        panel.anchoredPosition = anchoredPos;
        AddInnerBorder(panel);

        RectTransform icon = CreatePanel("Icon", panel, iconColor);
        icon.sizeDelta = new Vector2(height * 0.5f, height * 0.5f);
        icon.anchorMin = new Vector2(0f, 0.5f);
        icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0f, 0.5f);
        icon.anchoredPosition = new Vector2(16f, 0f);

        TextMeshProUGUI valueText = CreateText(panel, value, height * 0.42f, TextColor);
        valueText.rectTransform.anchorMin = new Vector2(0f, 0f);
        valueText.rectTransform.anchorMax = new Vector2(1f, 1f);
        valueText.rectTransform.offsetMin = new Vector2(height * 0.85f, 0f);
        valueText.rectTransform.offsetMax = new Vector2(-10f, 0f);
        valueText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    void CreateLevelBadge(RectTransform parent, Vector2 anchoredPos)
    {
        RectTransform panel = CreatePanel("Level Badge", parent, PanelColor);
        panel.sizeDelta = new Vector2(160f, 130f);
        panel.anchoredPosition = anchoredPos;

        Image border = panel.GetComponent<Image>();
        border.color = new Color(PanelColor.r, PanelColor.g, PanelColor.b, 1f);

        RectTransform gold = CreatePanel("Gold Rim", panel, new Color(0f, 0f, 0f, 0f));
        StretchFull(gold);
        Outline outline = gold.gameObject.AddComponent<Outline>();
        outline.effectColor = GoldAccent;
        outline.effectDistance = new Vector2(2f, 2f);

        TextMeshProUGUI label = CreateText(panel, "LEVEL", 26f, new Color(0.85f, 0.8f, 1f, 0.9f));
        label.rectTransform.anchorMin = new Vector2(0f, 0.58f);
        label.rectTransform.anchorMax = new Vector2(1f, 1f);
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI number = CreateText(panel, "42", 52f, TextColor);
        number.fontStyle = FontStyles.Bold;
        number.rectTransform.anchorMin = new Vector2(0f, 0f);
        number.rectTransform.anchorMax = new Vector2(1f, 0.6f);
        number.rectTransform.offsetMin = Vector2.zero;
        number.rectTransform.offsetMax = Vector2.zero;
    }

    void AddInnerBorder(RectTransform panel)
    {
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = PanelHighlight;
        outline.effectDistance = new Vector2(0f, 1.5f);
    }

    // ---------- Center success popup ----------

    void BuildCenterPopup(Transform parent)
    {
        RectTransform root = CreatePanel("Success Popup", parent, new Color(0.14f, 0.1f, 0.24f, 0.96f));
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(420f, 220f);
        root.anchoredPosition = Vector2.zero;
        AddInnerBorder(root);

        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        root.gameObject.SetActive(false);

        // A plain rounded badge instead of a star glyph - symbol
        // characters aren't guaranteed to exist in the Bangers SDF font.
        RectTransform icon = CreatePanel("Icon", root, GoldAccent);
        icon.anchorMin = new Vector2(0.5f, 0.42f);
        icon.anchorMax = new Vector2(0.5f, 1f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.sizeDelta = new Vector2(76f, 76f);
        icon.anchoredPosition = new Vector2(0f, -6f);

        TextMeshProUGUI label = CreateText(root, "Great!", 54f, TextColor);
        label.font = _font;
        label.fontStyle = FontStyles.Bold;
        label.rectTransform.anchorMin = new Vector2(0f, 0f);
        label.rectTransform.anchorMax = new Vector2(1f, 0.42f);
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;

        _popupRoot = root;
        _popupText = label;
        _popupGroup = group;
    }

    static readonly string[] SuccessWords = { "Great!", "Nice!", "Perfect!", "Awesome!" };

    public void PlayGroupSuccess()
    {
        _popupText.text = SuccessWords[Random.Range(0, SuccessWords.Length)];
        if (_popupRoutine != null)
            StopCoroutine(_popupRoutine);
        _popupRoutine = StartCoroutine(PopupRoutine(1f));
    }

    public void PlayBoardComplete()
    {
        _popupText.text = "Completed!";
        if (_popupRoutine != null)
            StopCoroutine(_popupRoutine);
        _popupRoutine = StartCoroutine(PopupRoutine(1.4f));
    }

    IEnumerator PopupRoutine(float scalePunch)
    {
        _popupRoot.gameObject.SetActive(true);
        _popupGroup.alpha = 1f;

        const float inDuration = 0.18f;
        float t = 0f;
        while (t < inDuration)
        {
            t += Time.deltaTime;
            float t01 = t / inDuration;
            float scale = Mathf.Lerp(0.4f, scalePunch, EaseOutBack(t01));
            _popupRoot.localScale = Vector3.one * scale;
            yield return null;
        }
        _popupRoot.localScale = Vector3.one * scalePunch;

        const float settleDuration = 0.08f;
        t = 0f;
        while (t < settleDuration)
        {
            t += Time.deltaTime;
            _popupRoot.localScale = Vector3.one * Mathf.Lerp(scalePunch, 1f, t / settleDuration);
            yield return null;
        }
        _popupRoot.localScale = Vector3.one;

        yield return new WaitForSeconds(0.55f);

        const float fadeDuration = 0.25f;
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float t01 = t / fadeDuration;
            _popupGroup.alpha = 1f - t01;
            _popupRoot.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, t01);
            yield return null;
        }

        _popupRoot.gameObject.SetActive(false);
        _popupRoutine = null;
    }

    // ---------- Combo counter ----------

    void BuildCombo(Transform parent)
    {
        RectTransform root = CreatePanel("Combo Badge", parent, new Color(0.16f, 0.12f, 0.27f, 0.92f));
        root.anchorMin = root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(1f, 1f);
        root.sizeDelta = new Vector2(150f, 66f);
        root.anchoredPosition = new Vector2(-40f, -260f);
        AddInnerBorder(root);

        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        TextMeshProUGUI text = CreateText(root, "x2", 34f, GoldAccent);
        text.fontStyle = FontStyles.Bold;
        StretchFull(text.rectTransform);

        _comboRoot = root;
        _comboText = text;
        _comboGroup = group;
    }

    public void RegisterCorrectPlacement()
    {
        _comboCount++;
        if (_comboCount < 2)
            return;

        _comboText.text = $"x{_comboCount}";
        if (_comboRoutine != null)
            StopCoroutine(_comboRoutine);
        _comboRoutine = StartCoroutine(ComboPulseRoutine());
    }

    public void RegisterWrongPlacement()
    {
        _comboCount = 0;
        if (_comboRoutine != null)
        {
            StopCoroutine(_comboRoutine);
            _comboRoutine = null;
        }
        _comboGroup.alpha = 0f;
    }

    IEnumerator ComboPulseRoutine()
    {
        _comboGroup.alpha = 1f;

        const float punchDuration = 0.14f;
        float t = 0f;
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            _comboRoot.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, EaseOutBack(t / punchDuration));
            yield return null;
        }
        _comboRoot.localScale = Vector3.one;

        yield return new WaitForSeconds(1.6f);

        const float fadeDuration = 0.3f;
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            _comboGroup.alpha = 1f - t / fadeDuration;
            yield return null;
        }
        _comboRoutine = null;
    }

    // ---------- Shared helpers ----------

    RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);

        Image image = go.GetComponent<Image>();
        image.sprite = UISprites.RoundedRect();
        image.type = Image.Type.Sliced;
        image.color = color;
        return rect;
    }

    TextMeshProUGUI CreateText(RectTransform parent, string text, float fontSize, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (_font != null)
            tmp.font = _font;
        return tmp;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float t1 = t - 1f;
        return 1f + c3 * t1 * t1 * t1 + c1 * t1 * t1;
    }
}
