using System;
using UnityEngine;

[Serializable]
public class TerrainRegionDefinition {
    [SerializeField] private int regionId = 0;
    [SerializeField] private string regionName = "Default";

    [Header("Noise")]
    [SerializeField] private int seedOffset = 0;
    [Min(0.0001f)]
    [SerializeField] private float noiseScale = 0.06f;

    [Header("Height")]
    [SerializeField] private float baseHeight = 8f;
    [Min(0f)]
    [SerializeField] private float heightVariation = 8f;

    public int RegionId => regionId;
    public string RegionName => regionName;
    public int SeedOffset => seedOffset;
    public float BaseHeight => baseHeight;
    public float HeightVariation => heightVariation;
    public float NoiseScale => noiseScale;

    public TerrainRegionDefinition() {
    }

    public TerrainRegionDefinition(int regionId, string regionName, int seedOffset, float baseHeight, float heightVariation, float noiseScale) {
        this.regionId = regionId;
        this.regionName = regionName;
        this.seedOffset = seedOffset;
        this.baseHeight = baseHeight;
        this.heightVariation = heightVariation;
        this.noiseScale = noiseScale;
    }

    public static TerrainRegionDefinition CreateDefault() {
        return new TerrainRegionDefinition(0, "Default", 0, 8f, 8f, 0.06f);
    }

    public string GetDisplayName() {
        return $"{regionId}: {regionName}";
    }
}