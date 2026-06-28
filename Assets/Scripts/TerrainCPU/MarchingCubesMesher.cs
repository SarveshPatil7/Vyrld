using System.Collections.Generic;
using UnityEngine;

public static class MarchingCubesMesher {
    private const float SurfaceLevel = 0f;

    public static Mesh GenerateMesh(DensityChunkData data) {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int x = 0; x < data.cellCount; x++) {
            for (int y = 0; y < data.cellCount; y++) {
                for (int z = 0; z < data.cellCount; z++) {
                    MarchCell(data, x, y, z, vertices, triangles);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private static void MarchCell(
        DensityChunkData data,
        int x,
        int y,
        int z,
        List<Vector3> vertices,
        List<int> triangles) {
        float[] cubeDensities = new float[8];
        Vector3[] cubePositions = new Vector3[8];

        for (int i = 0; i < 8; i++) {
            Vector3Int corner = MarchingCubesTables.Corners[i];

            int sx = x + corner.x;
            int sy = y + corner.y;
            int sz = z + corner.z;

            cubeDensities[i] = data.Get(sx, sy, sz);
            cubePositions[i] = data.SampleToLocalPosition(sx, sy, sz);
        }

        int cubeIndex = 0;

        if (cubeDensities[0] < SurfaceLevel) cubeIndex |= 1;
        if (cubeDensities[1] < SurfaceLevel) cubeIndex |= 2;
        if (cubeDensities[2] < SurfaceLevel) cubeIndex |= 4;
        if (cubeDensities[3] < SurfaceLevel) cubeIndex |= 8;
        if (cubeDensities[4] < SurfaceLevel) cubeIndex |= 16;
        if (cubeDensities[5] < SurfaceLevel) cubeIndex |= 32;
        if (cubeDensities[6] < SurfaceLevel) cubeIndex |= 64;
        if (cubeDensities[7] < SurfaceLevel) cubeIndex |= 128;

        int edgeMask = MarchingCubesTables.EdgeTable[cubeIndex];

        if (edgeMask == 0) {
            return;
        }

        Vector3[] edgeVertices = new Vector3[12];

        for (int edge = 0; edge < 12; edge++) {
            if ((edgeMask & (1 << edge)) == 0) {
                continue;
            }

            int a = MarchingCubesTables.Edges[edge, 0];
            int b = MarchingCubesTables.Edges[edge, 1];

            edgeVertices[edge] = InterpolateVertex(
                cubePositions[a],
                cubePositions[b],
                cubeDensities[a],
                cubeDensities[b]
            );
        }

        for (int i = 0; MarchingCubesTables.TriangleTable[cubeIndex, i] != -1; i += 3) {
            int edgeA = MarchingCubesTables.TriangleTable[cubeIndex, i];
            int edgeB = MarchingCubesTables.TriangleTable[cubeIndex, i + 1];
            int edgeC = MarchingCubesTables.TriangleTable[cubeIndex, i + 2];

            int vertexIndex = vertices.Count;

            vertices.Add(edgeVertices[edgeA]);
            vertices.Add(edgeVertices[edgeB]);
            vertices.Add(edgeVertices[edgeC]);

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
        }
    }

    private static Vector3 InterpolateVertex(Vector3 a, Vector3 b, float densityA, float densityB) {
        float denominator = densityB - densityA;

        if (Mathf.Abs(denominator) < 0.00001f) {
            return a;
        }

        float t = (SurfaceLevel - densityA) / denominator;
        t = Mathf.Clamp01(t);

        return Vector3.Lerp(a, b, t);
    }
}