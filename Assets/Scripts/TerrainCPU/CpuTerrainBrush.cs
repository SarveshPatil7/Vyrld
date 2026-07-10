using UnityEngine;

public enum CpuTerrainBrushType {
    SmoothSphere,
    HardSphere,
    RoughSphere,
    Flatten
}

public enum CpuTerrainFlattenMode {
    Horizontal,
    AveragePlane
}

public struct CpuTerrainBrushContext {
    public Vector3 planePoint;
    public Vector3 planeNormal;

    public static CpuTerrainBrushContext Horizontal(Vector3 center) {
        return new CpuTerrainBrushContext {
            planePoint = center,
            planeNormal = Vector3.up
        };
    }
}

public static class CpuTerrainBrush {
    public static float EvaluateDensityDelta(Vector3 sampleWorldPosition, float currentDensity, Vector3 brushCenter, float radius, float signedStrength,
                                                CpuTerrainBrushType brushType, float roughnessScale, float roughnessAmount, CpuTerrainBrushContext context) {
        if (radius <= 0f) {
            return 0f;
        }

        float distance = Vector3.Distance(sampleWorldPosition, brushCenter);

        if (distance > radius) {
            return 0f;
        }

        float falloff = 1f - distance / radius;

        if (brushType == CpuTerrainBrushType.HardSphere) {
            return signedStrength;
        }

        if (brushType == CpuTerrainBrushType.RoughSphere) {
            float noise = SampleNoise3D(sampleWorldPosition * roughnessScale);

            float centeredNoise = (noise - 0.5f) * 2f;
            float roughMultiplier = 1f + centeredNoise * roughnessAmount;
            roughMultiplier = Mathf.Max(0f, roughMultiplier);

            return signedStrength * falloff * roughMultiplier;
        }

        if (brushType == CpuTerrainBrushType.Flatten) {
            return EvaluateFlattenDelta(
                sampleWorldPosition,
                currentDensity,
                signedStrength,
                falloff,
                context
            );
        }

        return signedStrength * falloff;
    }

    private static float EvaluateFlattenDelta(Vector3 sampleWorldPosition, float currentDensity, float signedStrength, float falloff, CpuTerrainBrushContext context) {
        Vector3 planeNormal = context.planeNormal.normalized;

        if (planeNormal == Vector3.zero) {
            planeNormal = Vector3.up;
        }

        float targetDensity = Vector3.Dot(
            context.planePoint - sampleWorldPosition,
            planeNormal
        );

        float desiredChange = targetDensity - currentDensity;

        float flattenStrength = Mathf.Clamp01(Mathf.Abs(signedStrength) * 0.1f);

        return desiredChange * flattenStrength * falloff;
    }

    private static float SampleNoise3D(Vector3 position) {
        float xy = Mathf.PerlinNoise(position.x, position.y);
        float yz = Mathf.PerlinNoise(position.y, position.z);
        float xz = Mathf.PerlinNoise(position.x, position.z);

        return (xy + yz + xz) / 3f;
    }
}