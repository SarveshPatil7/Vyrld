using UnityEngine;

public static class DensityInitializer {
    public static void FillFromSeed(DensityChunkData data, int seed) {
        TerrainNoisePreset preset = TerrainNoisePreset.CreateDefault();
        preset.seed = seed;
        FillFromNoisePreset(data, preset);
    }

    public static void FillFromSeed(DensityChunkData data, int seed, TerrainRegionDefinition terrainRegion) {
        FillFromNoisePreset(data, CreatePresetFromLegacyRegion(seed, terrainRegion));
    }

    public static void FillFromNoisePreset(DensityChunkData data, TerrainNoisePreset noisePreset) {
        TerrainNoisePreset activePreset = noisePreset ?? TerrainNoisePreset.CreateDefault();

        for (int x = 0; x < data.sampleCount; x++) {
            for (int y = 0; y < data.sampleCount; y++) {
                for (int z = 0; z < data.sampleCount; z++) {
                    Vector3 worldPos = data.SampleToWorldPosition(x, y, z);
                    float density = EvaluateDensity(worldPos, activePreset);
                    data.Set(x, y, z, density);
                }
            }
        }
    }

    public static float EvaluateDensity(Vector3 worldPos, int seed, TerrainRegionDefinition terrainRegion) {
        return EvaluateDensity(worldPos, CreatePresetFromLegacyRegion(seed, terrainRegion));
    }

    public static float EvaluateDensity(Vector3 worldPos, TerrainNoisePreset noisePreset) {
        TerrainNoisePreset activePreset = noisePreset ?? TerrainNoisePreset.CreateDefault();

        float noise = EvaluateFractalHeightNoise(worldPos, activePreset);
        float terrainHeight = activePreset.floorOffset + noise * activePreset.noiseWeight;
        float density = (terrainHeight - worldPos.y) * activePreset.weightMultiplier;

        if (activePreset.hardFloorWeight > 0f) {
            float hardFloorDensity = (activePreset.hardFloorHeight - worldPos.y) * activePreset.hardFloorWeight;
            density = Mathf.Max(density, hardFloorDensity);
        }

        return density;
    }

    private static float EvaluateFractalHeightNoise(Vector3 worldPos, TerrainNoisePreset noisePreset) {
        int octaveCount = Mathf.Max(1, noisePreset.numOctaves);
        float safeNoiseScale = Mathf.Max(0.0001f, noisePreset.noiseScale);

        float amplitude = 1f;
        float frequency = 1f;
        float noiseSum = 0f;
        float amplitudeSum = 0f;

        for (int octave = 0; octave < octaveCount; octave++) {
            Vector2 octaveOffset = GetOctaveOffset2D(noisePreset.seed, octave);

            float sampleX = (worldPos.x + octaveOffset.x) / safeNoiseScale * frequency;
            float sampleZ = (worldPos.z + octaveOffset.y) / safeNoiseScale * frequency;

            float noise = Mathf.PerlinNoise(sampleX, sampleZ);

            noiseSum += noise * amplitude;
            amplitudeSum += amplitude;

            amplitude *= Mathf.Clamp01(noisePreset.persistence);
            frequency *= Mathf.Max(0.0001f, noisePreset.lacunarity);
        }

        if (amplitudeSum <= 0.0001f) {
            return 0f;
        }

        return noiseSum / amplitudeSum;
    }

    private static TerrainNoisePreset CreatePresetFromLegacyRegion(int seed, TerrainRegionDefinition terrainRegion) {
        TerrainRegionDefinition activeRegion = terrainRegion ?? TerrainRegionDefinition.CreateDefault();

        TerrainNoisePreset preset = TerrainNoisePreset.CreateDefault();
        preset.presetName = activeRegion.RegionName;
        preset.seed = seed + activeRegion.SeedOffset;
        preset.numOctaves = 1;
        preset.lacunarity = 2f;
        preset.persistence = 0.5f;
        preset.noiseScale = 1f / Mathf.Max(0.0001f, activeRegion.NoiseScale);
        preset.noiseWeight = activeRegion.HeightVariation;
        preset.floorOffset = activeRegion.BaseHeight;
        preset.weightMultiplier = 1f;
        preset.hardFloorHeight = -32f;
        preset.hardFloorWeight = 0f;

        return preset;
    }

    private static Vector2 GetOctaveOffset2D(int seed, int octaveIndex) {
        float offsetRange = 1000f;

        float x = HashSigned(seed, octaveIndex, 11) * offsetRange;
        float z = HashSigned(seed, octaveIndex, 73) * offsetRange;

        return new Vector2(x, z);
    }

    private static float HashSigned(int seed, int a, int b) {
        return Hash01(seed, a, b) * 2f - 1f;
    }

    private static float Hash01(int x, int y, int z) {
        unchecked {
            int hash = x * 374761393 + y * 668265263 + z * 2147483647;
            hash = (hash ^ (hash >> 13)) * 1274126177;
            hash = hash ^ (hash >> 16);

            return (hash & 0x7fffffff) / (float) int.MaxValue;
        }
    }
}