using System;
using UnityEngine;

// https://arxiv.org/ftp/arxiv/papers/0908/0908.0892.pdf

public class GalaxyMapMath
{
    public struct GalaxyShape
    {
        public float size;              // Distance at the end of the arm
        public float B;                 // Model parameter
        public float N;                 // Model parameter
        public float angleEndRad;       // End angle of the spiral arms
        public float armWidthRel;       // 0..1, width of arm
        public float centerSizeRel;     // 0..1, radius of center
    }

    // Returns distance of a point on a galactic arm
    public static float PointOnArmDistance(float angleRad, float angleRadEnd, float size, float B, float N)
    {
        // If angle is zero, we might get a log of zero or division by infinity
        // Let's just return zero instead
        if (angleRad == 0)
            return 0;

        if (angleRad < 0)
            angleRad = -angleRad;

        // Calculate scale
        float rEnd = 1 / Mathf.Abs(Mathf.Log(B * Mathf.Tan(angleRadEnd / (2.0f * N))));

        float r = 1 / Mathf.Abs(Mathf.Log(B * Mathf.Tan(angleRad / (2.0f * N))));
        r = r * size / rEnd;
        return r;
    }

    public static float PointOnArmDistance(float angleRad, GalaxyShape shape)
    {
        return PointOnArmDistance(angleRad, shape.angleEndRad, shape.size, shape.B, shape.N);
    }

    public static (Vector3, Vector3) CameraPositions(float angleRad, GalaxyShape shape, float camOffsetDistance, float camOffsetHeight)
    {
        float distLookAt = PointOnArmDistance(angleRad, shape);
        Vector3 posLookAt = PolarToCart(angleRad, distLookAt);
        
        float camDistProj = distLookAt + camOffsetDistance * shape.size; // Projected distance to camera
        float camHeight = camOffsetHeight * shape.size;                // Camera height above plane
        Vector3 camPos = PolarToCart(angleRad, camDistProj, camHeight);

        return (camPos, posLookAt);
    }

    // Converts polar coordinates to cartezian coordinates in XZ plane
    // Y axis is always zero
    public static Vector3 PolarToCart(float angleRad, float distance, float height = 0)
    {
        return new Vector3(distance * Mathf.Cos(angleRad), height, distance * Mathf.Sin(angleRad));
    }
}