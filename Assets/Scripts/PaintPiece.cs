using System.Collections;
using TMPro;
using UnityEngine;

public class PaintPiece : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] TextMeshPro numberText;
    [SerializeField] Renderer meshRenderer;

    public int Number { get; private set; }
    public Vector3 TrayPosition { get; set; }

    Collider _collider;
    Color _baseColor;
    Coroutine _feedbackRoutine;

    void Awake()
    {
        _collider = GetComponentInChildren<Collider>();
    }

    public void Init(Color color, int colorNumber)
    {
        Number = colorNumber;
        _baseColor = color;
        SetColor(color);
        numberText.text = colorNumber.ToString();
    }

    private void SetColor(Color color)
    {
        Material mat = meshRenderer.material;
        mat.color = new Color(color.r, color.g, color.b, 1);
        // Glossy "premium tile" look: a real specular highlight from the
        // scene light instead of a flat matte color.
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.75f);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0.15f);
        spriteRenderer.color = new Color(color.r, color.g, color.b, 0.5f);
    }

    public void SetTrayVisible()
    {
        meshRenderer.enabled = true;
        spriteRenderer.enabled = true;
        numberText.enabled = true;
    }

    public void SetColliderEnabled(bool value)
    {
        if (_collider != null)
            _collider.enabled = value;
    }

    public void PlayRejectAndReturn()
    {
        if (_feedbackRoutine != null)
            StopCoroutine(_feedbackRoutine);
        _feedbackRoutine = StartCoroutine(RejectAndReturnRoutine());
    }

    // A correct drop already changed the game/board state (TryPlacePiece
    // already ran) - this just delays this tray object's own destruction
    // until it visually snaps into the socket and does a small landing
    // pop, instead of vanishing the instant it's released.
    public void PlayCorrectPlacement(Vector3 targetPosition)
    {
        if (_feedbackRoutine != null)
            StopCoroutine(_feedbackRoutine);
        _feedbackRoutine = StartCoroutine(CorrectPlacementRoutine(targetPosition));
    }

    IEnumerator CorrectPlacementRoutine(Vector3 targetPosition)
    {
        Vector3 baseScale = transform.localScale;

        const float snapDuration = 0.16f;
        Vector3 from = transform.position;
        float t = 0f;
        while (t < snapDuration)
        {
            t += Time.deltaTime;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / snapDuration), 3f);
            transform.position = Vector3.Lerp(from, targetPosition, eased);
            yield return null;
        }
        transform.position = targetPosition;

        // TAK: quick overshoot up, then a slight compress - the "impact"
        // moment before the cube cracks apart.
        const float popUpDuration = 0.07f;
        t = 0f;
        while (t < popUpDuration)
        {
            t += Time.deltaTime;
            transform.localScale = baseScale * Mathf.Lerp(1f, 1.12f, t / popUpDuration);
            yield return null;
        }

        const float popDownDuration = 0.06f;
        t = 0f;
        while (t < popDownDuration)
        {
            t += Time.deltaTime;
            transform.localScale = baseScale * Mathf.Lerp(1.12f, 0.94f, t / popDownDuration);
            yield return null;
        }

        // Crack: the cube itself disappears and scatters into a few small
        // tinted fragments instead of just vanishing - the socket stays in
        // its neutral "filled" state (see BoardInstanced.shader's _Filled
        // handling), no true color leaks here.
        meshRenderer.enabled = false;
        spriteRenderer.enabled = false;
        numberText.enabled = false;
        CrackFragments.Spawn(targetPosition, _baseColor, baseScale.x);

        Destroy(gameObject);
    }

    IEnumerator RejectAndReturnRoutine()
    {
        Vector3 startPos = transform.position;
        Color rejectColor = Color.red;

        const float shakeDuration = 0.25f;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float shakeAmount = Mathf.Sin(t * 40f) * 0.08f * (1f - t / shakeDuration);
            transform.position = startPos + new Vector3(shakeAmount, 0f, 0f);
            SetColor(Color.Lerp(_baseColor, rejectColor, 0.6f * (1f - t / shakeDuration)));
            yield return null;
        }

        transform.position = startPos;
        SetColor(_baseColor);

        const float returnDuration = 0.2f;
        t = 0f;
        Vector3 from = transform.position;
        while (t < returnDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, TrayPosition, t / returnDuration);
            yield return null;
        }

        transform.position = TrayPosition;
        SetColliderEnabled(true);
        _feedbackRoutine = null;
    }
}
