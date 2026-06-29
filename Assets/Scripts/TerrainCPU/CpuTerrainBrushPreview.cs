using UnityEngine;

public class CpuTerrainBrushPreview {
    private GameObject previewObject;
    private MeshFilter previewFilter;
    private MeshRenderer previewRenderer;
    private Mesh spherePreviewMesh;
    private Mesh diskPreviewMesh;

    public void Initialize(Color previewColor) {
        previewObject = new GameObject("CPU Terrain Brush Preview");

        previewFilter = previewObject.AddComponent<MeshFilter>();
        previewRenderer = previewObject.AddComponent<MeshRenderer>();

        spherePreviewMesh = CreateSpherePreviewMesh();
        diskPreviewMesh = CreateDiskPreviewMesh(64);

        previewFilter.sharedMesh = spherePreviewMesh;

        Material previewMaterial = CreatePreviewMaterial(previewColor);
        previewRenderer.material = previewMaterial;

        previewObject.SetActive(false);
    }

    public void UpdatePreview(bool shouldShow, Vector3 brushHitPoint, float editRadius, CpuTerrainBrushType brushType, Color previewColor) {
        if (previewObject == null) {
            return;
        }

        previewObject.SetActive(shouldShow);

        if (!shouldShow) {
            return;
        }

        UpdatePreviewMesh(brushType);

        previewObject.transform.position = brushHitPoint;
        previewObject.transform.rotation = Quaternion.identity;

        if (brushType == CpuTerrainBrushType.Flatten) {
            previewObject.transform.localScale = Vector3.one * editRadius;
        }
        else {
            previewObject.transform.localScale = Vector3.one * editRadius * 2f;
        }

        if (previewRenderer != null) {
            SetPreviewMaterialColor(previewRenderer.material, previewColor);
        }
    }

    public void Hide() {
        if (previewObject != null) {
            previewObject.SetActive(false);
        }
    }

    public void Dispose() {
        if (previewObject != null) {
            Object.Destroy(previewObject);
            previewObject = null;
        }

        if (spherePreviewMesh != null) {
            Object.Destroy(spherePreviewMesh);
            spherePreviewMesh = null;
        }

        if (diskPreviewMesh != null) {
            Object.Destroy(diskPreviewMesh);
            diskPreviewMesh = null;
        }
    }

    private void UpdatePreviewMesh(CpuTerrainBrushType brushType) {
        if (previewFilter == null) {
            return;
        }

        Mesh targetMesh = brushType == CpuTerrainBrushType.Flatten ? diskPreviewMesh : spherePreviewMesh;

        if (previewFilter.sharedMesh != targetMesh) {
            previewFilter.sharedMesh = targetMesh;
        }
    }

    private Material CreatePreviewMaterial(Color previewColor) {
        Shader previewShader = Shader.Find("Universal Render Pipeline/Unlit");

        if (previewShader == null) {
            previewShader = Shader.Find("Unlit/Transparent");
        }

        if (previewShader == null) {
            previewShader = Shader.Find("Sprites/Default");
        }

        Material previewMaterial = new Material(previewShader);
        SetPreviewMaterialColor(previewMaterial, previewColor);

        previewMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
        previewMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        previewMaterial.SetInt("_ZWrite", 0);
        previewMaterial.renderQueue = 3000;

        if (previewMaterial.HasProperty("_Surface")) {
            previewMaterial.SetFloat("_Surface", 1f);
        }

        if (previewMaterial.HasProperty("_Cull")) {
            previewMaterial.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
        }

        return previewMaterial;
    }

    private Mesh CreateSpherePreviewMesh() {
        GameObject temporarySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh sourceMesh = temporarySphere.GetComponent<MeshFilter>().sharedMesh;
        Mesh previewMesh = Object.Instantiate(sourceMesh);
        previewMesh.name = "CPU Terrain Brush Sphere Preview Mesh";
        Object.Destroy(temporarySphere);
        return previewMesh;
    }

    private Mesh CreateDiskPreviewMesh(int segmentCount) {
        segmentCount = Mathf.Max(8, segmentCount);

        Vector3[] vertices = new Vector3[segmentCount + 1];
        Vector3[] normals = new Vector3[segmentCount + 1];
        Vector2[] uvs = new Vector2[segmentCount + 1];
        int[] triangles = new int[segmentCount * 6];

        vertices[0] = Vector3.zero;
        normals[0] = Vector3.up;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segmentCount; i++) {
            float angle = (float) i / segmentCount * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            vertices[i + 1] = new Vector3(x, 0f, z);
            normals[i + 1] = Vector3.up;
            uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);
        }

        int triangleIndex = 0;

        for (int i = 0; i < segmentCount; i++) {
            int current = i + 1;
            int next = i == segmentCount - 1 ? 1 : i + 2;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = current;
            triangles[triangleIndex++] = next;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = current;
        }

        Mesh mesh = new Mesh();
        mesh.name = "CPU Terrain Brush Disk Preview Mesh";
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }

    private void SetPreviewMaterialColor(Material material, Color color) {
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