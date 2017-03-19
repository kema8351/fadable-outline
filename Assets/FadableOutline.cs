using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FadableOutline : BaseMeshEffect
{
    static List<UIVertex> tempVertices = new List<UIVertex>();

    [SerializeField]
    float _distance = 4f;
    public float Distance
    {
        get { return _distance; }
        set { _distance = value; this.graphic.SetVerticesDirty(); }
    }

    [SerializeField, Range(3, 100)]
    int _outlineCount = 8;
    public int OutlineCount
    {
        get { return _outlineCount; }
        set { _outlineCount = value; this.graphic.SetVerticesDirty(); }
    }

    [SerializeField, Range(2, 10)]
    int _alphaCount = 2;
    public int AlphaCount
    {
        get { return _alphaCount; }
        set { _alphaCount = value; this.graphic.SetVerticesDirty(); }
    }

    [SerializeField]
    Color32 _color = new Color32(255, 255, 255, 255);
    public Color32 Color
    {
        get { return _color; }
        set { _color = value; this.graphic.SetVerticesDirty(); }
    }

    float? cacheDistance = null;
    List<Vector3> cacheDifferences = new List<Vector3>();
    CanvasGroup[] parentCanvasGroups;
    Dictionary<CanvasGroup, float> canvasGroupAlphas = new Dictionary<CanvasGroup, float>();
    float cacheTotalCanvasGroupAlpha = 1f;

    protected override void OnEnable()
    {
        parentCanvasGroups = GetComponentsInParent<CanvasGroup>();
        CacheCanvasGroupAlphas();
    }

    private void Update()
    {
        if (parentCanvasGroups == null)
            return;

        if (parentCanvasGroups.Length != canvasGroupAlphas.Count ||
            parentCanvasGroups.Any(canvasGroup => canvasGroupAlphas[canvasGroup] != canvasGroup.alpha))
        {
            CacheCanvasGroupAlphas();
            this.graphic.SetVerticesDirty();
        }
    }

    void CacheCanvasGroupAlphas()
    {
        cacheTotalCanvasGroupAlpha = 1f;
        canvasGroupAlphas.Clear();

        foreach (var canvasGroup in parentCanvasGroups)
        {
            var alpha = canvasGroup.alpha;
            canvasGroupAlphas.Add(canvasGroup, alpha);
            cacheTotalCanvasGroupAlpha *= alpha;
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!this.IsActive())
            return;

        if (!cacheDistance.HasValue ||
            cacheDistance.Value != this.Distance ||
            cacheDifferences.Count != this.OutlineCount)
        {
            cacheDifferences.Clear();
            for (int i = 0; i < this.OutlineCount; i++)
            {
                float radian = Mathf.PI * (float)(1 + i * 2) / (float)this.OutlineCount;
                cacheDifferences.Add(new Vector3(
                    Mathf.Cos(radian) * this.Distance,
                    Mathf.Sin(radian) * this.Distance,
                    0f
                ));
            }
        }

        vh.GetUIVertexStream(tempVertices);
        int originalVertexCount = tempVertices.Count;
        Color32 outlineColor = GetOutlinedColor();

        for (int i = 1; i < cacheDifferences.Count; i++)
        {
            var difference = cacheDifferences[i];

            for (int j = 0; j < originalVertexCount; j++)
            {
                var vertex = tempVertices[j];
                vertex.position += difference;
                vertex.color = outlineColor;
                tempVertices.Add(vertex);
            }
        }

        var difference0 = cacheDifferences[0];
        for (int i = 0; i < originalVertexCount; i++)
        {
            var vertex = tempVertices[i];
            tempVertices.Add(vertex);

            vertex.position += difference0;
            vertex.color = outlineColor;
            tempVertices[i] = vertex;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(tempVertices);
    }

    Color32 GetOutlinedColor()
    {
        var outlineColor = this.Color;

        var outlineAlpha = (float)(outlineColor.a) / 255f;
        var textAlpha = (float)graphic.color.a;
        var canvasAlpha = cacheTotalCanvasGroupAlpha;
        var totalAlpha = outlineAlpha * textAlpha * canvasAlpha;

        var resultAlpha = 1f;
        for (int i = 0; i < this.AlphaCount; i++)
            resultAlpha *= totalAlpha;

        return new Color32(
            outlineColor.r,
            outlineColor.g,
            outlineColor.b,
            (Byte)Mathf.CeilToInt(resultAlpha * 255f)
        );
    }
}
