using TMPro;
using UnityEngine;

public class PaintPiece : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] TextMeshPro numberText;
    [SerializeField] Renderer meshRenderer;

    public void Init(Color color, int colorNumber)
    {
        SetColor(color);
        numberText.text = colorNumber.ToString();
    }

    private void SetColor(Color color)
    {
        meshRenderer.material.color = new Color(color.r, color.g, color.b, 1);
        spriteRenderer.color = new Color(color.r, color.g, color.b, 0.5f);
    }

    public void SetVisible(bool visible)
    {
        meshRenderer.enabled = visible;
        numberText.enabled = !visible;
    }

    public void SetTrayVisible()
    {
        meshRenderer.enabled = true;
        numberText.enabled = true;
    }
}