using System;
using UnityEngine;

public class GalaxyMapMath
{
    public struct GalaxyShape
    {
        public float size;              // Distance at the end of the arm
        public float rPower;                 // Model parameter
        public float angleEndRad;       // End angle of the spiral arms
        public float armWidthRel;       // 0..1, width of arm
        public float armWidthPow;       // 0..+Inf, power used in arm width calculations
        public float centerSizeRel;     // 0..1, radius of center

        public float PointOnArmDistance(float angleRad)
        {
            // If angle is zero, we might get a log of zero or division by infinity
            // Let's just return zero instead
            if (angleRad == 0)
                return 0;

            if (angleRad < 0)
                angleRad = -angleRad;

            float rEnd = Mathf.Pow(this.angleEndRad, this.rPower);

            float r = Mathf.Pow(angleRad, this.rPower) / rEnd * this.size;

            return r;
        }

        public float ArmWidth(float angleRad)
        {
            if (angleRad < 0)
                angleRad = -angleRad;

            float r = this.PointOnArmDistance(angleRad);
            float angleRatio = angleRad / this.angleEndRad;
            float scale = Mathf.Pow(angleRatio, this.armWidthPow);
            float width = scale * this.armWidthRel * this.size;
            return width;
        }

        // Tests if given point is inside the galactic spiral
        public bool TestPointInSpiral(float r, float angleRad)
        {
            if (r < this.centerSizeRel * this.size)
                return true;

            if (angleRad < 0)
                angleRad = -angleRad;

            // We must test this angleRad and all angles: angleRad + 2*PI*N
            float angleRadTest = angleRad;
            bool result = false;
            while (angleRadTest < this.angleEndRad)
            {
                float armDist = this.PointOnArmDistance(angleRadTest);
                float armWidth = this.ArmWidth(angleRadTest);
                bool thisAngleTest = (r < armDist + 0.5f * armWidth) && (r > armDist - 0.5f * armWidth);
                if (thisAngleTest)
                {
                    result = true;
                    break;
                }
                angleRadTest += 2.0f * Mathf.PI;
            }
            return result;            
        }

        public bool TestPointInSpiral(Vector3 pos)
        {
            pos.z = -pos.z; // Hack: we must invert the angle, since our spiral is spinning counter-clockwise

            var posPolar = CartToPolar(pos);
            float angle = posPolar.angleRad;
            if (angle < 0)
                angle += 2.0f * Mathf.PI;
            return this.TestPointInSpiral(posPolar.r, angle);
        }

        public (Vector3, Vector3) CameraPositions(float angleRad, float camOffsetDistance, float camOffsetHeight)
        {
            float distLookAt = this.PointOnArmDistance(angleRad);
            Vector3 posLookAt = PolarToCart(angleRad, distLookAt);

            float camDistProj = distLookAt + camOffsetDistance * this.size; // Projected distance to camera
            float camHeight = camOffsetHeight * this.size;                  // Camera height above plane
            Vector3 camPos = PolarToCart(angleRad, camDistProj, camHeight);

            return (camPos, posLookAt);
        }
    }

    // Returns tangent of pitch angle at a point on the galactic arm
    /*
    public static float PointOnArmPitchTan(float angleRad, GalaxyShape shape)
    {
        if (angleRad < 0)
            angleRad = 0;

        if (angleRad == 0)
            return Mathf.PI / 2; // 90 degrees

        float cot = -shape.N * Mathf.Sin(angleRad / shape.N) * Mathf.Log(shape.rPower * Mathf.Tan(angleRad / (2.0f * shape.N)));
        return 1 / cot;
    }*/

    // Converts polar coordinates to cartezian coordinates in XZ plane
    // Y axis is always zero
    public static Vector3 PolarToCart(float angleRad, float distance, float height = 0)
    {
        return new Vector3(distance * Mathf.Cos(angleRad), height, distance * Mathf.Sin(angleRad));
    }

    public static (float r, float angleRad) CartToPolar(Vector3 pos)
    {
        return (pos.magnitude, Mathf.Atan2(pos.z, pos.x));
    }

    public static Vector3 Vec2dTo3d(Vector2 pos)
    {
        return new Vector3(pos.x, 0, pos.y);
    }

    public static Vector2 Vec3dTo2d(Vector3 pos)
    {
        return new Vector2(pos.x, pos.z);
    }
}