using UnityEngine;

public static class DensityInitializer {
    public static void FillFromSeed(DensityChunkData data, int seed) {
        for (int x = 0; x < data.sampleCount; x++) {
            for (int y = 0; y < data.sampleCount; y++) {
                for (int z = 0; z < data.sampleCount; z++) {
                    Vector3 worldPos = data.SampleToWorldPosition(x, y, z);
                    float density = GetInitialDensity(worldPos, seed);
                    data.Set(x, y, z, density);
                }
            }
        }
    }

    private static float GetInitialDensity(Vector3 worldPos, int seed) {
        float seedOffsetX = seed * 13.37f;
        float seedOffsetZ = seed * 91.73f;

        float noise = Mathf.PerlinNoise(
            worldPos.x * 0.06f + seedOffsetX,
            worldPos.z * 0.06f + seedOffsetZ
        );

        float terrainHeight = 8f + noise * 8f;

        return terrainHeight - worldPos.y;
    }
}