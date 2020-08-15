using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct OrbitParameters
{
    // Longitude means rotational offset from the primary axis of the system in orbital mechanics.
    // e.g. if rotation is around z axis then either x or y might be the primary axis from which longitudes are measured.
    // Perihelion is the point of closest approach to the orbited body.
    [Tooltip("The angle from primary axis of the point of closest approach, in degrees"), Range(0, 360)]
    public float longitudeOfPerihelion;
    [Tooltip("Average orbital distance"), Range(0, 200)]
    public float meanDistance;
    [Tooltip("Motion of orbiting body per second, in degrees per second"), Range(0, 45)]
    public float motionPerSecond;
    [Tooltip("How elliptical the orbit is"), Range(0, 1)]
    public float eccentricity;
    // Mean longitude is the ecliptic longitude at which an orbiting body could be found if its orbit were circular, and free of perturbations, and if its inclination were zero
    [Tooltip("Angle the orbit starts from, in degrees"), Range(0, 360)]
    public float meanLongitude;


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

}

public class Orbit : MonoBehaviour
{
    public OrbitParameters parameters = new OrbitParameters {
        longitudeOfPerihelion = 0,
        meanDistance = 5,
        motionPerSecond = 0.1f,
        eccentricity = 0,
        meanLongitude = 0
    };

    [Tooltip("How finely subdivided the path rendering is"), Range(0, 1)]
    public float pathQuality = 1;

    float startTime = 0;

    [Tooltip("Transform to apply the position to, defaults to any child called 'Position'")]
    public Transform position;

    void UpdatePosition(float time)
    {
        this.position.localPosition = this.parameters.GetPosition(time);
    }

    void OnValidate()
    {
        if (this.position == null)
        {
            this.position = this.transform.Find("Position");
        }

        this.UpdatePosition(0);
        this.UpdateOrbitPath();
        // Debug.LogError($"Orbit hierarchies must not have any manual transforms applied: these objects are invalid: {}");
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

        int pathPoints = (int)(360 * this.pathQuality);
        float totalOrbitTime = 360 / this.parameters.motionPerSecond;
        float timePerPoint = totalOrbitTime / pathPoints;
        var path = new List<Vector3>();
        for (int i = 0; i < pathPoints; i++)
        {
            path.Add(this.parameters.GetPosition(i * timePerPoint));
        }
        lineRenderer.positionCount = path.Count;
        lineRenderer.SetPositions(path.ToArray());
    }

    // Start is called before the first frame update
    void Start()
    {
        if(this.position == null)
        {
            this.position = this.transform.Find("Position");
        }

        bool ValidateParents()
        {
            // Only game objects being controlled by Orbit component are allowed to have non-identity transforms
            var orbitControlled = GameObject.FindObjectsOfType<Orbit>().Select(o => o.position.gameObject);
            var invalidParents = this.GetAllParents()
                .Where(p => !orbitControlled.Contains(p) && !p.transform.IsIdentity());
            if(invalidParents.Any())
            {
                foreach (var p in invalidParents)
                {
                    Debug.LogError($"{this}: parent {p.name} is invalid, it has none zero transform", p);
                }
                return false;
            }
            return true;
        }
        Debug.Assert(ValidateParents());
        this.startTime = Time.time;
        this.UpdateOrbitPath();
    }

    // Update is called once per frame
    void Update()
    {
        this.UpdatePosition(Time.time - this.startTime);
    }
}
