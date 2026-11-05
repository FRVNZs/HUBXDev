using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient Pro")]
public class UIGradient : BaseMeshEffect
{
    public Color color1 = Color.white;
    public Color color2 = Color.black;
    [Range(-180f, 180f)] public float angle = 0f;

    [Header("Настройка перехода")]
    [Range(0f, 1f)] public float offset = 0.5f;   // Смещение центра (влево-вправо)
    [Range(0.01f, 1f)] public float softness = 0.5f; // Ширина размытия (0 - жестко, 1 - плавно)

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        UIVertex vertex = new UIVertex();

        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float dot = Vector2.Dot(vertex.position, dir);
            min = Mathf.Min(min, dot);
            max = Mathf.Max(max, dot);
        }

        float range = max - min;
        if (range <= 0) range = 1f;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float dot = Vector2.Dot(vertex.position, dir);
            float t = (dot - min) / range;

            // Мягкий переход через SmoothStep
            float lowerBound = offset - softness * 0.5f;
            float upperBound = offset + softness * 0.5f;
            float res = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(lowerBound, upperBound, t));

            vertex.color *= Color.Lerp(color1, color2, res);
            vh.SetUIVertex(vertex, i);
        }
    }
}