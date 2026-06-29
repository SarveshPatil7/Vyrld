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
                    float density = EvaluateDensity(worldPos, seed, activeRegion);
                    data.Set(x, y, z, density);
                }
            }
        }
    }

    public static float EvaluateDensity(Vector3 worldPos, int seed, TerrainRegionDefinition terrainRegion) {
        TerrainRegionDefinition activeRegion = terrainRegion ?? TerrainRegionDefinition.CreateDefault();
        float safeNoiseScale = Mathf.Max(0.0001f, activeRegion.NoiseScale);

        float seedOffsetX = (seed + activeRegion.SeedOffset) * 13.37f;
        float seedOffsetZ = (seed + activeRegion.SeedOffset) * 91.73f;

        float noise = Mathf.PerlinNoise(
            worldPos.x * safeNoiseScale + seedOffsetX,
            worldPos.z * safeNoiseScale + seedOffsetZ
        );

        float terrainHeight = activeRegion.BaseHeight + noise * activeRegion.HeightVariation;

        return terrainHeight - worldPos.y;
    }
}