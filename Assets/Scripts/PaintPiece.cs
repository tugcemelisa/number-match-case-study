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
        meshRenderer.material.color = new Color(color.r, color.g, color.b, 1);
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
