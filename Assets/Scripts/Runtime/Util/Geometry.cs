

using UnityEngine;

public static class Geometry
{
    // Intersects ray r = p + td, |d| = 1, with sphere s and, if intersecting, 
    // returns t value of intersection and intersection point q 

    public struct Intersect
    {
        public readonly static Intersect none = new Intersect { occurred = false };
        public bool occurred;
        public float t;
        public Vector3 at;
        public bool occurredOnSegment => this.occurred && this.t >= 0 && this.t <= 1;
    }

    public static Intersect IntersectLineSegmentSphere(Vector3 l0, Vector3 l1, Vector3 center, float radius)
    {
        var v = l1 - l0;
        var intersect = IntersectRaySphere(l0, v.normalized, center, radius);
        intersect.t /= v.magnitude;
        intersect.occurred = intersect.t >= 0 && intersect.t <= 1;
        return intersect;
    }
    
    public static Intersect IntersectRaySphere(Vector3 p, Vector3 d, Vector3 center, float radius)
    {
        var m = p - center;
        float b = Vector3.Dot(m, d);
        float c = Vector3.Dot(m, m) - radius * radius;

        // Exit if r’s origin outside s (c > 0) and r pointing away from s (b > 0)
        if (c > 0.0f && b > 0.0f) return Intersect.none;
        float discr = b * b - c;

        // A negative discriminant corresponds to ray missing sphere 
        if (discr < 0.0f) return Intersect.none;

        // Ray now found to intersect sphere, compute smallest t value of intersection
        float t = -b - Mathf.Sqrt(discr);

        // If t is negative, ray started inside sphere so clamp t to zero 
        if (t < 0.0f) t = 0.0f;

        return new Intersect { occurred = true, t = t, at = p + t * d };
    }

    //// Taken from http://answers.unity.com/answers/1658313/view.html
    //// Returns both intersection points 
    //public static Vector3[] IntersectionPoint(Vector3 p1, Vector3 p2, Vector3 center, float radius)
    //{
    //    //  get the distance between X and Z on the segment
    //    var dp = p2.x0z() - p1.x0z();
    //    // new Vector3 { p2.x - p1.x, 0,  };
    //    //dp.x = p2.x - p1.x;
    //    //dp.z = p2.z - p1.z;
    //    //   I don't get the math here
    //    float a = dp.x * dp.x + dp.z * dp.z;
    //    float b = 2 * (dp.x * (p1.x - center.x) + dp.z * (p1.z - center.z));
    //    float c = center.x * center.x + center.z * center.z;
    //    c += p1.x * p1.x + p1.z * p1.z;
    //    c -= 2 * (center.x * p1.x + center.z * p1.z);
    //    c -= radius * radius;
    //    float bb4ac = b * b - 4 * a * c;
    //    if (Mathf.Abs(a) < float.Epsilon || bb4ac < 0)
    //    {
    //        //  line does not intersect
    //        return new Vector3[] { Vector3.zero, Vector3.zero };
    //    }
    //    float mu1 = (-b + Mathf.Sqrt(bb4ac)) / (2 * a);
    //    float mu2 = (-b - Mathf.Sqrt(bb4ac)) / (2 * a);
    //    var sect = new Vector3[2];
    //    sect[0] = new Vector3(p1.x + mu1 * (p2.x - p1.x), 0, p1.z + mu1 * (p2.z - p1.z));
    //    sect[1] = new Vector3(p1.x + mu2 * (p2.x - p1.x), 0, p1.z + mu2 * (p2.z - p1.z));

    //    return sect;
    //}
}