using UnityEngine;

public static class DensityInitializer {
    public static void FillFromSeed(DensityChunkData data, int seed) {
        FillFromSeed(data, seed, TerrainRegionDefinition.CreateDefault());
    }

    public static void FillFromSeed(DensityChunkData data, int seed, TerrainRegionDefinition terrainRegion) {
        TerrainRegionDefinition activeRegion = terrainRegion ?? TerrainRegionDefinition.CreateDefault();

        for (int x = 0; x < data.sampleCount; x++) {
            for (int y = 0; y < data.sampleCount; y++) {
                for (int z = 0; z < data.sampleCount; z++) {
                    Vector3 worldPos = data.SampleToWorldPosition(x, y, z);
                    float density = GetInitialDensity(worldPos, seed, activeRegion);
                    data.Set(x, y, z, density);
                }
            }
        }
    }

    private static float GetInitialDensity(Vector3 worldPos, int seed, TerrainRegionDefinition terrainRegion) {
        float safeNoiseScale = Mathf.Max(0.0001f, terrainRegion.NoiseScale);

        float seedOffsetX = (seed + terrainRegion.SeedOffset) * 13.37f;
        float seedOffsetZ = (seed + terrainRegion.SeedOffset) * 91.73f;

        float noise = Mathf.PerlinNoise(
            worldPos.x * safeNoiseScale + seedOffsetX,
            worldPos.z * safeNoiseScale + seedOffsetZ
        );

        float terrainHeight = terrainRegion.BaseHeight + noise * terrainRegion.HeightVariation;

        return terrainHeight - worldPos.y;
    }
}