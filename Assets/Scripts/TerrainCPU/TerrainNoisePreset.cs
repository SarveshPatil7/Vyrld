using System;
using UnityEngine;

[Serializable]
public class TerrainNoisePreset {
    [Header("Preset")]
    public string presetName = "Default";

    [Header("Noise")]
    public int seed = 12345;
    public int numOctaves = 4;
    public float lacunarity = 2f;
    public float persistence = 0.5f;
    public float noiseScale = 24f;
    public float noiseWeight = 8f;
    public bool closeEdges = false;
    public float floorOffset = 8f;
    public float weightMultiplier = 1f;

    [Header("Hard Floor")]
    public float hardFloorHeight = -32f;
    public float hardFloorWeight = 0f;

    [Header("Extra Params")]
    public Vector4 shaderParams = Vector4.zero;

    public static TerrainNoisePreset CreateDefault() {
        return new TerrainNoisePreset();
    }

    public string GetDisplayName() {
        return presetName;
    }
}