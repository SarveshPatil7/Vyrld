using UnityEngine;

public class TerrainChunkBoxVisual {
    private const int EdgeCount = 12;

    private readonly GameObject rootObject;
    private readonly LineRenderer[] edges = new LineRenderer[EdgeCount];
    private readonly Material material;

    public TerrainChunkBoxVisual(string name, Color color, float lineWidth) {
        rootObject = new GameObject(name);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null) {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null) {
            shader = Shader.Find("Sprites/Default");
        }

        material = new Material(shader);
        SetMaterialColor(color);

        for (int i = 0; i < EdgeCount; i++) {
            GameObject edgeObject = new GameObject($"Edge_{i}");
            edgeObject.transform.SetParent(rootObject.transform);

            LineRenderer lineRenderer = edgeObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.material = material;

            edges[i] = lineRenderer;
        }

        SetVisible(false);
    }

    public void SetBounds(Bounds bounds) {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3 p000 = new Vector3(min.x, min.y, min.z);
        Vector3 p100 = new Vector3(max.x, min.y, min.z);
        Vector3 p110 = new Vector3(max.x, min.y, max.z);
        Vector3 p010 = new Vector3(min.x, min.y, max.z);

        Vector3 p001 = new Vector3(min.x, max.y, min.z);
        Vector3 p101 = new Vector3(max.x, max.y, min.z);
        Vector3 p111 = new Vector3(max.x, max.y, max.z);
        Vector3 p011 = new Vector3(min.x, max.y, max.z);

        SetEdge(0, p000, p100);
        SetEdge(1, p100, p110);
        SetEdge(2, p110, p010);
        SetEdge(3, p010, p000);

        SetEdge(4, p001, p101);
        SetEdge(5, p101, p111);
        SetEdge(6, p111, p011);
        SetEdge(7, p011, p001);

        SetEdge(8, p000, p001);
        SetEdge(9, p100, p101);
        SetEdge(10, p110, p111);
        SetEdge(11, p010, p011);
    }

    public void SetColor(Color color) {
        SetMaterialColor(color);
    }

    public void SetLineWidth(float lineWidth) {
        for (int i = 0; i < edges.Length; i++) {
            if (edges[i] != null) {
                edges[i].widthMultiplier = lineWidth;
            }
        }
    }

    public void SetVisible(bool visible) {
        if (rootObject != null) {
            rootObject.SetActive(visible);
        }
    }

    public void Dispose() {
        if (rootObject != null) {
            Object.Destroy(rootObject);
        }

        if (material != null) {
            Object.Destroy(material);
        }
    }

    private void SetEdge(int edgeIndex, Vector3 start, Vector3 end) {
        LineRenderer edge = edges[edgeIndex];

        if (edge == null) {
            return;
        }

        edge.SetPosition(0, start);
        edge.SetPosition(1, end);
    }

    private void SetMaterialColor(Color color) {
        if (material == null) {
            return;
        }

        if (material.HasProperty("_BaseColor")) {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color")) {
            material.SetColor("_Color", color);
        }

        material.color = color;
    }
}