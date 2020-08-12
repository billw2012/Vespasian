using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orbit : MonoBehaviour
{
    // Longitude means rotational offset from the primary axis of the system in orbital mechanics.
    // e.g. if rotation is around z axis then either x or y might be the primary axis from which longitudes are measured.
    // Perihelion is the point of closest approach to the orbited body.
    [Tooltip("The angle from primary axis of the point of closest approach, in degrees"), Range(0, 360)]
    public float longitudeOfPerihelion = 0;
    [Tooltip("Average orbital distance"), Range(0, 200)]
    public float meanDistance = 5;
    [Tooltip("Motion of orbiting body per second, in degrees per second"), Range(0, 45)]
    public float motionPerSecond = 0.1f;
    [Tooltip("How elliptical the orbit is"), Range(0, 1)]
    public float eccentricity = 0;
    // Mean longitude is the ecliptic longitude at which an orbiting body could be found if its orbit were circular, and free of perturbations, and if its inclination were zero
    [Tooltip("Angle the orbit starts from, in degrees"), Range(0, 360)]
    public float meanLongitude = 0;
    [Tooltip("How finely subdivided the path rendering is"), Range(0, 1)]
    public float pathQuality = 1;

    float startTime = 0;

    static float Mod2PI(float val)
    {
        while (val > Mathf.PI * 2)
            val -= Mathf.PI * 2;
        while (val < 0)
            val += Mathf.PI * 2;
        return val;
    }

    public Vector2 GetPosition(float time, float meanLongitudeOffset = 0)
    {
        float meanAnomaly = Mod2PI((this.motionPerSecond * time + this.meanLongitude + meanLongitudeOffset - this.longitudeOfPerihelion) * Mathf.Deg2Rad);
        float trueAnomaly = meanAnomaly + 1 * ((2 * this.eccentricity - Mathf.Pow(this.eccentricity, 3) / 4) * Mathf.Sin(meanAnomaly)
            + 5 / 4 * Mathf.Pow(this.eccentricity, 2) * Mathf.Sin(2 * meanAnomaly) + 13 / 12 * Mathf.Pow(this.eccentricity, 3) * Mathf.Sin(3 * meanAnomaly));
        float radiusVector = this.meanDistance * (1 - Mathf.Pow(this.eccentricity, 2)) / (1 + this.eccentricity * Mathf.Cos(trueAnomaly));
        return new Vector2(
            radiusVector * Mathf.Cos(trueAnomaly + this.longitudeOfPerihelion * Mathf.Deg2Rad),
            radiusVector * Mathf.Sin(trueAnomaly + this.longitudeOfPerihelion * Mathf.Deg2Rad)
        );
    }

    void UpdatePosition(float time)
    {
        this.transform.Find("Position").localPosition = GetPosition(time);
    }

    void OnValidate()
    {
        this.UpdatePosition(0);
        this.UpdateOrbitPath();
    }

    void UpdateOrbitPath()
    {
        var lineRenderer = this.GetComponent<LineRenderer>();
        if(lineRenderer == null)
        {
            return;
        }
        //lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        //lineRenderer.startWidth = lineRenderer.endWidth = 0.05f;
        //lineRenderer.startColor = lineRenderer.endColor = new Color(1, 1, 1, 0.2f);
        //lineRenderer.useWorldSpace = false;
        //lineRenderer.loop = true;
        // TODO: would be nice if it faded out from the current pos, but that requires recreating it each frame or 
        // doing a custom shader I think
        //lineRenderer.endColor = new Color(1, 1, 1, 0);
        //}
        //else
        //{
        //    lineRenderer = this.orbitPath.GetComponent<LineRenderer>();
        //}
        var pathPoints = (int)(360 * this.pathQuality);
        var totalOrbitTime = 360 / this.motionPerSecond;
        var timePerPoint = totalOrbitTime / pathPoints;
        var path = new List<Vector3>();
        for (int i = 0; i < pathPoints; i++)
        {
            path.Add(this.GetPosition(i * timePerPoint));
        }
        lineRenderer.positionCount = path.Count;
        lineRenderer.SetPositions(path.ToArray());
    }

    // Start is called before the first frame update
    void Start()
    {
        this.startTime = Time.time;
        this.UpdateOrbitPath();
    }

    // Update is called once per frame
    void Update()
    {
        this.UpdatePosition(Time.time - this.startTime);
    }
}
