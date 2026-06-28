using UnityEngine;

public enum CpuTerrainBrushType {
    SmoothSphere,
    HardSphere
}

public static class CpuTerrainBrush {
    public static float EvaluateDensityDelta(
        Vector3 sampleWorldPosition,
        Vector3 brushCenter,
        float radius,
        float signedStrength,
        CpuTerrainBrushType brushType) {
        if (radius <= 0f) {
            return 0f;
        }

        float distance = Vector3.Distance(sampleWorldPosition, brushCenter);

        if (distance > radius) {
            return 0f;
        }

        if (brushType == CpuTerrainBrushType.HardSphere) {
            return signedStrength;
        }

        float falloff = 1f - distance / radius;
        return signedStrength * falloff;
    }
}