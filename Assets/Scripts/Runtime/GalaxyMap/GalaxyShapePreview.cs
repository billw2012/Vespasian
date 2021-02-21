using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A component to preview galaxy properties in the editor

public class GalaxyShapePreview : MonoBehaviour
{
    List<Vector3> linePosVec = null;     // For positive angles
    List<Vector3> linePosNegVec = null;  // For negative angles
    List<Vector3> linePosInnerBorderVec = null;  // Inner and outer borders of arms
    List<Vector3> linePosOuterBorderVec = null;
    List<Vector3> linePosNegInnerBorderVec = null;  // Inner and outer borders of arms
    List<Vector3> linePosNegOuterBorderVec = null;

    [Range(0, 10.0f)]
    public float size = 1.0f;

    [Range(0, 10.0f)]
    public float rPower = 1.0f;

    [Range(0, 40.0f)]
    public float N = 4.0f;

    [Range(0, 20)]
    public float angleEndRad = 2.0f;

    [Range(0, 1)]
    public float armWidthRelative = 0.13f;

    [Range(0, 3)]
    public float armWidthPower = 1.0f;

    [Range(0, 1)]
    public float centerSizeRelative = 0.2f;

    private GalaxyMapMath.GalaxyShape shape;

    public GameObject starIconPrefab = null;

    void OnValidate()
    {
        // Read shape values
        this.shape = this.GetGalaxyShape();

        // Calculate positions for the line
        int nPoints = 256;
        List<float> angle_vec = new List<float>(nPoints);
        this.linePosVec = new List<Vector3>(nPoints);
        this.linePosNegVec = new List<Vector3>(nPoints);
        this.linePosInnerBorderVec = new List<Vector3>(nPoints);
        this.linePosOuterBorderVec = new List<Vector3>(nPoints);
        this.linePosNegInnerBorderVec = new List<Vector3>(nPoints);
        this.linePosNegOuterBorderVec = new List<Vector3>(nPoints);

        //Debug.Log($"Vector capacity: {this.linePosVec.Count}");
        for (int i = 0; i < this.linePosVec.Capacity; i++)
        {
            float angleRad = - this.angleEndRad * (float)i / nPoints;
            float dist = shape.PointOnArmDistance(angleRad);
            float angleRatio = Mathf.Abs( (float)i / (float)this.linePosVec.Capacity );
            float armWidth = shape.ArmWidth(angleRad);

            // Arm border lines (inner and outer borders)
            float distInner = Mathf.Clamp(dist - 0.5f * armWidth, 0, 100);
            this.linePosInnerBorderVec.Add(GalaxyMapMath.PolarToCart(angleRad, distInner));
            float distOuter = Mathf.Clamp(dist + 0.5f * armWidth, 0, 100);
            this.linePosOuterBorderVec.Add(GalaxyMapMath.PolarToCart(angleRad, distOuter));

            //Debug.Log($"i: {i}, angle: {angleRad}");
            this.linePosVec.Add(GalaxyMapMath.PolarToCart(angleRad, dist));

            // ------ Second arm -----------
            angleRad = angleRad + Mathf.PI;

            this.linePosNegVec.Add(GalaxyMapMath.PolarToCart(angleRad, dist));

            // Arm border lines (inner and outer borders)
            this.linePosNegInnerBorderVec.Add(GalaxyMapMath.PolarToCart(angleRad, distInner));
            this.linePosNegOuterBorderVec.Add(GalaxyMapMath.PolarToCart(angleRad, distOuter));
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (this.linePosVec != null)
        {
            for (int i = 0; i < this.linePosVec.Count - 1; i++)
            {
                // First arm
                Gizmos.DrawLine(this.linePosVec[i] + this.transform.position, this.linePosVec[i + 1] + this.transform.position);

                // Inner border
                Gizmos.DrawLine(this.linePosInnerBorderVec[i] + this.transform.position, this.linePosInnerBorderVec[i + 1] + this.transform.position);

                // Outer border
                Gizmos.DrawLine(this.linePosOuterBorderVec[i] + this.transform.position, this.linePosOuterBorderVec[i + 1] + this.transform.position);

                // Opposite arm
                //Gizmos.DrawLine(this.linePosNegVec[i] + this.transform.position, this.linePosNegVec[i + 1] + this.transform.position);


                // Inner border
                //Gizmos.DrawLine(this.linePosNegInnerBorderVec[i] + this.transform.position, this.linePosNegInnerBorderVec[i + 1] + this.transform.position);

                // Outer border
                //Gizmos.DrawLine(this.linePosNegOuterBorderVec[i] + this.transform.position, this.linePosNegOuterBorderVec[i + 1] + this.transform.position);

                // Center
                Gizmos.DrawWireSphere(this.transform.position, this.centerSizeRelative * this.size);
            }
        }
    }

    public GalaxyMapMath.GalaxyShape GetGalaxyShape()
    {
        GalaxyMapMath.GalaxyShape shape;
        shape.size = this.size;
        shape.rPower = this.rPower;
        shape.angleEndRad = this.angleEndRad;
        shape.armWidthRel = this.armWidthRelative;
        shape.armWidthPow = this.armWidthPower;
        shape.centerSizeRel = this.centerSizeRelative;
        return shape;
    }

    void GenerateStars()
    {
        if (this.starIconPrefab == null)
            return;

        int nStarsMax = 1024;
        int nStarsGenerated = 0;
        int nTries = 0;
        var shape = this.GetGalaxyShape();
        while (nStarsGenerated < nStarsMax && nTries < 10000)
        {
            float gs = shape.size; // Galaxy size (radius)
            Vector3 posRandom = new Vector3(Random.Range(-gs, gs), 0, Random.Range(-gs, gs));
            if (shape.TestPointInSpiral(posRandom))
            {
                GameObject starObj = GameObject.Instantiate(this.starIconPrefab, this.transform);
                starObj.transform.localPosition = posRandom;
                float starSize = 0.01f;
                starObj.transform.localScale = new Vector3(starSize, starSize, starSize);
                nStarsGenerated++;
            }
            nTries++;
        }
    }

    void Awake()
    {
        this.GenerateStars();
    }
}
